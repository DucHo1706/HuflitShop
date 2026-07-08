using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HuflitShopCore.Models;
using HuflitShopCore.Services;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly ISalaryService _salaryService;

        public SalaryController(ISalaryService salaryService)
        {
            _salaryService = salaryService;
        }

        private bool IsStaff()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin") ||
                   User.IsInRole("2") || User.IsInRole("ROLE-EMPLOYEE") || User.IsInRole("Employee");
        }

        // GET: Admin/Salary/MySalary
        public async Task<IActionResult> MySalary(int? month, int? year)
        {
            if (!IsStaff()) return Forbid();

            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var viewModel = await _salaryService.GetEmployeeSalaryDetailAsync(userId, selectedMonth, selectedYear);
            if (viewModel == null) return NotFound();

            return View(viewModel);
        }

        // GET: Admin/Salary/Index
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> Index(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var stats = await _salaryService.GetAdminSalaryStatisticsAsync(selectedMonth, selectedYear);

            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(stats);
        }

        // AJAX API: Get adjustments for a staff in month/year
        [HttpGet]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> GetAdjustmentsJson(string staffId, int month, int year)
        {
            var adjustments = await _salaryService.GetAdjustmentsByStaffAsync(staffId, month, year);
            return Json(adjustments);
        }

        // AJAX API: Add an adjustment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> AddAdjustment(string staffId, string type, decimal amount, string reason, int month, int year)
        {
            var adjustment = new SalaryAdjustment
            {
                StaffId = staffId,
                Type = type,
                Amount = amount,
                Reason = reason,
                Month = month,
                Year = year,
                CreatedAt = DateTime.Now
            };

            var (success, message) = await _salaryService.AddAdjustmentAsync(adjustment);
            return Json(new { success, message });
        }

        // AJAX API: Delete an adjustment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> DeleteAdjustment(string id)
        {
            var (success, message) = await _salaryService.DeleteAdjustmentAsync(id);
            return Json(new { success, message });
        }
    }
}
