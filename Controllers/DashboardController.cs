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
    [Route("api/dashboard"), Authorize(Policy = "ActiveSubscription")]
   
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewResponse>> Overview() =>
        Ok(await dashboard.GetOverviewAsync(User.GetBusinessId()));

    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSummaryResponse>>> Customers() =>
        Ok(await dashboard.ListCustomersAsync(User.GetBusinessId()));

    [HttpGet("payments")]
    public async Task<ActionResult<List<PaymentResponse>>> Payments() =>
        Ok(await dashboard.ListPaymentsAsync(User.GetBusinessId()));
}
}