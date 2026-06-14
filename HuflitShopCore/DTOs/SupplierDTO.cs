using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class SupplierDTO
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ContactName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}