using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
    public record OrderItemResponse(string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

}
