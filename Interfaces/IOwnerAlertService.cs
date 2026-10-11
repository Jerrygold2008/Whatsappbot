using System;
using System.Threading.Tasks;
using SellFlowApi.Models;

namespace SellFlowApi.Interfaces
{
/// <summary>Sends alerts and action buttons to the business owner's personal WhatsApp number.</summary>
public interface IOwnerAlertService
{
    Task NotifyNewOrderAsync(Guid orderId);
    Task NotifyPaymentProofAsync(Guid orderId, Guid proofId);
    Task NotifyOrderUpdatedAsync(Guid orderId, string changedBy);
    Task SendLowStockReminderAsync(Guid businessId);
    Task SendPendingActionsAsync(Guid businessId);
    Task SendOwnerTextAsync(Guid businessId, string text);
}

/// <summary>Handles button taps and commands coming from the owner's WhatsApp number.</summary>
public interface IOwnerCommandHandler
{
    Task HandleAsync(WhatsAppAccount account, BusinessSettings settings, string input);
}
}
