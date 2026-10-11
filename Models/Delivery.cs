using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class Delivery
    {
     public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string RecipientName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string State { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Landmark { get; set; }
    public decimal DeliveryFee { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    }
}