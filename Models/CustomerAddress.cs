using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class CustomerAddress
    {
       public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string RecipientName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string State { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Landmark { get; set; }
    public bool IsDefault { get; set; }
}
    }
