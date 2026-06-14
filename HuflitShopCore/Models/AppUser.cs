using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Users")]
    public class AppUser
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(255)]
        public string FullName { get; set; }

        // Thêm trường Role để phân quyền (Customer/Admin)
        public string? Role { get; set; }

        // Thêm trường Avatar để lưu đường dẫn ảnh
        public string? Avatar { get; set; }

        [StringLength(255)]
        public string AvatarPublicId { get; set; }

        [StringLength(50)]
        public string AvatarVersion { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? Gender { get; set; } // 0: Nữ, 1: Nam, 2: Khác

        public bool IsActive { get; set; } = true;

        public DateTime JoinedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(255)]
        public string UserName { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public virtual ICollection<Address> Addresses { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}