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
    [Route("api/businesses"), Authorize]
   
public class BusinessesController(IBusinessService businesses, IAuthService auth) : ControllerBase
{
    // Returns a fresh JWT that now carries the businessId claim.
    [HttpPost]
    public async Task<ActionResult<BusinessCreatedResponse>> Create(CreateBusinessRequest request)
    {
        var userId = User.GetUserId();
        var business = await businesses.CreateAsync(userId, request);
        var token = await auth.RefreshAsync(userId);
        return Ok(new BusinessCreatedResponse(business, token.Token));
    }

    [HttpGet("me")]
    public async Task<ActionResult<BusinessResponse>> Me() => Ok(await businesses.GetAsync(User.GetBusinessId()));

    [HttpPut("me")]
    public async Task<ActionResult<BusinessResponse>> Update(UpdateBusinessRequest request) =>
        Ok(await businesses.UpdateAsync(User.GetBusinessId(), request));

    [HttpPut("me/settings")]
    public async Task<ActionResult<BusinessResponse>> UpdateSettings(UpdateSettingsRequest request) =>
        Ok(await businesses.UpdateSettingsAsync(User.GetBusinessId(), request));
}
}