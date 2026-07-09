using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly AppDbContext _context;

        public SalaryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SalaryViewModel?> GetEmployeeSalaryDetailAsync(string userId, int month, int year)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var attendances = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(a => a.WorkSchedule.StaffId == userId 
                            && a.WorkSchedule.WorkDate.Month == month 
                            && a.WorkSchedule.WorkDate.Year == year
                            && a.CheckOutTime != null)
                .OrderBy(a => a.WorkSchedule.WorkDate)
                .ToListAsync();

            double totalHours = attendances.Sum(a => a.CalculatedHours);
            decimal totalSalary = (decimal)totalHours * user.HourlyRate;

            // Truy vấn các khoản thưởng/phạt điều chỉnh lương
            var adjustments = await _context.SalaryAdjustments
                .Where(a => a.StaffId == userId && a.Month == month && a.Year == year)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            decimal totalAllowances = adjustments.Where(a => a.Type == "Allowance").Sum(a => a.Amount);
            decimal totalDeductions = adjustments.Where(a => a.Type == "Deduction").Sum(a => a.Amount);
            decimal netSalary = totalSalary + totalAllowances - totalDeductions;

            return new SalaryViewModel
            {
                Month = month,
                Year = year,
                TotalHours = totalHours,
                HourlyRate = user.HourlyRate,
                TotalSalary = totalSalary,
                TotalAllowances = totalAllowances,
                TotalDeductions = totalDeductions,
                NetSalary = netSalary,
                Attendances = attendances,
                Adjustments = adjustments
            };
        }

        public async Task<List<AdminSalaryStatisticViewModel>> GetAdminSalaryStatisticsAsync(int month, int year)
        {
            var staffAttendances = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Staff)
                .Where(a => a.WorkSchedule.WorkDate.Month == month 
                            && a.WorkSchedule.WorkDate.Year == year
                            && a.CheckOutTime != null)
                .ToListAsync();

            // Truy vấn tất cả điều chỉnh lương trong tháng/năm này
            var adjustments = await _context.SalaryAdjustments
                .Where(a => a.Month == month && a.Year == year)
                .ToListAsync();

            // Group attendances by StaffId
            var stats = staffAttendances
                .GroupBy(a => a.WorkSchedule.StaffId)
                .Select(g => {
                    var staff = g.First().WorkSchedule.Staff;
                    double tHours = g.Sum(x => x.CalculatedHours);
                    decimal baseSalary = (decimal)tHours * staff.HourlyRate;

                    // Lọc thưởng phạt riêng cho nhân viên này
                    var staffAdjs = adjustments.Where(a => a.StaffId == staff.Id).ToList();
                    decimal totalAllowances = staffAdjs.Where(a => a.Type == "Allowance").Sum(a => a.Amount);
                    decimal totalDeductions = staffAdjs.Where(a => a.Type == "Deduction").Sum(a => a.Amount);
                    decimal netSalary = baseSalary + totalAllowances - totalDeductions;

                    return new AdminSalaryStatisticViewModel
                    {
                        StaffId = staff.Id,
                        StaffName = staff.FullName,
                        StaffUserName = staff.UserName,
                        HourlyRate = staff.HourlyRate,
                        TotalHours = tHours,
                        TotalSalary = baseSalary,
                        Allowances = totalAllowances,
                        Deductions = totalDeductions,
                        NetSalary = netSalary
                    };
                })
                .OrderByDescending(x => x.NetSalary)
                .ToList();

            // Bổ sung những nhân viên có thưởng/phạt trong tháng nhưng không có giờ làm việc nào
            var workedStaffIds = stats.Select(s => s.StaffId).ToList();
            var nonWorkedStaffWithAdjustments = adjustments
                .Where(a => !workedStaffIds.Contains(a.StaffId))
                .GroupBy(a => a.StaffId)
                .ToList();

            foreach (var group in nonWorkedStaffWithAdjustments)
            {
                var staffId = group.Key;
                var staff = await _context.Users.FindAsync(staffId);
                if (staff != null)
                {
                    decimal totalAllowances = group.Where(a => a.Type == "Allowance").Sum(a => a.Amount);
                    decimal totalDeductions = group.Where(a => a.Type == "Deduction").Sum(a => a.Amount);
                    decimal netSalary = totalAllowances - totalDeductions;

                    stats.Add(new AdminSalaryStatisticViewModel
                    {
                        StaffId = staff.Id,
                        StaffName = staff.FullName,
                        StaffUserName = staff.UserName,
                        HourlyRate = staff.HourlyRate,
                        TotalHours = 0,
                        TotalSalary = 0,
                        Allowances = totalAllowances,
                        Deductions = totalDeductions,
                        NetSalary = netSalary
                    });
                }
            }

            return stats.OrderByDescending(x => x.NetSalary).ToList();
        }

        public async Task<(bool Success, string Message)> AddAdjustmentAsync(SalaryAdjustment adjustment)
        {
            if (adjustment == null) return (false, "Dữ liệu không hợp lệ.");
            if (string.IsNullOrWhiteSpace(adjustment.StaffId)) return (false, "Vui lòng chọn nhân viên.");
            if (adjustment.Amount <= 0) return (false, "Số tiền phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(adjustment.Reason)) return (false, "Vui lòng nhập lý do.");
            if (adjustment.Month < 1 || adjustment.Month > 12) return (false, "Tháng không hợp lệ.");

            var user = await _context.Users.FindAsync(adjustment.StaffId);
            if (user == null) return (false, "Nhân viên không tồn tại.");

            _context.SalaryAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            return (true, "Đã thêm khoản điều chỉnh lương thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAdjustmentAsync(string id)
        {
            var adj = await _context.SalaryAdjustments.FindAsync(id);
            if (adj == null) return (false, "Khoản điều chỉnh này không tồn tại.");

            _context.SalaryAdjustments.Remove(adj);
            await _context.SaveChangesAsync();

            return (true, "Đã xóa khoản điều chỉnh lương thành công.");
        }

        public async Task<List<SalaryAdjustment>> GetAdjustmentsByStaffAsync(string staffId, int month, int year)
        {
            return await _context.SalaryAdjustments
                .Where(a => a.StaffId == staffId && a.Month == month && a.Year == year)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
