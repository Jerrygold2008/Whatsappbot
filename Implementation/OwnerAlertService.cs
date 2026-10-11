using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
// Owner-facing WhatsApp alerts. Every message follows the same layout:
//   *TITLE*  ─ divider ─  labelled facts  ─ divider ─  call to action, with reply buttons.
// Button ids:  pay:ok:{orderId}  pay:no:{orderId}  ord:{Status}:{orderId}
public class OwnerAlertService(SellFlowApiDbContext db, IWhatsAppClient wa, IConfiguration cfg,
    ILogger<OwnerAlertService> log) : IOwnerAlertService
{
    public const int LowStockThreshold = 5;
    private const string Divider = "━━━━━━━━━━━━━━";

    public async Task NotifyNewOrderAsync(Guid orderId)
    {
        try
        {
            var o = await LoadOrder(orderId);
            if (o is null) return;
            var (order, customer) = o.Value;

            var sb = new StringBuilder();
            sb.AppendLine("🛒 *NEW ORDER RECEIVED*").AppendLine(Divider);
            AppendOrderFacts(sb, order, customer);
            sb.AppendLine(Divider);

            if (order.PaymentMethod == PaymentMethod.BankTransfer)
            {
                sb.Append("⏳ Awaiting the customer's transfer receipt. You'll get it here for approval.");
                await SendToOwnerAsync(order.BusinessId, sb.ToString(), "SellFlow · New order", null);
            }
            else
            {
                sb.Append("💵 *Pay on Delivery.* Please confirm you can fulfil this order.");
                await SendToOwnerAsync(order.BusinessId, sb.ToString(), "SellFlow · New order", null,
                    ($"ord:{OrderStatus.Confirmed}:{order.Id}", "✅ Accept order"),
                    ($"ord:{OrderStatus.Cancelled}:{order.Id}", "❌ Decline order"));
            }
        }
        catch (Exception ex) { log.LogError(ex, "Owner new-order alert failed for {OrderId}", orderId); }
    }

    public async Task NotifyPaymentProofAsync(Guid orderId, Guid proofId)
    {
        try
        {
            var o = await LoadOrder(orderId);
            if (o is null) return;
            var (order, customer) = o.Value;
            var proof = await db.PaymentProofs.AsNoTracking().FirstOrDefaultAsync(p => p.Id == proofId);
            var account = await Account(order.BusinessId);
            if (account is null) return;

            string? headerId = null;
            if (proof is not null && proof.ContentType.StartsWith("image/"))
                headerId = await wa.UploadMediaAsync(account, proof.FileData, proof.ContentType);

            var sb = new StringBuilder();
            sb.AppendLine("🧾 *PAYMENT PROOF RECEIVED*").AppendLine(Divider);
            AppendOrderFacts(sb, order, customer);
            sb.AppendLine(Divider);
            sb.Append(headerId is null
                ? "The receipt is attached in your SellFlow dashboard. Please verify the credit in your bank app, then choose an option below."
                : "Please verify the credit in your bank app, then choose an option below.");

            await SendToOwnerAsync(order.BusinessId, sb.ToString(), "SellFlow · Payment review", headerId,
                ($"pay:ok:{order.Id}", "✅ Confirm payment"),
                ($"pay:no:{order.Id}", "❌ Reject payment"));
        }
        catch (Exception ex) { log.LogError(ex, "Owner proof alert failed for {OrderId}", orderId); }
    }

    public async Task NotifyOrderUpdatedAsync(Guid orderId, string changedBy)
    {
        try
        {
            var o = await LoadOrder(orderId);
            if (o is null) return;
            var (order, customer) = o.Value;

            var actions = NextActions(order).ToArray();
            var sb = new StringBuilder();
            sb.AppendLine($"{StatusIcon(order.Status)} *ORDER {StatusLabel(order.Status).ToUpperInvariant()}*").AppendLine(Divider);
            sb.AppendLine($"*Order:* {order.OrderNumber}");
            sb.AppendLine($"*Customer:* {customer?.FullName ?? "—"} (+{customer?.WhatsAppNumber})");
            sb.AppendLine($"*Total:* {Naira(order.Total)}");
            sb.AppendLine($"*Payment:* {PaymentLabel(order.PaymentStatus)}");
            sb.AppendLine(Divider);
            sb.Append(actions.Length > 0
                ? "The customer has been notified. Update the order when it moves to the next stage:"
                : "The customer has been notified. No further action is needed.");

            await SendToOwnerAsync(order.BusinessId, sb.ToString(), "SellFlow · Order update", null, actions);
        }
        catch (Exception ex) { log.LogError(ex, "Owner order-update alert failed for {OrderId}", orderId); }
    }

    public async Task SendLowStockReminderAsync(Guid businessId)
    {
        try
        {
            var products = await db.Products.AsNoTracking()
                .Where(p => p.BusinessId == businessId && p.IsActive && p.StockQuantity - p.ReservedQuantity <= LowStockThreshold)
                .OrderBy(p => p.StockQuantity - p.ReservedQuantity).Take(15).ToListAsync();
            if (products.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("📦 *STOCK REMINDER*").AppendLine(Divider);
            sb.AppendLine($"{products.Count} product{(products.Count == 1 ? "" : "s")} need restocking:").AppendLine();
            foreach (var p in products)
            {
                var left = p.StockQuantity - p.ReservedQuantity;
                sb.AppendLine(left <= 0 ? $"🔴 {p.Name} — *Out of stock*" : $"🟠 {p.Name} — {left} left");
            }
            sb.AppendLine(Divider);
            sb.Append("Out-of-stock products are hidden from customers. Restock them from your SellFlow dashboard.");
            await SendToOwnerAsync(businessId, sb.ToString(), "SellFlow · Daily reminder", null);
        }
        catch (Exception ex) { log.LogError(ex, "Low-stock reminder failed for {BusinessId}", businessId); }
    }

    public async Task SendPendingActionsAsync(Guid businessId)
    {
        var awaitingProof = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.Status == OrderStatus.Pending && o.PaymentStatus == PaymentStatus.ProofSubmitted)
            .OrderBy(o => o.CreatedAt).Take(3).Select(o => o.Id).ToListAsync();
        var active = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId &&
                        (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Processing || o.Status == OrderStatus.Dispatched ||
                         (o.Status == OrderStatus.Pending && o.PaymentMethod == PaymentMethod.PayOnDelivery)))
            .OrderBy(o => o.CreatedAt).Take(5).Select(o => o.Id).ToListAsync();

        if (awaitingProof.Count == 0 && active.Count == 0)
        {
            await SendOwnerTextAsync(businessId, "✨ *All caught up*\n" + Divider + "\nThere are no orders waiting for your action right now.");
            return;
        }

        await SendOwnerTextAsync(businessId,
            $"📋 *PENDING ACTIONS*\n{Divider}\n• Payments to review: *{awaitingProof.Count}*\n• Orders in progress: *{active.Count}*\n{Divider}\nAction cards follow below.");
        foreach (var id in awaitingProof)
        {
            var proof = await db.PaymentProofs.AsNoTracking().Where(p => p.OrderId == id)
                .OrderByDescending(p => p.UploadedAt).Select(p => p.Id).FirstOrDefaultAsync();
            await NotifyPaymentProofAsync(id, proof);
        }
        foreach (var id in active) await NotifyOrderUpdatedAsync(id, "owner:pending");
    }

    public async Task SendOwnerTextAsync(Guid businessId, string text) =>
        await SendToOwnerAsync(businessId, text, null, null);

    // ---------- helpers ----------

    public static IEnumerable<(string Id, string Title)> NextActions(Order o) => o.Status switch
    {
        OrderStatus.Pending when o.PaymentMethod == PaymentMethod.PayOnDelivery =>
            [($"ord:{OrderStatus.Confirmed}:{o.Id}", "✅ Accept order"), ($"ord:{OrderStatus.Cancelled}:{o.Id}", "❌ Decline order")],
        OrderStatus.Confirmed =>
            [($"ord:{OrderStatus.Processing}:{o.Id}", "🛠️ Processing"), ($"ord:{OrderStatus.Dispatched}:{o.Id}", "🚚 In transit"),
             ($"ord:{OrderStatus.Cancelled}:{o.Id}", "❌ Cancel order")],
        OrderStatus.Processing =>
            [($"ord:{OrderStatus.Dispatched}:{o.Id}", "🚚 In transit"), ($"ord:{OrderStatus.Cancelled}:{o.Id}", "❌ Cancel order")],
        OrderStatus.Dispatched =>
            [($"ord:{OrderStatus.Delivered}:{o.Id}", "📦 Delivered"), ($"ord:{OrderStatus.Failed}:{o.Id}", "⚠️ Delivery failed")],
        _ => []
    };

    public static string StatusLabel(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Processing => "Processing",
        OrderStatus.Dispatched => "In transit",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Failed => "Delivery failed",
        _ => s.ToString()
    };

    private static string StatusIcon(OrderStatus s) => s switch
    {
        OrderStatus.Confirmed => "✅", OrderStatus.Processing => "🛠️", OrderStatus.Dispatched => "🚚",
        OrderStatus.Delivered => "📦", OrderStatus.Cancelled => "❌", OrderStatus.Failed => "⚠️", _ => "🕒"
    };

    private static string PaymentLabel(PaymentStatus s) => s switch
    {
        PaymentStatus.Unpaid => "Unpaid (pay on delivery)",
        PaymentStatus.Pending => "Awaiting transfer",
        PaymentStatus.ProofSubmitted => "Receipt submitted",
        PaymentStatus.Successful => "Paid ✔",
        PaymentStatus.Failed => "Failed",
        PaymentStatus.NeedsRefund => "Refund required",
        PaymentStatus.Refunded => "Refunded",
        _ => s.ToString()
    };

    private static void AppendOrderFacts(StringBuilder sb, Order order, Customer? customer)
    {
        sb.AppendLine($"*Order:* {order.OrderNumber}");
        sb.AppendLine($"*Customer:* {customer?.FullName ?? "—"}");
        sb.AppendLine($"*WhatsApp:* +{customer?.WhatsAppNumber}");
        sb.AppendLine();
        foreach (var i in order.Items) sb.AppendLine($"• {i.ProductName} × {i.Quantity} — {Naira(i.UnitPrice * i.Quantity)}");
        sb.AppendLine();
        sb.AppendLine($"*Subtotal:* {Naira(order.Subtotal)}");
        sb.AppendLine($"*Delivery:* {Naira(order.DeliveryFee)}");
        sb.AppendLine($"*Total:* {Naira(order.Total)}");
        sb.AppendLine($"*Payment:* {(order.PaymentMethod == PaymentMethod.BankTransfer ? "Bank transfer" : "Pay on delivery")}");
        if (order.Delivery is { } d)
            sb.AppendLine($"*Deliver to:* {d.RecipientName}, {d.Address}, {d.City}, {d.State}" +
                          (string.IsNullOrWhiteSpace(d.Landmark) ? "" : $" (near {d.Landmark})"));
    }

    private async Task<(Order, Customer?)?> LoadOrder(Guid orderId)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return null;
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == order.CustomerId);
        return (order, customer);
    }

    private Task<WhatsAppAccount?> Account(Guid businessId) =>
        db.WhatsAppAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Status == WhatsAppStatus.Connected);

    private async Task SendToOwnerAsync(Guid businessId, string body, string? footer, string? headerImageId,
        params (string Id, string Title)[] buttons)
    {
        var settings = await db.BusinessSettings.AsNoTracking().FirstOrDefaultAsync(s => s.BusinessId == businessId);
        var owner = settings?.OwnerWhatsAppNumber;
        if (string.IsNullOrWhiteSpace(owner)) { log.LogInformation("No owner WhatsApp number for {BusinessId}", businessId); return; }
        var account = await Account(businessId);
        if (account is null) return;

        // Free-form messages need an open 24h window (the owner messaged the bot recently).
        var windowOpen = settings!.OwnerLastMessageAt is { } last && DateTime.UtcNow - last < TimeSpan.FromHours(23.5);
        if (!windowOpen)
        {
            // Outside the window Meta requires an approved template. Template body example:
            // "You have a new SellFlow update: {{1}}. Reply *pending* to review and take action."
            var template = cfg["WhatsApp:OwnerAlertTemplate"];
            if (string.IsNullOrWhiteSpace(template))
            {
                log.LogWarning("Owner 24h window closed for {BusinessId} and no WhatsApp:OwnerAlertTemplate configured.", businessId);
                return;
            }
            var summary = body.Split('\n')[0].Replace("*", "").Trim();
            await wa.SendTemplateAsync(account, owner!, template!, cfg["WhatsApp:OwnerAlertTemplateLanguage"] ?? "en", summary);
            return;
        }

        if (buttons.Length > 0 || headerImageId is not null)
            await wa.SendActionCardAsync(account, owner!, body, footer, headerImageId, buttons);
        else
            await wa.SendTextAsync(account, owner!, footer is null ? body : $"{body}\n\n_{footer}_");
    }

    private static string Naira(decimal v) => $"₦{v:N0}";
}
}
