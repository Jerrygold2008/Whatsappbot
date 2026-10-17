using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Models;

namespace SellFlowApi.Interfaces
{

public interface IWhatsAppClient
{
    Task<bool> SendTextAsync(WhatsAppAccount account, string to, string text);
    Task<bool> SendButtonsAsync(WhatsAppAccount account, string to, string body, params (string Id, string Title)[] buttons);

    /// <summary>Interactive button message with optional image header and footer.</summary>
    Task<bool> SendActionCardAsync(WhatsAppAccount account, string to, string body, string? footer,
        string? headerImageMediaId, params (string Id, string Title)[] buttons);

    /// <summary>Approved template with positional body parameters ({{1}}, {{2}}, ...).</summary>
    Task<bool> SendTemplateAsync(WhatsAppAccount account, string to, string template, string language, params string[] bodyParams);

    /// <summary>Product card: optional photo (public https link to a JPG/PNG), a text body and 1-3 tap buttons.</summary>
    Task<bool> SendProductCardAsync(WhatsAppAccount account, string to, string body, string? imageUrl,
        params (string Id, string Title)[] buttons);

    Task<(byte[] Data, string Mime)?> DownloadMediaAsync(WhatsAppAccount account, string mediaId);
    Task<string?> UploadMediaAsync(WhatsAppAccount account, byte[] data, string mime);
}
}
