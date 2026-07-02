using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly AppDbContext _context;

        public SalaryController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsStaff()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin") ||
                   User.IsInRole("2") || User.IsInRole("ROLE-EMPLOYEE") || User.IsInRole("Employee");
        }

        private bool IsAdmin()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin");
        }

        // GET: Admin/Salary/MySalary
        // For Employees to see their own salary
        public async Task<IActionResult> MySalary(int? month, int? year)
        {
            if (!IsStaff()) return Forbid();

            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            var attendances = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(a => a.WorkSchedule.StaffId == userId 
                            && a.WorkSchedule.WorkDate.Month == selectedMonth 
                            && a.WorkSchedule.WorkDate.Year == selectedYear
                            && a.CheckOutTime != null)
                .OrderBy(a => a.WorkSchedule.WorkDate)
                .ToListAsync();

            double totalHours = attendances.Sum(a => a.CalculatedHours);
            decimal totalSalary = (decimal)totalHours * user.HourlyRate;

            var viewModel = new SalaryViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                TotalHours = totalHours,
                HourlyRate = user.HourlyRate,
                TotalSalary = totalSalary,
                Attendances = attendances
            };

            return View(viewModel);
        }

        // GET: Admin/Salary/Index
        // For Admins to see salary statistics for all employees
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> Index(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            // Fetch all users with Staff roles
            // Instead of querying Identity roles manually, we can just grab all users who have an HourlyRate or assume we check their role.
            // Since we know "2", "ROLE-EMPLOYEE", "Employee" is the staff role, let's just query users who are employees
            // In ASP.NET Core Identity, to get users in role, we usually inject UserManager. 
            // For simplicity with DbContext, we'll fetch all users except admins, or just those who have WorkSchedules in this month.
            // A more robust way is to fetch users who actually worked this month.

            var staffAttendances = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Staff)
                .Where(a => a.WorkSchedule.WorkDate.Month == selectedMonth 
                            && a.WorkSchedule.WorkDate.Year == selectedYear
                            && a.CheckOutTime != null)
                .ToListAsync();

            var stats = staffAttendances
                .GroupBy(a => a.WorkSchedule.StaffId)
                .Select(g => {
                    var staff = g.First().WorkSchedule.Staff;
                    double tHours = g.Sum(x => x.CalculatedHours);
                    return new AdminSalaryStatisticViewModel
                    {
                        StaffId = staff.Id,
                        StaffName = staff.FullName,
                        StaffUserName = staff.UserName,
                        HourlyRate = staff.HourlyRate,
                        TotalHours = tHours,
                        TotalSalary = (decimal)tHours * staff.HourlyRate
                    };
                })
                .OrderByDescending(x => x.TotalSalary)
                .ToList();

            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(stats);
        }
    }
}
