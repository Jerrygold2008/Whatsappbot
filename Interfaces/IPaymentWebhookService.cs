using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Interfaces
{
   
public interface IPaymentWebhookService
{
    Task HandleAsync(string rawBody, string? signature);
}

}