using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class PromotionDTO : IValidatableObject
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
        [StringLength(50)]
        public string PromoCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
        public string DiscountType { get; set; } = "Percent"; // Percent hoặc FixedAmount

        [Required(ErrorMessage = "Vui lòng nhập mức giảm giá")]
        public decimal DiscountValue { get; set; }

        public decimal MinOrderAmount { get; set; } = 0;
        public decimal? MaxDiscountAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);
        
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        
        public bool IsOngoing => IsActive && StartDate <= DateTime.Now && EndDate >= DateTime.Now;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate > EndDate)
            {
                yield return new ValidationResult("Ngày bắt đầu không được lớn hơn ngày kết thúc.", new[] { nameof(StartDate), nameof(EndDate) });
            }
        }
    }
}