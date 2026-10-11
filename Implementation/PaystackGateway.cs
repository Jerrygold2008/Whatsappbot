using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Implementation
{
   
// Paystack works in KOBO. Convert only here; the rest of the app uses naira.
public class PaystackGateway(HttpClient http, IConfiguration cfg) : IPaymentGateway
{
    public async Task<string> InitializeAsync(string reference, decimal amountNaira, string email, string? subaccount)
    {
        var body = new Dictionary<string, object>
        {
            ["email"] = email,
            ["amount"] = (long)Math.Round(amountNaira * 100m),
            ["reference"] = reference,
            ["currency"] = "NGN"
        };
        if (!string.IsNullOrWhiteSpace(subaccount)) body["subaccount"] = subaccount;   // settles to the merchant

        using var req = Request(HttpMethod.Post, "transaction/initialize");
        req.Content = JsonContent.Create(body);
        using var res = await http.SendAsync(req);
        if (!res.IsSuccessStatusCode) throw new DomainException("Could not start the payment. Please try again.");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("authorization_url").GetString()!;
    }

    public async Task<(bool Success, decimal AmountNaira)> VerifyAsync(string reference)
    {
        using var req = Request(HttpMethod.Get, $"transaction/verify/{Uri.EscapeDataString(reference)}");
        using var res = await http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return (false, 0m);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        var ok = data.GetProperty("status").GetString() == "success";
        return (ok, data.GetProperty("amount").GetInt64() / 100m);
    }

    private HttpRequestMessage Request(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new("Bearer", cfg["Paystack:SecretKey"]);
        return req;
    }
}

}