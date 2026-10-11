using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Dtos
{
  public record UpdateOrderStatusRequest(OrderStatus Status);
}
