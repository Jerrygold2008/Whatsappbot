using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Implementation
{
public class DashboardService(SellFlowApiDbContext db) : IDashboardService
{
    private const int LowStockThreshold = 5;

    public async Task<DashboardOverviewResponse> GetOverviewAsync(Guid businessId)
    {
        // "Today" in Nigeria (WAT = UTC+1, no DST)
        var startUtc = DateTime.UtcNow.AddHours(1).Date.AddHours(-1);

        var todays = db.Orders.Where(o => o.BusinessId == businessId && o.CreatedAt >= startUtc);
        var count = await todays.CountAsync();
        var revenue = await todays.Where(o => o.PaymentStatus == PaymentStatus.Successful)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;
        var pending = await db.Orders.CountAsync(o => o.BusinessId == businessId && o.Status == OrderStatus.Pending);
        var products = await db.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive);
        var low = await db.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive
                                                    && p.StockQuantity - p.ReservedQuantity <= LowStockThreshold);
        return new DashboardOverviewResponse(count, revenue, pending, products, low);
    }

    public async Task<List<CustomerSummaryResponse>> ListCustomersAsync(Guid businessId)
    {
        var stats = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Spent = g.Sum(o => o.PaymentStatus == PaymentStatus.Successful ? o.Total : 0m),
                Last = g.Max(o => o.CreatedAt)
            }).ToListAsync();

        var customers = await db.Customers.AsNoTracking().Where(c => c.BusinessId == businessId).ToListAsync();
        return customers.Select(c =>
        {
            var s = stats.FirstOrDefault(x => x.CustomerId == c.Id);
            return new CustomerSummaryResponse(c.Id, c.FullName, c.WhatsAppNumber,
                s?.Count ?? 0, s?.Spent ?? 0m, s?.Last);
        }).OrderByDescending(c => c.LastOrderAt).ToList();
    }

    public async Task<List<PaymentResponse>> ListPaymentsAsync(Guid businessId)
    {
        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.Purpose == PaymentPurpose.Order)
            .OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync();

        var orderIds = payments.Where(p => p.OrderId != null).Select(p => p.OrderId!.Value).ToList();
        var orders = await db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id);

        return payments.Select(p =>
        {
            var o = p.OrderId is { } id ? orders.GetValueOrDefault(id) : null;
            return new PaymentResponse(p.Reference, o?.OrderNumber, p.Amount, p.Status, o?.PaymentMethod, p.PaidAt, p.CreatedAt);
        }).ToList();
    }
}

}