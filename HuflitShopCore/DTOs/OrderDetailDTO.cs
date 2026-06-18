namespace HuflitShopCore.DTOs
{
    public class OrderDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string ProductVariantId { get; set; } = string.Empty;
        
        public int Quantity { get; set; }
        public decimal PurchasedPrice { get; set; }
        
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public string SizeNameSnapshot { get; set; } = string.Empty;
        public string ColorNameSnapshot { get; set; } = string.Empty;
        
        public decimal TotalPrice => Quantity * PurchasedPrice;
        public string FullProductName => $"{ProductNameSnapshot} - {ColorNameSnapshot} - {SizeNameSnapshot}";
        public string? ProductImageUrl { get; set; }
        public string? ProductId { get; set; }
    }
}