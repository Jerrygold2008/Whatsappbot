using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Models;

namespace SellFlowApi.Interfaces
{
public interface IOrderService
{
    Task<CheckoutResult> CreateOrderAsync(Guid businessId, Guid customerId, Guid cartId, PaymentMethod method, CustomerAddress address);
    Task TransitionAsync(Guid businessId, Guid orderId, OrderStatus to, string changedBy);
    Task<List<OrderResponse>> ListAsync(Guid businessId, OrderStatus? status);
    Task<OrderResponse?> GetAsync(Guid businessId, Guid orderId);
    Task ConfirmPaymentAsync(Guid businessId, Guid orderId, string confirmedBy);
    Task RejectPaymentAsync(Guid businessId, Guid orderId, string? reason, string rejectedBy);
    Task<PaymentProof?> SubmitProofAsync(Guid businessId, Guid customerId, byte[] data, string contentType);
    Task<List<PaymentProofResponse>> ListProofsAsync(Guid businessId, Guid orderId);
    Task<PaymentProof> GetProofAsync(Guid businessId, Guid orderId, Guid proofId);
}
}
