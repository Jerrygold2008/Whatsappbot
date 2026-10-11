using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Enums;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
// Messages from the owner's number never enter the customer shopping bot.
public class OwnerCommandHandler(SellFlowApiDbContext db, IWhatsAppClient wa, IOrderService orders,
    IOwnerAlertService alerts, ILogger<OwnerCommandHandler> log) : IOwnerCommandHandler
{
    private const string DefaultRejectReason = "We could not verify this transfer in our account.";

    public async Task HandleAsync(WhatsAppAccount account, BusinessSettings settings, string input)
    {
        var owner = settings.OwnerWhatsAppNumber!;
        var businessId = settings.BusinessId;
        var parts = input.Split(':');

        try
        {
            // Payment proof buttons
            if (parts is ["pay", var verdict, var rawId] && Guid.TryParse(rawId, out var payOrder))
            {
                if (!await OwnsOrder(businessId, payOrder)) return;
                if (verdict == "ok")
                {
                    await orders.ConfirmPaymentAsync(businessId, payOrder, $"owner-whatsapp:{owner}");
                    // ConfirmPaymentAsync sends the follow-up order card with next status buttons.
                }
                else
                {
                    await orders.RejectPaymentAsync(businessId, payOrder, DefaultRejectReason, $"owner-whatsapp:{owner}");
                    var num = await db.Orders.Where(o => o.Id == payOrder).Select(o => o.OrderNumber).FirstAsync();
                    await wa.SendTextAsync(account, owner,
                        $"🚫 *PAYMENT REJECTED*\n━━━━━━━━━━━━━━\n*Order:* {num}\n\nThe customer has been asked to send a clearer receipt. You'll be notified when it arrives.");
                }
                return;
            }

            // Order status buttons
            if (parts is ["ord", var rawStatus, var rawOrder]
                && Enum.TryParse<OrderStatus>(rawStatus, out var to) && Guid.TryParse(rawOrder, out var orderId))
            {
                if (!await OwnsOrder(businessId, orderId)) return;
                await orders.TransitionAsync(businessId, orderId, to, $"owner-whatsapp:{owner}");
                return;
            }

            switch (input.Trim().ToLowerInvariant())
            {
                case "pending": case "orders": case "menu": case "hi": case "hello":
                    await alerts.SendPendingActionsAsync(businessId); return;
                case "stock": case "inventory":
                    await alerts.SendLowStockReminderAsync(businessId);
                    return;
                default:
                    await wa.SendTextAsync(account, owner,
                        "👋 *SellFlow Owner Assistant*\n━━━━━━━━━━━━━━\nReply with:\n• *pending* — orders waiting for you\n• *stock* — low-stock products\n\nPayment and delivery updates arrive here automatically with action buttons.");
                    return;
            }
        }
        catch (DomainException ex)
        {
            // e.g. button tapped twice or order already moved on
            await wa.SendTextAsync(account, owner, $"ℹ️ *No change made*\n{ex.Message}");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Owner command failed: {Input}", input);
            await wa.SendTextAsync(account, owner, "⚠️ Something went wrong while updating that order. Please try again from your dashboard.");
        }
    }

    private Task<bool> OwnsOrder(Guid businessId, Guid orderId) =>
        db.Orders.AnyAsync(o => o.Id == orderId && o.BusinessId == businessId);
}
}
