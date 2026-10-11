using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class SubscriptionPlan
    {
     public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Price { get; set; }                       // naira: 200 for the test plan
    public int DurationInDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    }
}