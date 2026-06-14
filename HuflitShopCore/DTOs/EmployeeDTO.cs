using System;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class EmployeeDTO
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Tên nhân viên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; } // Dùng khi tạo mới nhân viên

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string? ConfirmPassword { get; set; }


        public string? PhoneNumber { get; set; }
        
        public int? Gender { get; set; } // 0: Nữ, 1: Nam, 2: Khác
        public string GenderName => Gender == 0 ? "Nữ" : (Gender == 1 ? "Nam" : (Gender == 2 ? "Khác" : "---"));

        public DateTime? DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        public string FullAddress { get; set; } = string.Empty;
    }
}