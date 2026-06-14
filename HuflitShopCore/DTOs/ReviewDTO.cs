using System;

namespace HuflitShopCore.DTOs
{
    public class ReviewDTO
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Rate { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ProductId { get; set; } = string.Empty;
    }
}