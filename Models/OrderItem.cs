using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";         // snapshot
    public decimal UnitPrice { get; set; }                   // snapshot
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    }
}