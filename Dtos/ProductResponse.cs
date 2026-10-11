using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record ProductResponse(Guid Id, string Name, string? Description, decimal Price, int StockQuantity,
    int ReservedQuantity, Guid? CategoryId, string? ImageUrl, bool IsActive);
}
