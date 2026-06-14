using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        public bool EmailSent { get; set; }
    }
}