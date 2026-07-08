using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class StaffShiftController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<HuflitShopCore.Hubs.ChatHub> _hubContext;

        public StaffShiftController(AppDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<HuflitShopCore.Hubs.ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private bool IsStaff()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin") ||
                   User.IsInRole("2") || User.IsInRole("ROLE-EMPLOYEE") || User.IsInRole("Employee");
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: StaffShift
        public async Task<IActionResult> Index(DateTime? startOfWeek)
        {
            if (!IsStaff()) return Forbid();

            DateTime today = DateTime.Today;
            DateTime monday;
            if (startOfWeek.HasValue)
            {
                monday = startOfWeek.Value.Date;
                int diff = (7 + (monday.DayOfWeek - DayOfWeek.Monday)) % 7;
                monday = monday.AddDays(-1 * diff).Date;
            }
            else
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                monday = today.AddDays(-1 * diff).Date;
            }
            DateTime sunday = monday.AddDays(6);

            ViewBag.Monday = monday;
            ViewBag.Sunday = sunday;
            ViewBag.Today = today;

            var shifts = await _context.Shifts.Where(s => s.IsActive).OrderBy(s => s.StartTime).ToListAsync();
            ViewBag.Shifts = shifts;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            var myRegistrations = await _context.ShiftRegistrations
                .Include(r => r.Shift)
                .Where(r => r.StaffId == userId && r.RegistrationDate >= monday && r.RegistrationDate <= sunday)
                .ToListAsync();

            var workSchedules = await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.StaffId == userId && w.WorkDate >= monday && w.WorkDate <= sunday)
                .ToListAsync();

            ViewBag.WorkSchedules = workSchedules;

            return View(myRegistrations);
        }

        // POST: StaffShift/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DateTime date, int shiftId, DateTime? startOfWeek)
        {
            if (!IsStaff()) return Forbid();

            if (date.Date < DateTime.Today)
            {
                string errMsg = "Không thể đăng ký ca làm trong quá khứ.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["ErrorMessage"] = errMsg;
                return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            // Check if already registered and not rejected
            var exists = await _context.ShiftRegistrations.AnyAsync(r => r.StaffId == userId && r.RegistrationDate.Date == date.Date && r.ShiftId == shiftId && r.Status != "Rejected");
            if (exists)
            {
                string errMsg = "Bạn đã đăng ký (hoặc đã được duyệt) ca này vào ngày này rồi.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["ErrorMessage"] = errMsg;
                return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
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

            // Real-time Notification
            try
            {
                var staffUser = await _context.Users.FindAsync(userId);
                var staffName = staffUser?.FullName ?? staffUser?.UserName ?? "Nhân viên";
                var shift = await _context.Shifts.FindAsync(shiftId);
                var shiftName = shift?.Name ?? "Ca làm";
                
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveSystemNotification", new
                {
                    title = "Đăng ký ca mới!",
                    message = $"Nhân viên {staffName} vừa đăng ký {shiftName} ngày {date.ToString("dd/MM/yyyy")}.",
                    icon = "info"
                });
            }
            catch { /* Ignore error */ }

            string successMsg = "Đăng ký ca làm thành công! Đang chờ quản lý duyệt.";
            if (IsAjaxRequest())
            {
                return Json(new { 
                    success = true, 
                    message = successMsg, 
                    registrationId = registration.Id,
                    formattedDate = dayOfWeekIndex(date) // dynamic rendering helper
                });
            }
            TempData["SuccessMessage"] = successMsg;
            return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
        }

        private int dayOfWeekIndex(DateTime date)
        {
            // Monday -> 0, Sunday -> 6
            int val = (int)date.DayOfWeek;
            return val == 0 ? 6 : val - 1;
        }

        // POST: StaffShift/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, DateTime? startOfWeek)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var reg = await _context.ShiftRegistrations.FindAsync(id);
            if (reg == null || reg.StaffId != userId) return NotFound();

            if (reg.Status == "Approved")
            {
                string errMsg = "Không thể hủy ca đã được duyệt. Vui lòng xin phép quản lý.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["ErrorMessage"] = errMsg;
                return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
            }

            _context.ShiftRegistrations.Remove(reg);
            await _context.SaveChangesAsync();
            
            string successMsg = "Đã hủy đăng ký ca làm.";
            if (IsAjaxRequest()) return Json(new { success = true, message = successMsg });
            TempData["SuccessMessage"] = successMsg;
            return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
        }
    }
}
