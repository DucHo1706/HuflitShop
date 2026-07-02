using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class StaffShiftController : Controller
    {
        private readonly AppDbContext _context;

        public StaffShiftController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsStaff()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin") ||
                   User.IsInRole("2") || User.IsInRole("ROLE-EMPLOYEE") || User.IsInRole("Employee");
        }

        // GET: StaffShift
        public async Task<IActionResult> Index()
        {
            if (!IsStaff()) return Forbid();

            var shifts = await _context.Shifts.Where(s => s.IsActive).ToListAsync();
            ViewBag.Shifts = shifts;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            var myRegistrations = await _context.ShiftRegistrations
                .Include(r => r.Shift)
                .Where(r => r.StaffId == userId && r.RegistrationDate >= DateTime.Today)
                .OrderBy(r => r.RegistrationDate)
                .ThenBy(r => r.Shift.StartTime)
                .ToListAsync();

            // Xóa phần lấy pastRegistrations vì user không cần hiển thị lịch sử nữa

            return View(myRegistrations);
        }

        // POST: StaffShift/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DateTime date, int shiftId)
        {
            if (!IsStaff()) return Forbid();

            if (date.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Không thể đăng ký ca làm trong quá khứ.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            // Check if already registered and not rejected
            var exists = await _context.ShiftRegistrations.AnyAsync(r => r.StaffId == userId && r.RegistrationDate.Date == date.Date && r.ShiftId == shiftId && r.Status != "Rejected");
            if (exists)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký (hoặc đã được duyệt) ca này vào ngày này rồi.";
                return RedirectToAction(nameof(Index));
            }

            var registration = new ShiftRegistration
            {
                StaffId = userId,
                ShiftId = shiftId,
                RegistrationDate = date.Date,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.ShiftRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký ca làm thành công! Đang chờ quản lý duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // POST: StaffShift/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var reg = await _context.ShiftRegistrations.FindAsync(id);
            if (reg == null || reg.StaffId != userId) return NotFound();

            if (reg.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Không thể hủy ca đã được duyệt. Vui lòng xin phép quản lý.";
                return RedirectToAction(nameof(Index));
            }

            _context.ShiftRegistrations.Remove(reg);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã hủy đăng ký ca làm.";
            return RedirectToAction(nameof(Index));
        }
    }
}
