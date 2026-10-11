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
 public class StockService(SellFlowApiDbContext db) : IStockService
{
    // Single conditional UPDATE: succeeds only if enough unreserved stock exists.
    public async Task<bool> TryReserveAsync(Guid productId, Guid businessId, int qty)
    {
        var rows = await db.Products
            .Where(p => p.Id == productId && p.BusinessId == businessId && p.IsActive
                        && p.StockQuantity - p.ReservedQuantity >= qty)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReservedQuantity, p => p.ReservedQuantity + qty));
        return rows == 1;
    }

    // Payment verified / merchant confirmed: reserved units become a real decrement.
    public Task CommitAsync(Guid productId, int qty) =>
        db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity - qty)
                .SetProperty(p => p.ReservedQuantity, p => p.ReservedQuantity - qty));

    public Task ReleaseAsync(Guid productId, int qty) =>
        db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReservedQuantity, p => p.ReservedQuantity - qty));

    public Task RestockAsync(Guid productId, int qty) =>
        db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + qty));

    public async Task CommitReservationsAsync(Order order)
    {
        foreach (var r in order.Reservations.Where(r => r.Status == ReservationStatus.Active))
        {
            await CommitAsync(r.ProductId, r.Quantity);
            r.Status = ReservationStatus.Committed;
        }
    }

    public async Task ReleaseReservationsAsync(Order order)
    {
        foreach (var r in order.Reservations.Where(r => r.Status == ReservationStatus.Active))
        {
            await ReleaseAsync(r.ProductId, r.Quantity);
            r.Status = ReservationStatus.Released;
        }
    }
}
}