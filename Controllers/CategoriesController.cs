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
    [Route("api/categories"), Authorize(Policy = "ActiveSubscription")]
   public class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> List() => Ok(await categories.ListAsync(User.GetBusinessId()));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request) =>
        Ok(await categories.CreateAsync(User.GetBusinessId(), request));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, CategoryRequest request) =>
        Ok(await categories.UpdateAsync(User.GetBusinessId(), id, request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await categories.DeleteAsync(User.GetBusinessId(), id);
        return NoContent();
    }
}

}