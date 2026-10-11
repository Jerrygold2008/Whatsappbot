using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
  public record DashboardOverviewResponse(int TodaysOrders, decimal TodaysRevenue, int PendingOrders,
    int TotalProducts, int LowStockProducts);
}
