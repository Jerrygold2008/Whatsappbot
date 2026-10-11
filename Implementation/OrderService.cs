using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
  
public class OrderService(SellFlowApiDbContext db, IStockService stock, INotificationService notifier,
    IOwnerAlertService ownerAlerts) : IOrderService
{
    private static readonly TimeSpan BankTransferHold = TimeSpan.FromHours(2);
    private static readonly TimeSpan PodHold = TimeSpan.FromHours(24);
 
    // Allowed transitions: anything not listed is rejected.
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending]    = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed]  = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Dispatched, OrderStatus.Cancelled],
        [OrderStatus.Dispatched] = [OrderStatus.Delivered, OrderStatus.Failed],
        [OrderStatus.Delivered]  = [],
        [OrderStatus.Cancelled]  = [],
        [OrderStatus.Failed]     = [],
    };
 
    public async Task<CheckoutResult> CreateOrderAsync(
        Guid businessId, Guid customerId, Guid cartId, PaymentMethod method, CustomerAddress address)
    {
        var business = await db.Businesses.Include(b => b.Settings)
            .FirstAsync(b => b.Id == businessId);
        var s = business.Settings;
 
        if (method == PaymentMethod.BankTransfer && !s.AcceptBankTransfer) throw new DomainException("Pay Now is not available.");
        if (method == PaymentMethod.PayOnDelivery && !s.AcceptPayOnDelivery) throw new DomainException("Pay on Delivery is not available.");
        if (s.DeliveryFee is null) throw new DomainException("This store has not set a delivery fee.");
 
        var cart = await db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId && c.BusinessId == businessId
                                      && c.CustomerId == customerId && c.Status == CartStatus.Active)
            ?? throw new DomainException("Cart not found.");
        if (cart.Items.Count == 0) throw new DomainException("Your cart is empty.");
 
        await using var tx = await db.Database.BeginTransactionAsync();
 
        var order = new Order
        {
            OrderNumber = $"ORD-{Random.Shared.Next(100000, 999999)}-{DateTime.UtcNow:yyMMdd}",
            BusinessId = businessId, CustomerId = customerId,
            PaymentMethod = method,
            PaymentStatus = method == PaymentMethod.BankTransfer ? PaymentStatus.Pending : PaymentStatus.Unpaid,
            DeliveryFee = s.DeliveryFee.Value,
        };
 
        var hold = method == PaymentMethod.BankTransfer ? BankTransferHold : PodHold;
 
        foreach (var item in cart.Items)
        {
            // Always re-read price from DB. Never trust client/chat values.
            var product = await db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.BusinessId == businessId && p.IsActive)
                ?? throw new DomainException("A product in your cart is no longer available.");
 
            if (!await stock.TryReserveAsync(product.Id, businessId, item.Quantity))
                throw new DomainException($"Sorry, {product.Name} doesn't have enough stock.");
 
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id, ProductName = product.Name,
                UnitPrice = product.Price, Quantity = item.Quantity
            });
            order.Reservations.Add(new StockReservation
            {
                ProductId = product.Id, Quantity = item.Quantity, ExpiresAt = DateTime.UtcNow + hold
            });
        }
 
        order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.Total = order.Subtotal + order.DeliveryFee;
 
        if (method == PaymentMethod.PayOnDelivery)
        {
            if (s.MaxPodOrderValue is { } max && order.Total > max)
                throw new DomainException("This order is too large for Pay on Delivery. Please pay now.");
            if (s.PodRequiresReturningCustomer &&
                !await db.Orders.AnyAsync(o => o.CustomerId == customerId && o.Status == OrderStatus.Delivered))
                throw new DomainException("First-time customers must pay now.");
        }
 
        order.Delivery = new Delivery
        {
            RecipientName = address.RecipientName, Phone = address.Phone, State = address.State,
            City = address.City, Address = address.Address, Landmark = address.Landmark,
            DeliveryFee = order.DeliveryFee
        };
 
        db.Orders.Add(order);
        cart.Status = CartStatus.Converted;
 
        BankDetails? bank = null;
        if (method == PaymentMethod.BankTransfer)
        {
            if (!s.HasBankDetails) throw new DomainException("This store hasn't added bank details yet.");
            db.Payments.Add(new Payment
            {
                BusinessId = businessId, Purpose = PaymentPurpose.Order, OrderId = order.Id,
                Reference = $"TRF-{order.OrderNumber}", Amount = order.Total, Provider = "bank_transfer"
            });
            bank = new BankDetails(s.BankName!, s.AccountNumber!, s.AccountName!);
        }
 
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new CheckoutResult(order, bank);
    }
 
    // Merchant/system status change. Enforces the state machine and side effects.
    public async Task TransitionAsync(Guid businessId, Guid orderId, OrderStatus to, string changedBy)
    {
        var order = await db.Orders.Include(o => o.Items).Include(o => o.Reservations).Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BusinessId == businessId)   // tenant scope
            ?? throw new DomainException("Order not found.");
 
        if (!Allowed[order.Status].Contains(to))
            throw new DomainException($"Cannot move order from {order.Status} to {to}.");
 
        // POD must be confirmed by merchant; Pay Now orders confirm automatically via webhook.
        if (to == OrderStatus.Confirmed && order.PaymentMethod == PaymentMethod.BankTransfer
            && order.PaymentStatus != PaymentStatus.Successful)
            throw new DomainException("Confirm the customer's payment first.");
 
        await using var tx = await db.Database.BeginTransactionAsync();
        var from = order.Status;
 
        switch (to)
        {
            case OrderStatus.Confirmed when order.PaymentMethod == PaymentMethod.PayOnDelivery:
                await stock.CommitReservationsAsync(order);
                break;
 
            case OrderStatus.Cancelled:
                await stock.ReleaseReservationsAsync(order);                       // never committed
                foreach (var r in order.Reservations.Where(r => r.Status == ReservationStatus.Committed))
                    await stock.RestockAsync(r.ProductId, r.Quantity);             // already decremented
                if (order.PaymentStatus == PaymentStatus.Successful)
                    order.PaymentStatus = PaymentStatus.NeedsRefund;               // merchant refunds manually in MVP
                else if (order.PaymentStatus is PaymentStatus.Pending or PaymentStatus.ProofSubmitted)
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    await db.Payments.Where(p => p.OrderId == order.Id && p.Status != PaymentStatus.Successful)
                        .ExecuteUpdateAsync(x => x.SetProperty(p => p.Status, PaymentStatus.Failed));
                }
                break;
 
            case OrderStatus.Delivered when order.PaymentMethod == PaymentMethod.PayOnDelivery:
                order.PaymentStatus = PaymentStatus.Successful;                    // cash collected
                break;
        }
 
        order.Status = to;
        if (order.Delivery is not null) order.Delivery.Status = to;
        db.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, From = from, To = to, ChangedBy = changedBy });
 
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await notifier.NotifyOrderStatusAsync(order.Id);
        await ownerAlerts.NotifyOrderUpdatedAsync(order.Id, changedBy);
    }
 
    public async Task<List<OrderResponse>> ListAsync(Guid businessId, OrderStatus? status)
    {
        var q = db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Delivery)
            .Where(o => o.BusinessId == businessId);
        if (status is not null) q = q.Where(o => o.Status == status);
 
        var list = await q.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync();
        var ids = list.Select(o => o.CustomerId).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var proofCounts = await db.PaymentProofs.AsNoTracking()
            .Where(p => ids.Count > 0 && list.Select(o => o.Id).Contains(p.OrderId))
            .GroupBy(p => p.OrderId).Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count);
        return list.Select(o => ToResponse(o, customers.GetValueOrDefault(o.CustomerId), proofCounts.GetValueOrDefault(o.Id))).ToList();
    }
 
    public async Task<OrderResponse?> GetAsync(Guid businessId, Guid orderId)
    {
        var o = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Delivery)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.BusinessId == businessId);
        if (o is null) return null;
        var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == o.CustomerId);
        var count = await db.PaymentProofs.CountAsync(p => p.OrderId == o.Id);
        return ToResponse(o, c, count);
    }
 
    private static OrderResponse ToResponse(Order o, Customer? c, int proofCount) => new(
        o.Id, o.OrderNumber, c?.FullName, c?.WhatsAppNumber ?? "", o.Subtotal, o.DeliveryFee, o.Total,
        o.PaymentMethod, o.PaymentStatus, o.Status, o.CreatedAt,
        o.Items.Select(i => new OrderItemResponse(i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList(),
        o.Delivery is null ? null : new DeliveryResponse(o.Delivery.RecipientName, o.Delivery.Phone, o.Delivery.State,
            o.Delivery.City, o.Delivery.Address, o.Delivery.Landmark, o.Delivery.Status),
        proofCount);
 
    // ---------------- Bank-transfer payment review ----------------
 
    public async Task ConfirmPaymentAsync(Guid businessId, Guid orderId, string confirmedBy)
    {
        var order = await db.Orders.Include(o => o.Items).Include(o => o.Reservations).Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BusinessId == businessId)
            ?? throw new NotFoundException("Order not found.");
 
        if (order.PaymentMethod != PaymentMethod.BankTransfer) throw new DomainException("This is a Pay on Delivery order.");
        if (order.PaymentStatus == PaymentStatus.Successful) throw new DomainException("Payment was already confirmed.");
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Cancelled))
            throw new DomainException("This order can no longer be confirmed.");
 
        await using var tx = await db.Database.BeginTransactionAsync();
        var from = order.Status;
 
        if (from == OrderStatus.Cancelled)
        {
            // Customer paid after the hold expired (or the order was cancelled): re-reserve, or tell the merchant to refund.
            foreach (var i in order.Items)
                if (!await stock.TryReserveAsync(i.ProductId, businessId, i.Quantity))
                    throw new DomainException("This order expired and the items are no longer in stock. Please refund the customer.");
            foreach (var i in order.Items)
                order.Reservations.Add(new StockReservation { ProductId = i.ProductId, Quantity = i.Quantity, ExpiresAt = DateTime.UtcNow });
        }
 
        await stock.CommitReservationsAsync(order);
        order.PaymentStatus = PaymentStatus.Successful;
        order.Status = OrderStatus.Confirmed;
        if (order.Delivery is not null) order.Delivery.Status = OrderStatus.Confirmed;
        db.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, From = from, To = OrderStatus.Confirmed, ChangedBy = confirmedBy });
 
        await db.Payments.Where(p => p.OrderId == order.Id && p.Status != PaymentStatus.Successful)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.Status, PaymentStatus.Successful)
                .SetProperty(p => p.PaidAt, DateTime.UtcNow));
 
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await notifier.NotifyOrderStatusAsync(order.Id);
        await ownerAlerts.NotifyOrderUpdatedAsync(order.Id, confirmedBy);
    }
 
    public async Task RejectPaymentAsync(Guid businessId, Guid orderId, string? reason, string rejectedBy)
    {
        var order = await db.Orders.Include(o => o.Reservations)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BusinessId == businessId)
            ?? throw new NotFoundException("Order not found.");
 
        if (order.PaymentMethod != PaymentMethod.BankTransfer || order.Status != OrderStatus.Pending
            || order.PaymentStatus is not (PaymentStatus.Pending or PaymentStatus.ProofSubmitted))
            throw new DomainException("There is no pending payment to reject on this order.");
 
        order.PaymentStatus = PaymentStatus.Pending;        // customer can send a new receipt
        var hold = DateTime.UtcNow.AddHours(2);
        foreach (var r in order.Reservations.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < hold))
            r.ExpiresAt = hold;
 
        await db.Payments.Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.ProofSubmitted)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.Status, PaymentStatus.Pending));
        await db.SaveChangesAsync();
        await notifier.NotifyPaymentRejectedAsync(order.Id, reason);
    }
 
    // Customer sent a transfer receipt on WhatsApp.
    public async Task<PaymentProof?> SubmitProofAsync(Guid businessId, Guid customerId, byte[] data, string contentType)
    {
        var order = await db.Orders
            .Where(o => o.BusinessId == businessId && o.CustomerId == customerId && o.PaymentMethod == PaymentMethod.BankTransfer
                        && o.Status == OrderStatus.Pending
                        && (o.PaymentStatus == PaymentStatus.Pending || o.PaymentStatus == PaymentStatus.ProofSubmitted))
            .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        if (order is null) return null;

        var proof = new PaymentProof { BusinessId = businessId, OrderId = order.Id, FileData = data, ContentType = contentType };
        db.PaymentProofs.Add(proof);
        order.PaymentStatus = PaymentStatus.ProofSubmitted;
        await db.Payments.Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.Status, PaymentStatus.ProofSubmitted));
        await db.SaveChangesAsync();
        return proof;
    }

    public async Task<List<PaymentProofResponse>> ListProofsAsync(Guid businessId, Guid orderId)
    {
        var list = await db.PaymentProofs.AsNoTracking()
            .Where(p => p.OrderId == orderId && p.BusinessId == businessId)     // tenant scope
            .OrderByDescending(p => p.UploadedAt).ToListAsync();
        return list.Select(p => new PaymentProofResponse(p.Id, p.UploadedAt, p.ContentType)).ToList();
    }
 
    public async Task<PaymentProof> GetProofAsync(Guid businessId, Guid orderId, Guid proofId) =>
        await db.PaymentProofs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proofId && p.OrderId == orderId && p.BusinessId == businessId)
        ?? throw new NotFoundException("Receipt not found.");
}
}