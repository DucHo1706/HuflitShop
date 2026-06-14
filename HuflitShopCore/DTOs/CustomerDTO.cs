using System;

namespace HuflitShopCore.DTOs
{
    public class CustomerDTO
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int? Gender { get; set; } 
        public string GenderName => Gender == 0 ? "Nữ" : (Gender == 1 ? "Nam" : (Gender == 2 ? "Khác" : "---"));
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoinedDate { get; set; }
        public bool? IsActive { get; set; }
        public string FullAddress { get; set; } = string.Empty;
    }
}