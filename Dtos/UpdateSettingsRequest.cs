using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace SellFlowApi.Dtos
{
public record UpdateSettingsRequest(bool AcceptBankTransfer, bool AcceptPayOnDelivery, decimal? DeliveryFee,
    decimal? MaxPodOrderValue, bool PodRequiresReturningCustomer, string? BankName, string? AccountNumber, string? AccountName,
    string? OwnerWhatsAppNumber = null);
}
