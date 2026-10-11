using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class Payment
    {
     public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public PaymentPurpose Purpose { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? PlanId { get; set; }                     // for subscription payments
    public string Reference { get; set; } = "";           // unique
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string Provider { get; set; } = "paystack";
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}