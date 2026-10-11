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
    [Route("api/store"), Authorize]
    
public class StoreController(IStoreReadinessService readiness) : ControllerBase
{
    // Setup checklist for the dashboard: what's still missing before the store goes live.
    [HttpGet("readiness")]
    public async Task<ActionResult<Readiness>> Readiness() => Ok(await readiness.CheckAsync(User.GetBusinessId()));
}
}