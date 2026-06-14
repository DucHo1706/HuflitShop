using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Address")]
    public class Address
    {
        [Key]
        [Column(TypeName = "varchar(50)")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        [Column(TypeName = "nvarchar(max)")]
        public string SpecificAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
        [Column(TypeName = "nvarchar(255)")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
        [Column(TypeName = "nvarchar(255)")]
        public string District { get; set; } = string.Empty;
    }
}