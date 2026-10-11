using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record Readiness(bool IsLive, List<string> Missing);
}
