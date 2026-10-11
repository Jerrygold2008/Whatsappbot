using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record BusinessResponse(Guid Id, string Name, string? Description, string? WhatsAppNumber,
    bool WhatsAppConnected, UpdateSettingsRequest Settings);

}
