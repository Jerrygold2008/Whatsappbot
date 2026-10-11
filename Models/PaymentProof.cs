using System;

namespace SellFlowApi.Models
{
    public class PaymentProof
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BusinessId { get; set; }
        public Guid OrderId { get; set; }
        public byte[] FileData { get; set; } = [];
        public string ContentType { get; set; } = "image/jpeg";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
