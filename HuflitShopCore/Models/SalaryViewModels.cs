using System.Collections.Generic;

namespace HuflitShopCore.Models
{
    public class SalaryViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public double TotalHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalSalary { get; set; } // Lương cơ bản theo giờ làm việc
        public decimal TotalAllowances { get; set; } // Tổng các khoản thưởng thêm
        public decimal TotalDeductions { get; set; } // Tổng các khoản phạt khấu trừ
        public decimal NetSalary { get; set; } // Lương thực nhận = TotalSalary + TotalAllowances - TotalDeductions
        
        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
        public List<SalaryAdjustment> Adjustments { get; set; } = new List<SalaryAdjustment>(); // Các khoản thưởng/phạt chi tiết
    }

    public class AdminSalaryStatisticViewModel
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string StaffUserName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalSalary { get; set; } // Lương cơ bản theo giờ
        public decimal Allowances { get; set; } // Tổng thưởng
        public decimal Deductions { get; set; } // Tổng phạt
        public decimal NetSalary { get; set; } // Lương thực nhận (Net)
    }
}
