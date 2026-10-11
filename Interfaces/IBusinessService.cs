using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
   
public interface IBusinessService
{
    Task<BusinessResponse> CreateAsync(Guid userId, CreateBusinessRequest request);
    Task<BusinessResponse> GetAsync(Guid businessId);
    Task<BusinessResponse> UpdateAsync(Guid businessId, UpdateBusinessRequest request);
    Task<BusinessResponse> UpdateSettingsAsync(Guid businessId, UpdateSettingsRequest request);
}
}