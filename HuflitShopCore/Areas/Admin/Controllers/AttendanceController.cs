using System;
using System.Linq;
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
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        private bool IsStaff()
        {
            return User.IsInRole("1") || User.IsInRole("ROLE-ADMIN") || User.IsInRole("Admin") ||
                   User.IsInRole("2") || User.IsInRole("ROLE-EMPLOYEE") || User.IsInRole("Employee");
        }

        // GET: Admin/Attendance
        public async Task<IActionResult> Index()
        {
            if (!IsStaff()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var workSchedules = await _attendanceService.GetTodayAndYesterdayOvernightSchedulesAsync(userId);
            if (!workSchedules.Any())
            {
                ViewBag.HasSchedule = false;
                return View(null);
            }

            ViewBag.HasSchedule = true;

            var scheduleIds = workSchedules.Select(w => w.Id).ToList();
            var attendances = await _attendanceService.GetAttendancesForSchedulesAsync(scheduleIds);

            // Tìm ca đang diễn ra (đã check-in nhưng chưa check-out)
            var activeAttendance = attendances.FirstOrDefault(a => a.CheckOutTime == null);
            WorkSchedule activeSchedule = null;

            if (activeAttendance != null)
            {
                activeSchedule = workSchedules.First(w => w.Id == activeAttendance.WorkScheduleId);
            }
            else
            {
                // Nếu không có ca nào đang diễn ra, tìm ca tiếp theo chưa check-in
                activeSchedule = workSchedules.FirstOrDefault(w => !attendances.Any(a => a.WorkScheduleId == w.Id));
                
                if (activeSchedule == null)
                {
                    // Nếu tất cả đã check-out, hiển thị ca cuối cùng
                    activeSchedule = workSchedules.Last();
                    activeAttendance = attendances.FirstOrDefault(a => a.WorkScheduleId == activeSchedule.Id);
                }
            }

            ViewBag.WorkSchedule = activeSchedule;

            // Lấy lịch sử chấm công 30 ngày gần nhất
            var pastAttendances = await _attendanceService.GetPastAttendancesAsync(userId, 30);
            ViewBag.PastAttendances = pastAttendances;

            return View(activeAttendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int workScheduleId)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _attendanceService.CheckInAsync(userId, workScheduleId, clientIp);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int attendanceId)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _attendanceService.CheckOutAsync(userId, attendanceId, clientIp);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
