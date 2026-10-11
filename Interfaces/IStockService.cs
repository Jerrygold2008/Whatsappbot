using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Models;

namespace SellFlowApi.Interfaces
{
   
public interface IStockService
{
    Task<bool> TryReserveAsync(Guid productId, Guid businessId, int qty);
    Task CommitAsync(Guid productId, int qty);
    Task ReleaseAsync(Guid productId, int qty);
    Task RestockAsync(Guid productId, int qty);
    Task CommitReservationsAsync(Order order);
    Task ReleaseReservationsAsync(Order order);
}

}