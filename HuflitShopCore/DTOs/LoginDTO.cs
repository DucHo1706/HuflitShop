using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}