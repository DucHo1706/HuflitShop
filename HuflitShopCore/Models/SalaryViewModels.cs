using System.Collections.Generic;

namespace HuflitShopCore.Models
{
    public class SalaryViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public double TotalHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalSalary { get; set; }
        
        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
    }

    public class AdminSalaryStatisticViewModel
    {
        public string StaffId { get; set; }
        public string StaffName { get; set; }
        public string StaffUserName { get; set; }
        public decimal HourlyRate { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalSalary { get; set; }
    }
}
