using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
    
public class PaymentWebhookService(SellFlowApiDbContext db, IPaymentGateway gateway, IStockService stock,
    ISubscriptionService subs, INotificationService notifier, IConfiguration cfg) : IPaymentWebhookService
{
    public static bool IsValidSignature(string rawBody, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(signatureHeader)) return false;
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant()));
    }

    public async Task HandleAsync(string rawBody, string? signature)
    {
        if (!IsValidSignature(rawBody, signature, cfg["Paystack:SecretKey"]!))
            throw new UnauthorizedAccessException("Bad signature");

        using var doc = JsonDocument.Parse(rawBody);
        if (doc.RootElement.GetProperty("event").GetString() != "charge.success") return;
        var reference = doc.RootElement.GetProperty("data").GetProperty("reference").GetString()!;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        if (payment is null || payment.Status == PaymentStatus.Successful) return;   // unknown or already processed

        // Never trust the webhook body: ask Paystack directly.
        var (ok, amount) = await gateway.VerifyAsync(reference);
        if (!ok) return;

        if (amount != payment.Amount)
        {
            payment.Status = PaymentStatus.NeedsRefund;       // wrong amount: manual review, do NOT confirm
            await db.SaveChangesAsync();
            return;
        }

        // Atomic claim: only ONE concurrent webhook retry wins this update.
        var claimed = await db.Payments
            .Where(p => p.Id == payment.Id && p.Status != PaymentStatus.Successful)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Successful)
                .SetProperty(p => p.PaidAt, DateTime.UtcNow));
        if (claimed == 0) return;

        if (payment.Purpose == PaymentPurpose.Subscription)
        {
            await subs.ActivateAsync(payment.BusinessId, payment.PlanId!.Value);
            await db.SaveChangesAsync();
            return;
        }

        await ApplyOrderPaymentAsync(payment);
    }

    private async Task ApplyOrderPaymentAsync(Payment payment)
    {
        var order = await db.Orders.Include(o => o.Items).Include(o => o.Reservations).Include(o => o.Delivery)
            .FirstAsync(o => o.Id == payment.OrderId);

        await using var tx = await db.Database.BeginTransactionAsync();

        var reservationsLive = order.Reservations.Any(r => r.Status == ReservationStatus.Active);

        if (!reservationsLive && order.Status == OrderStatus.Cancelled)
        {
            // LATE PAYMENT: hold expired. Try to re-reserve; if stock is gone, flag a refund.
            var allOk = true;
            foreach (var i in order.Items)
                allOk &= await stock.TryReserveAsync(i.ProductId, order.BusinessId, i.Quantity);

            if (!allOk)
            {
                order.PaymentStatus = PaymentStatus.NeedsRefund;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return;   // TODO: alert merchant + platform admin
            }
            foreach (var i in order.Items)
                order.Reservations.Add(new StockReservation { ProductId = i.ProductId, Quantity = i.Quantity, ExpiresAt = DateTime.UtcNow });
        }

        await stock.CommitReservationsAsync(order);
        order.PaymentStatus = PaymentStatus.Successful;
        db.OrderStatusHistory.Add(new OrderStatusHistory
            { OrderId = order.Id, From = order.Status, To = OrderStatus.Confirmed, ChangedBy = "system:payment" });
        order.Status = OrderStatus.Confirmed;
        if (order.Delivery is not null) order.Delivery.Status = OrderStatus.Confirmed;

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await notifier.NotifyOrderStatusAsync(order.Id);
    }
}

}