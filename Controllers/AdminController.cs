using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SellFlowApi.Dtos;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Controllers
{
    [ApiController]
    [Route("api/admin"), Authorize(Policy = "ActiveSubscription")]
   
public class AdminController(IAdminService admin, IConfiguration cfg) : ControllerBase
{
    [HttpPost("whatsapp/link")]
    public async Task<IActionResult> LinkWhatsApp(LinkWhatsAppRequest request)
    {
        if (!IsAdmin()) return Unauthorized();
        await admin.LinkWhatsAppAsync(request);
        return NoContent();
    }

    [HttpPut("businesses/{businessId:guid}/paystack-subaccount")]
    public async Task<IActionResult> SetSubaccount(Guid businessId, SetSubaccountRequest request)
    {
        if (!IsAdmin()) return Unauthorized();
        await admin.SetPaystackSubaccountAsync(businessId, request.SubaccountCode);
        return NoContent();
    }

    private bool IsAdmin()
    {
        var expected = cfg["Admin:Key"] ?? "";
        var supplied = Request.Headers["X-Admin-Key"].ToString();
        return expected.Length > 0 &&
               CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
    }
}

}