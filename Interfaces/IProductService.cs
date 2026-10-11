using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
    
public interface IProductService
{
    Task<List<ProductResponse>> ListAsync(Guid businessId, bool includeInactive);
    Task<ProductResponse> GetAsync(Guid businessId, Guid id);
    Task<ProductResponse> CreateAsync(Guid businessId, ProductRequest request);
    Task<ProductResponse> UpdateAsync(Guid businessId, Guid id, ProductRequest request);
    Task DeactivateAsync(Guid businessId, Guid id);
    Task<ProductResponse> SetImageAsync(Guid businessId, Guid id, string imageUrl);
}

}