using SellFlowApi.Data;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
    // Keeps product photos in the database, like payment receipts, so they survive Render restarts and redeploys.
    public class DbFileStorageService(SellFlowApiDbContext db, IConfiguration cfg) : IFileStorageService
    {
        private const int MaxBytes = 5 * 1024 * 1024;

        public async Task<string> SaveProductImageAsync(Guid businessId, IFormFile file)
        {
            if (file.Length == 0 || file.Length > MaxBytes) throw new DomainException("Image must be under 5 MB.");

            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            var bytes = buffer.ToArray();

            var contentType = Sniff(bytes) ?? throw new DomainException("Only JPG or PNG images are allowed.");

            var image = new ProductImage { BusinessId = businessId, ContentType = contentType, Data = bytes };
            db.ProductImages.Add(image);
            await db.SaveChangesAsync();

            var baseUrl = (cfg["App:PublicBaseUrl"] ?? "").TrimEnd('/');
            return $"{baseUrl}/api/images/{image.Id}";
        }

        // Looks at the first bytes of the file, not its name. WhatsApp only accepts real JPEG or PNG data.
        private static string? Sniff(byte[] b) =>
            b.Length > 8 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
            : b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 ? "image/png"
            : null;
    }
}
