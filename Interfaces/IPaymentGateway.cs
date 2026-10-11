using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Interfaces
{
    
public interface IPaymentGateway
{
    Task<string> InitializeAsync(string reference, decimal amountNaira, string email, string? subaccount);
    Task<(bool Success, decimal AmountNaira)> VerifyAsync(string reference);
}
}