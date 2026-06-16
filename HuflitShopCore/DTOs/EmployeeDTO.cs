using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class EmployeeDTO : IValidatableObject
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

        // Địa chỉ chi tiết
        public string? City { get; set; }
        public string? District { get; set; }
        public string? SpecificAddress { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 1. Kiểm tra ngày sinh (DateOfBirth)
            if (DateOfBirth.HasValue)
            {
                if (DateOfBirth.Value.Date > DateTime.Today)
                {
                    yield return new ValidationResult("Ngày sinh không được ở tương lai.", new[] { nameof(DateOfBirth) });
                }
                else if (DateOfBirth.Value.Date < DateTime.Today.AddYears(-120))
                {
                    yield return new ValidationResult("Ngày sinh không hợp lệ (vượt quá 120 tuổi).", new[] { nameof(DateOfBirth) });
                }
            }

            // 2. Kiểm tra thông tin địa chỉ nếu có điền bất kỳ trường nào
            if (!string.IsNullOrWhiteSpace(City) || !string.IsNullOrWhiteSpace(District) || !string.IsNullOrWhiteSpace(SpecificAddress))
            {
                if (string.IsNullOrWhiteSpace(City))
                {
                    yield return new ValidationResult("Vui lòng chọn/nhập Tỉnh/Thành phố.", new[] { nameof(City) });
                }
                if (string.IsNullOrWhiteSpace(District))
                {
                    yield return new ValidationResult("Vui lòng chọn/nhập Quận/Huyện.", new[] { nameof(District) });
                }
                if (string.IsNullOrWhiteSpace(SpecificAddress))
                {
                    yield return new ValidationResult("Vui lòng nhập Địa chỉ cụ thể.", new[] { nameof(SpecificAddress) });
                }
            }
        }
    }
}