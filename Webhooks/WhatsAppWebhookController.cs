using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using SellFlowApi.Helpers;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Webhooks
{
[ApiController, Route("api/whatsapp/webhook")]
public class WhatsAppWebhookController(IConfiguration cfg, Hangfire.IBackgroundJobClient jobs) : ControllerBase
{
    // Meta's one-time verification handshake
    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string mode,
                                [FromQuery(Name = "hub.verify_token")] string token,
                                [FromQuery(Name = "hub.challenge")] string challenge) =>
        mode == "subscribe" && token == cfg["WhatsApp:VerifyToken"] ? Content(challenge) : Forbid();

    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        // Verify X-Hub-Signature-256 with the Meta app secret
        var sig = Request.Headers["X-Hub-Signature-256"].ToString().Replace("sha256=", "");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(cfg["WhatsApp:AppSecret"]!));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig.ToLowerInvariant())))
            return Unauthorized();

        // Acknowledge fast; process in the background so Meta doesn't retry.
        foreach (var message in MetaPayload.Parse(raw))
            jobs.Enqueue<IWhatsAppProcessor>(p => p.ProcessAsync(message));

        return Ok();
    }
}
}