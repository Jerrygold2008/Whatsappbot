using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
   
public interface ICategoryService
{
    Task<List<CategoryResponse>> ListAsync(Guid businessId);
    Task<CategoryResponse> CreateAsync(Guid businessId, CategoryRequest request);
    Task<CategoryResponse> UpdateAsync(Guid businessId, Guid id, CategoryRequest request);
    Task DeleteAsync(Guid businessId, Guid id);
}

}