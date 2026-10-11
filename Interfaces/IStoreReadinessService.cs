using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Dtos;

namespace SellFlowApi.Interfaces
{
    
public interface IStoreReadinessService
{
    Task<Readiness> CheckAsync(Guid businessId);
}
}