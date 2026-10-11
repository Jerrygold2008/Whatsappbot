using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Enums;

namespace SellFlowApi.Models
{
    public class Message
    {
  public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public SenderType Sender { get; set; }
    public string Content { get; set; } = "";
    public string? WhatsAppMessageId { get; set; }        // unique when not null
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}