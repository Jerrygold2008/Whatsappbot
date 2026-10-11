using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record ProductRequest(string Name, string? Description, decimal Price, int StockQuantity,
    Guid? CategoryId, string? ImageUrl, bool IsActive = true);
}
