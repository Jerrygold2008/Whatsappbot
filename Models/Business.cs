using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class Business
    {
           public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? PaystackSubaccountCode { get; set; }   // customer money goes to merchant
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BusinessSettings Settings { get; set; } = new();
    public WhatsAppAccount? WhatsApp { get; set; }
    }
}