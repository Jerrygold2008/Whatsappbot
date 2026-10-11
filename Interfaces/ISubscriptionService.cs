using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
   
public interface ISubscriptionService
{
    Task<bool> HasActiveAccessAsync(Guid businessId);
    Task<string> StartPaymentAsync(Guid businessId, string merchantEmail, Guid? planId = null);
    Task ActivateAsync(Guid businessId, Guid planId);      // caller must SaveChanges
    Task ExpireAsync();
    Task<List<PlanResponse>> GetPlansAsync();
    Task<SubscriptionStatusResponse> GetStatusAsync(Guid businessId);
}
}