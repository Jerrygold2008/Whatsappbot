namespace SellFlowApi.Dtos
{ 
public record PaymentProofResponse(Guid Id, DateTime UploadedAt, string ContentType);
}