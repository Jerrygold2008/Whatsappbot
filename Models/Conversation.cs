using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class Conversation
    {
     public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public ConversationState State { get; set; } = ConversationState.MainMenu;
    public string ContextJson { get; set; } = "{}";       // product list, chosen product, qty, address draft
    public int InvalidInputCount { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCustomerMessageAt { get; set; }  // drives the 24h free-form window
}

    }
