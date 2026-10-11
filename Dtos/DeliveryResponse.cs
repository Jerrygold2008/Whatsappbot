using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Dtos
{
   public record DeliveryResponse(string RecipientName, string Phone, string State, string City,
    string Address, string? Landmark, OrderStatus Status);

}
