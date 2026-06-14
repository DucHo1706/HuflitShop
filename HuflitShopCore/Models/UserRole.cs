using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("UserRoles")]
    public class UserRole
    {
        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleId { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Role Role { get; set; }
    }
}