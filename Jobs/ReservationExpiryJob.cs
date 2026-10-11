using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Jobs
{
   
public class ReservationExpiryJob(SellFlowApiDbContext db, IOrderService orders)
{
    public async Task RunAsync()
    {
        var expiredOrderIds = await db.StockReservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < DateTime.UtcNow)
            .Select(r => r.OrderId).Distinct().ToListAsync();

        foreach (var id in expiredOrderIds)
        {
            var order = await db.Orders.Include(o => o.Reservations).FirstAsync(o => o.Id == id);
            if (order.Status != OrderStatus.Pending) continue;

            await orders.TransitionAsync(order.BusinessId, order.Id, OrderStatus.Cancelled, "system:expired");
        }
    }
}
}