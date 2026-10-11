using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Dtos
{
  public record PaymentResponse(string Reference, string? OrderNumber, decimal Amount, PaymentStatus Status,
    PaymentMethod? Method, DateTime? PaidAt, DateTime CreatedAt);
}
    