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
  
public class CategoryService(SellFlowApiDbContext db) : ICategoryService
{
    public async Task<List<CategoryResponse>> ListAsync(Guid businessId)
    {
        var list = await db.Categories.AsNoTracking().Where(c => c.BusinessId == businessId).OrderBy(c => c.Name).ToListAsync();
        return list.Select(c => new CategoryResponse(c.Id, c.Name)).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(Guid businessId, CategoryRequest r)
    {
        var name = Validate(r);
        if (await db.Categories.AnyAsync(c => c.BusinessId == businessId && c.Name.ToLower() == name.ToLower()))
            throw new DomainException("That category already exists.");
        var c = new Category { BusinessId = businessId, Name = name };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return new CategoryResponse(c.Id, c.Name);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid businessId, Guid id, CategoryRequest r)
    {
        var c = await Find(businessId, id);
        c.Name = Validate(r);
        await db.SaveChangesAsync();
        return new CategoryResponse(c.Id, c.Name);
    }

    public async Task DeleteAsync(Guid businessId, Guid id)
    {
        var c = await Find(businessId, id);
        await db.Products.Where(p => p.CategoryId == id && p.BusinessId == businessId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CategoryId, (Guid?)null));   // products keep existing, just uncategorised
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
    }

    private async Task<Category> Find(Guid businessId, Guid id) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId)
        ?? throw new NotFoundException("Category not found.");

    private static string Validate(CategoryRequest r) =>
        string.IsNullOrWhiteSpace(r.Name) ? throw new DomainException("Category name is required.") : r.Name.Trim();
}

}