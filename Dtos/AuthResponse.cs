using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
    public record AuthResponse
    (string Token, Guid? BusinessId, string Email);
}
