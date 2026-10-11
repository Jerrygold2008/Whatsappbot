using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Webhooks
{
    
[ApiController, Route("api/payments/webhook")]
public class PaymentWebhookController(IPaymentWebhookService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();
        try { await svc.HandleAsync(raw, Request.Headers["x-paystack-signature"]); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        return Ok();   // always 200 on valid signature so Paystack stops retrying
    }
}
}