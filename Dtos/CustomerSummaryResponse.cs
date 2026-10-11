using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record CustomerSummaryResponse(Guid Id, string? FullName, string WhatsAppNumber, int OrderCount,
    decimal TotalSpent, DateTime? LastOrderAt);

}
