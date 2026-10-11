using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class ChatContext
    {
      public List<Guid> ProductIds { get; set; } = [];      // numbered list shown to the customer
    public List<Guid> OrderIds { get; set; } = [];
    public Guid? ProductId { get; set; }
    public int Quantity { get; set; }
    public PaymentMethod? Method { get; set; }
    public int AddressStep { get; set; }
    public CustomerAddress Draft { get; set; } = new();
    }
}