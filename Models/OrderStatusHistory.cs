using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class OrderStatusHistory
    {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public OrderStatus From { get; set; }
    public OrderStatus To { get; set; }
    public string ChangedBy { get; set; } = "system";
    public DateTime At { get; set; } = DateTime.UtcNow;
    }
}