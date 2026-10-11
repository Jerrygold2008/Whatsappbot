namespace SellFlowApi.Interfaces
{
public interface INotificationService
{
    Task NotifyOrderStatusAsync(Guid orderId);
    Task NotifyPaymentRejectedAsync(Guid orderId, string? reason);
}
}
