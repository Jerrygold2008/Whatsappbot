using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
  
public class ProductService(SellFlowApiDbContext db) : IProductService
{
    public async Task<List<ProductResponse>> ListAsync(Guid businessId, bool includeInactive)
    {
        var list = await db.Products.AsNoTracking()
            .Where(p => p.BusinessId == businessId && (includeInactive || p.IsActive))
            .OrderBy(p => p.Name).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<ProductResponse> GetAsync(Guid businessId, Guid id) => Map(await Find(businessId, id));

    public async Task<ProductResponse> CreateAsync(Guid businessId, ProductRequest r)
    {
        Validate(r);
        await EnsureCategory(businessId, r.CategoryId);
        var p = new Product
        {
            BusinessId = businessId, CategoryId = r.CategoryId, Name = r.Name.Trim(), Description = r.Description?.Trim(),
            Price = r.Price, StockQuantity = r.StockQuantity, ImageUrl = r.ImageUrl, IsActive = r.IsActive
        };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        return Map(p);
    }

    public async Task<ProductResponse> UpdateAsync(Guid businessId, Guid id, ProductRequest r)
    {
        Validate(r);
        await EnsureCategory(businessId, r.CategoryId);
        var p = await Find(businessId, id);
        if (r.StockQuantity < p.ReservedQuantity)
            throw new DomainException($"{p.ReservedQuantity} unit(s) are reserved by pending orders. Stock cannot go below that.");

        p.Name = r.Name.Trim(); p.Description = r.Description?.Trim(); p.Price = r.Price;
        p.StockQuantity = r.StockQuantity; p.CategoryId = r.CategoryId; p.IsActive = r.IsActive;
        if (r.ImageUrl is not null) p.ImageUrl = r.ImageUrl;     // price change never touches existing orders (snapshots)

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new DomainException("Stock changed while saving. Please try again."); }
        return Map(p);
    }

    // Soft delete only: products referenced by orders must never be hard-deleted.
    public async Task DeactivateAsync(Guid businessId, Guid id)
    {
        var p = await Find(businessId, id);
        p.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task<ProductResponse> SetImageAsync(Guid businessId, Guid id, string imageUrl)
    {
        var p = await Find(businessId, id);
        p.ImageUrl = imageUrl;
        await db.SaveChangesAsync();
        return Map(p);
    }

    private async Task<Product> Find(Guid businessId, Guid id) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId)
        ?? throw new NotFoundException("Product not found.");

    private async Task EnsureCategory(Guid businessId, Guid? categoryId)
    {
        if (categoryId is null) return;
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId && c.BusinessId == businessId))
            throw new DomainException("Category does not belong to your business.");
    }

    private static void Validate(ProductRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new DomainException("Product name is required.");
        if (r.Price <= 0) throw new DomainException("Price must be greater than zero.");
        if (r.StockQuantity < 0) throw new DomainException("Stock cannot be negative.");
    }

    private static ProductResponse Map(Product p) => new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity,
        p.ReservedQuantity, p.CategoryId, p.ImageUrl, p.IsActive);
}

}