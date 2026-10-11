using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Security
{
   public class ActiveSubscriptionHandler(ISubscriptionService subs)
    : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, ActiveSubscriptionRequirement req)
    {
        var claim = ctx.User.FindFirst("businessId")?.Value;
        if (Guid.TryParse(claim, out var businessId) && await subs.HasActiveAccessAsync(businessId))
            ctx.Succeed(req);
    }
}

}

