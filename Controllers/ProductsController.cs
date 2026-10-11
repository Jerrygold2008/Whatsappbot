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
    [Route("api/products"), Authorize(Policy = "ActiveSubscription")]
   public class ProductsController(IProductService products, IFileStorageService storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> List([FromQuery] bool includeInactive = false) =>
        Ok(await products.ListAsync(User.GetBusinessId(), includeInactive));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id) => Ok(await products.GetAsync(User.GetBusinessId(), id));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        var created = await products.CreateAsync(User.GetBusinessId(), request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductRequest request) =>
        Ok(await products.UpdateAsync(User.GetBusinessId(), id, request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await products.DeactivateAsync(User.GetBusinessId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    public async Task<ActionResult<ProductResponse>> UploadImage(Guid id, IFormFile file)
    {
        var businessId = User.GetBusinessId();
        await products.GetAsync(businessId, id);                 // ownership check before writing any file
        var url = await storage.SaveProductImageAsync(businessId, file);
        return Ok(await products.SetImageAsync(businessId, id, url));
    }
}
}