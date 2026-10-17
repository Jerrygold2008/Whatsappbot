using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;

namespace SellFlowApi.Controllers
{
    // Public on purpose: WhatsApp downloads product photos from this address and customers are not signed in.
    // The ids are random, so a photo can't be guessed. Don't put anything private in a product photo.
    [ApiController, Route("api/images"), AllowAnonymous]
    public class ImagesController(SellFlowApiDbContext db) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var image = await db.ProductImages.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Data, x.ContentType })
                .FirstOrDefaultAsync();
            if (image is null) return NotFound();

            Response.Headers.CacheControl = "public,max-age=31536000,immutable";   // a new photo always gets a new id
            return File(image.Data, image.ContentType);
        }
    }
}
