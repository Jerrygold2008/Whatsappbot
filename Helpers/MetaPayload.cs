using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using SellFlowApi.Dtos;

namespace SellFlowApi.Helpers
{
    public static class MetaPayload
{
    public static List<InboundMessage> Parse(string json)
    {
        var result = new List<InboundMessage>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("entry", out var entries)) return result;

        foreach (var entry in entries.EnumerateArray())
        foreach (var change in entry.GetProperty("changes").EnumerateArray())
        {
            var value = change.GetProperty("value");
            if (!value.TryGetProperty("messages", out var msgs)) continue;   // status updates etc.
            var phoneId = value.GetProperty("metadata").GetProperty("phone_number_id").GetString()!;

            foreach (var m in msgs.EnumerateArray())
            {
                string? text = null, reply = null, mediaId = null, mime = null;
                var type = m.GetProperty("type").GetString();
                if (type == "text") text = m.GetProperty("text").GetProperty("body").GetString();
                else if (type == "interactive")
                {
                    var i = m.GetProperty("interactive");
                    reply = i.TryGetProperty("button_reply", out var br) ? br.GetProperty("id").GetString()
                          : i.TryGetProperty("list_reply", out var lr) ? lr.GetProperty("id").GetString() : null;
                }
                else if (type is "image" or "document")
                {
                    var media = m.GetProperty(type);
                    mediaId = media.GetProperty("id").GetString();
                    mime = media.TryGetProperty("mime_type", out var mt) ? mt.GetString() : null;
                    text = media.TryGetProperty("caption", out var cap) ? cap.GetString() : null;
                }
                else if (type == "button")      // quick-reply button on a template message
                    reply = m.GetProperty("button").TryGetProperty("payload", out var pl) ? pl.GetString() : null;
                result.Add(new InboundMessage(phoneId, m.GetProperty("from").GetString()!,
                                              m.GetProperty("id").GetString()!, text, reply, mediaId, mime));
            }
        }
        return result;
    }
}

}