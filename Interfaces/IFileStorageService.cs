using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Interfaces
{
public interface IFileStorageService
{
    Task<string> SaveProductImageAsync(Guid businessId, IFormFile file);
}
}