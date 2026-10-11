using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
public interface IDashboardService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(Guid businessId);
    Task<List<CustomerSummaryResponse>> ListCustomersAsync(Guid businessId);
    Task<List<PaymentResponse>> ListPaymentsAsync(Guid businessId);
}
}