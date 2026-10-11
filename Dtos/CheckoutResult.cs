using SellFlowApi.Models;

namespace SellFlowApi.Dtos
{
    // Bank is set when the customer chose bank transfer, so the bot can show the account details.
    public record CheckoutResult(Order Order, BankDetails? Bank);
}
