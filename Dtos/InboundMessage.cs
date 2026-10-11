using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Dtos
{
   public record InboundMessage(string PhoneNumberId, string From, string MessageId, string? Text, string? ReplyId,
    string? MediaId = null, string? MediaMime = null)
{
    // Interactive button/list replies win over typed text
    public string Input => (ReplyId ?? Text ?? "").Trim();
}

}
