using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ResetPasswordDTO
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; }
        public bool IsSuccess { get; set; }
    }
}