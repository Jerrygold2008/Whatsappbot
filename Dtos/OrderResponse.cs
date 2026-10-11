using System;
using System.Collections.Generic;
using SellFlowApi.Enums;

namespace SellFlowApi.Dtos
{
    public record OrderResponse(
        Guid Id,
        string OrderNumber,
        string? CustomerName,
        string CustomerPhone,
        decimal Subtotal,
        decimal DeliveryFee,
        decimal Total,
        PaymentMethod PaymentMethod,
        PaymentStatus PaymentStatus,
        OrderStatus Status,
        DateTime CreatedAt,
        List<OrderItemResponse> Items,
        DeliveryResponse? Delivery,
        int ProofCount
    );
}
