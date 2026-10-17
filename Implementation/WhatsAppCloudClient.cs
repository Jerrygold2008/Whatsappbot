using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{

public class WhatsAppCloudClient(HttpClient http, ITokenEncryptor crypto, ILogger<WhatsAppCloudClient> log) : IWhatsAppClient
{
    private const long MaxMediaBytes = 10 * 1024 * 1024;

    public Task<bool> SendTextAsync(WhatsAppAccount account, string to, string text) =>
        PostAsync(account, new { messaging_product = "whatsapp", to, type = "text", text = new { body = text, preview_url = false } });

    public Task<bool> SendButtonsAsync(WhatsAppAccount account, string to, string body, params (string Id, string Title)[] buttons) =>
        SendActionCardAsync(account, to, body, null, null, buttons);

    public Task<bool> SendActionCardAsync(WhatsAppAccount account, string to, string body, string? footer,
        string? headerImageMediaId, params (string Id, string Title)[] buttons)
    {
        var interactive = new Dictionary<string, object>
        {
            ["type"] = "button",
            ["body"] = new { text = Trim(body, 1024) },
            ["action"] = new
            {
                buttons = buttons.Take(3).Select(b => new { type = "reply", reply = new { id = b.Id, title = Trim(b.Title, 20) } })
            }
        };
        if (!string.IsNullOrWhiteSpace(footer)) interactive["footer"] = new { text = Trim(footer!, 60) };
        if (!string.IsNullOrWhiteSpace(headerImageMediaId))
            interactive["header"] = new { type = "image", image = new { id = headerImageMediaId } };

        return PostAsync(account, new { messaging_product = "whatsapp", to, type = "interactive", interactive });
    }

    public Task<bool> SendTemplateAsync(WhatsAppAccount account, string to, string template, string language, params string[] bodyParams) =>
        PostAsync(account, new
        {
            messaging_product = "whatsapp", to, type = "template",
            template = new
            {
                name = template,
                language = new { code = language },
                components = bodyParams.Length == 0 ? Array.Empty<object>() : new object[]
                {
                    new { type = "body", parameters = bodyParams.Select(p => new { type = "text", text = p }) }
                }
            }
        });

    public async Task<(byte[] Data, string Mime)?> DownloadMediaAsync(WhatsAppAccount account, string mediaId)
    {
        var token = crypto.Decrypt(account.EncryptedAccessToken);

        // 1. Resolve the short-lived media URL (expires after ~5 minutes; never persist it)
        using var metaReq = new HttpRequestMessage(HttpMethod.Get, mediaId);
        metaReq.Headers.Authorization = new("Bearer", token);
        using var metaRes = await http.SendAsync(metaReq);
        if (!metaRes.IsSuccessStatusCode)
        {
            log.LogWarning("WhatsApp media lookup failed {Status}: {Body}", (int)metaRes.StatusCode, await metaRes.Content.ReadAsStringAsync());
            return null;
        }
        using var meta = JsonDocument.Parse(await metaRes.Content.ReadAsStringAsync());
        var url = meta.RootElement.GetProperty("url").GetString()!;
        var mime = meta.RootElement.TryGetProperty("mime_type", out var m) ? m.GetString() ?? "image/jpeg" : "image/jpeg";
        if (meta.RootElement.TryGetProperty("file_size", out var size) && size.TryGetInt64(out var bytes) && bytes > MaxMediaBytes)
        {
            log.LogWarning("WhatsApp media {MediaId} too large ({Bytes} bytes)", mediaId, bytes);
            return null;
        }

        // 2. Download the bytes (same bearer token, bounded read)
        using var fileReq = new HttpRequestMessage(HttpMethod.Get, url);
        fileReq.Headers.Authorization = new("Bearer", token);
        using var fileRes = await http.SendAsync(fileReq, HttpCompletionOption.ResponseHeadersRead);
        if (!fileRes.IsSuccessStatusCode) return null;

        await using var stream = await fileRes.Content.ReadAsStreamAsync();
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (ms.Length > MaxMediaBytes) return null;
        }
        return (ms.ToArray(), mime);
    }

    public async Task<string?> UploadMediaAsync(WhatsAppAccount account, byte[] data, string mime)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("whatsapp"), "messaging_product");
        form.Add(new StringContent(mime), "type");
        var file = new ByteArrayContent(data);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(file, "file", mime.Contains("pdf") ? "receipt.pdf" : "receipt.jpg");

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{account.PhoneNumberId}/media") { Content = form };
        req.Headers.Authorization = new("Bearer", crypto.Decrypt(account.EncryptedAccessToken));
        using var res = await http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            log.LogWarning("WhatsApp media upload failed {Status}: {Body}", (int)res.StatusCode, body);
            return null;
        }
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString();
    }

    private async Task<bool> PostAsync(WhatsAppAccount account, object payload)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{account.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Authorization = new("Bearer", crypto.Decrypt(account.EncryptedAccessToken));

        using var res = await http.SendAsync(req);
        if (res.IsSuccessStatusCode) return true;
        log.LogWarning("WhatsApp send failed {Status}: {Body}", (int)res.StatusCode, await res.Content.ReadAsStringAsync());
        return false;
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

        public Task<bool> SendProductCardAsync(WhatsAppAccount account, string to, string body, string? imageUrl, params (string Id, string Title)[] buttons)
        {
            throw new NotImplementedException();
        }
    }
}
