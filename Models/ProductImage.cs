using System;

namespace SellFlowApi.Models
{
    public class ProductImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BusinessId { get; set; }
        public byte[] Data { get; set; } = [];
        public string ContentType { get; set; } = "image/jpeg";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
