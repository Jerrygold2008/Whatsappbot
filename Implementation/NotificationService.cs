using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{

// Customer notifications. Free-form only inside WhatsApp's 24h window; otherwise a template is required.
public class NotificationService(SellFlowApiDbContext db, IWhatsAppClient wa, IConfiguration cfg,
    ILogger<NotificationService> log) : INotificationService
{
    private const string Divider = "━━━━━━━━━━━━━━";

    public async Task NotifyPaymentRejectedAsync(Guid orderId, string? reason)
    {
        try
        {
            var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return;
            var text = $"⚠️ *PAYMENT NOT CONFIRMED*\n{Divider}\n*Order:* {order.OrderNumber}\n*Amount:* ₦{order.Total:N0}\n"
                + (string.IsNullOrWhiteSpace(reason) ? "" : $"*Reason:* {reason}\n")
                + $"{Divider}\nPlease reply with a clear screenshot of your transfer receipt showing the amount, date and reference. Your items are reserved for the next 2 hours.";
            await SendAsync(order, text, "payment_rejected");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to send payment rejection for order {OrderId}", orderId);
        }
    }

    public async Task NotifyOrderStatusAsync(Guid orderId)
    {
        try
        {
            var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return;

            var head = $"*Order:* {order.OrderNumber}\n*Total:* ₦{order.Total:N0}";
            var text = order.Status switch
            {
                OrderStatus.Confirmed  => $"✅ *ORDER CONFIRMED*\n{Divider}\n{head}\n{Divider}\n" +
                    (order.PaymentStatus == PaymentStatus.Successful ? "We've received your payment. " : "") +
                    "Your order is being prepared and we'll keep you updated at every step.",
                OrderStatus.Processing => $"🛠️ *ORDER IN PROGRESS*\n{Divider}\n{head}\n{Divider}\nWe're packing your items now.",
                OrderStatus.Dispatched => $"🚚 *ORDER IN TRANSIT*\n{Divider}\n{head}\n{Divider}\nYour order is on its way. Please keep your phone close so the rider can reach you." +
                    (order.PaymentMethod == PaymentMethod.PayOnDelivery ? $"\n\n💵 Please have *₦{order.Total:N0}* ready on delivery." : ""),
                OrderStatus.Delivered  => $"📦 *ORDER DELIVERED*\n{Divider}\n{head}\n{Divider}\nThank you for shopping with us! Reply *menu* anytime to order again.",
                OrderStatus.Cancelled  => $"❌ *ORDER CANCELLED*\n{Divider}\n{head}\n{Divider}\n" +
                    (order.PaymentStatus == PaymentStatus.NeedsRefund ? "Your payment will be refunded by the business." : "If this is unexpected, reply *agent* to speak with us."),
                OrderStatus.Failed     => $"⚠️ *DELIVERY UNSUCCESSFUL*\n{Divider}\n{head}\n{Divider}\nWe couldn't complete your delivery. Reply *agent* and we'll arrange a new time.",
                _ => null
            };
            if (text is null) return;
            await SendAsync(order, text, $"order_{order.Status.ToString().ToLowerInvariant()}");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to notify customer for order {OrderId}", orderId);   // never break the order flow
        }
    }

    private async Task SendAsync(Order order, string text, string templateKey)
    {
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId);
        var account = await db.WhatsAppAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.BusinessId == order.BusinessId && a.Status == WhatsAppStatus.Connected);
        if (account is null) return;
        var convo = await db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId == order.BusinessId && c.CustomerId == customer.Id);

        if (convo?.LastCustomerMessageAt is { } last && DateTime.UtcNow - last < TimeSpan.FromHours(24))
        {
            await wa.SendTextAsync(account, customer.WhatsAppNumber, text);
            return;
        }

        // Outside 24h: use an approved template if configured, e.g. WhatsApp:Templates:order_dispatched = "order_dispatched"
        // Template body example: "Hello, your order {{1}} is now {{2}}. Reply menu to view details."
        var template = cfg[$"WhatsApp:Templates:{templateKey}"];
        if (!string.IsNullOrWhiteSpace(template))
            await wa.SendTemplateAsync(account, customer.WhatsAppNumber, template!, cfg["WhatsApp:TemplateLanguage"] ?? "en",
                order.OrderNumber, OwnerAlertService.StatusLabel(order.Status).ToLowerInvariant());
        else
            log.LogWarning("Outside 24h window for {Order}; configure WhatsApp:Templates:{Key}.", order.OrderNumber, templateKey);
    }
}
}
