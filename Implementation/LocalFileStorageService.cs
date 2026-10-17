using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Implementation
{
   
public class LocalFileStorageService(IWebHostEnvironment env, IConfiguration cfg) : IFileStorageService
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png"];

    public async Task<string> SaveProductImageAsync(Guid businessId, IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024) throw new DomainException("Image must be under 5 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new DomainException("Only JPG or PNG images are allowed.");

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", businessId.ToString("N"));
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";                       // never trust the client file name
        await using (var fs = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(fs);

        var baseUrl = (cfg["App:PublicBaseUrl"] ?? "").TrimEnd('/');
        return $"{baseUrl}/uploads/{businessId:N}/{name}";
    }
}

}