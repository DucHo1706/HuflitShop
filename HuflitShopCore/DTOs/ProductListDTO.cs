namespace HuflitShopCore.DTOs
{
    public class ProductListDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageDefault { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}