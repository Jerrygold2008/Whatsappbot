using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellFlowApi.Models
{
    public class BusinessSettings
    {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public bool AcceptBankTransfer { get; set; } = true;
    public bool AcceptPayOnDelivery { get; set; }
    public decimal? DeliveryFee { get; set; }                // null = not configured, 0 = free
    public decimal? MaxPodOrderValue { get; set; }
    public bool PodRequiresReturningCustomer { get; set; }
 
    // Bank account shown to customers who choose "Pay now" (manual bank transfer)
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
 
    // Business owner's personal WhatsApp number (digits only, e.g. 2348012345678).
    // Receives payment-proof, new-order and low-stock alerts with action buttons.
    public string? OwnerWhatsAppNumber { get; set; }
    public DateTime? OwnerLastMessageAt { get; set; }      // opens WhatsApp's 24h free-form window

    public bool HasBankDetails =>
        !string.IsNullOrWhiteSpace(BankName) && !string.IsNullOrWhiteSpace(AccountNumber) && !string.IsNullOrWhiteSpace(AccountName);
    }
}