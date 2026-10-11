using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SellFlowApi.Dtos;
using SellFlowApi.Helpers;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Controllers
{
    [ApiController]
    [Route("api/subscription"), Authorize]
   
public class SubscriptionController(ISubscriptionService subs) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<ActionResult<List<PlanResponse>>> Plans() => Ok(await subs.GetPlansAsync());

    [HttpGet]
    public async Task<ActionResult<SubscriptionStatusResponse>> Status() =>
        Ok(await subs.GetStatusAsync(User.GetBusinessId()));

    [HttpPost("pay")]
    public async Task<ActionResult<PaymentLinkResponse>> Pay(PayRequest? request)
    {
        var url = await subs.StartPaymentAsync(User.GetBusinessId(), User.GetEmail(), request?.PlanId);
        return Ok(new PaymentLinkResponse(url));
    }
}
}