using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
    public record SubscriptionStatusResponse(bool IsActive, DateTime? EndDate, string? PlanName);
}
