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
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
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

            // Lấy ca làm của hôm nay, và ca qua đêm của hôm qua (để sáng hôm sau vẫn check-out được)
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var workSchedules = await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.StaffId == userId && 
                       (w.WorkDate.Date == today || 
                       (w.WorkDate.Date == yesterday && w.Shift.EndTime < w.Shift.StartTime)))
                .OrderBy(w => w.WorkDate)
                .ThenBy(w => w.Shift.StartTime)
                .ToListAsync();

            if (!workSchedules.Any())
            {
                ViewBag.HasSchedule = false;
                return View(null);
            }

            ViewBag.HasSchedule = true;

            var scheduleIds = workSchedules.Select(w => w.Id).ToList();
            var attendances = await _context.Attendances
                .Where(a => scheduleIds.Contains(a.WorkScheduleId))
                .ToListAsync();

            // Tìm ca đang diễn ra (đã check-in nhưng chưa check-out)
            var activeAttendance = attendances.FirstOrDefault(a => a.CheckOutTime == null);
            WorkSchedule activeSchedule = null;

            if (activeAttendance != null)
            {
                activeSchedule = workSchedules.First(w => w.Id == activeAttendance.WorkScheduleId);
            }
            else
            {
                // Nếu không có ca nào đang diễn ra, tìm ca tiếp theo chưa được check-in
                activeSchedule = workSchedules.FirstOrDefault(w => !attendances.Any(a => a.WorkScheduleId == w.Id));
                
                if (activeSchedule == null)
                {
                    // Nếu tất cả các ca đều đã check-out, hiển thị ca cuối cùng
                    activeSchedule = workSchedules.Last();
                    activeAttendance = attendances.FirstOrDefault(a => a.WorkScheduleId == activeSchedule.Id);
                }
            }

            ViewBag.WorkSchedule = activeSchedule;

            // Lấy lịch sử chấm công 30 ngày gần nhất của user
            var pastAttendances = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(a => a.WorkSchedule.StaffId == userId && a.CheckOutTime != null)
                .OrderByDescending(a => a.CheckInTime)
                .Take(30)
                .ToListAsync();
            
            ViewBag.PastAttendances = pastAttendances;

            return View(activeAttendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int workScheduleId)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var workSchedule = await _context.WorkSchedules
                .Include(w => w.Shift)
                .FirstOrDefaultAsync(w => w.Id == workScheduleId && w.StaffId == userId);

            if (workSchedule == null)
            {
                TempData["ErrorMessage"] = "Lịch làm việc không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var today = DateTime.Today;
            var isToday = workSchedule.WorkDate.Date == today;
            var isYesterdayOvernight = workSchedule.WorkDate.Date == today.AddDays(-1) 
                                       && workSchedule.Shift.EndTime < workSchedule.Shift.StartTime;

            if (!isToday && !isYesterdayOvernight)
            {
                TempData["ErrorMessage"] = "Lịch làm việc không hợp lệ (đã quá hạn hoặc chưa tới).";
                return RedirectToAction(nameof(Index));
            }

            var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a => a.WorkScheduleId == workScheduleId);
            if (existingAttendance != null)
            {
                TempData["ErrorMessage"] = "Bạn đã check-in rồi.";
                return RedirectToAction(nameof(Index));
            }

            // Verify IP
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var shopSetting = await _context.ShopSettings.FirstOrDefaultAsync(s => s.Key == "ShopWifiIpAddress");

            if (shopSetting != null && !string.IsNullOrEmpty(shopSetting.Value))
            {
                if (clientIp != shopSetting.Value && clientIp != "::1" && clientIp != "127.0.0.1") // Ignore localhost for testing if needed
                {
                    TempData["ErrorMessage"] = $"Lỗi: IP mạng của bạn ({clientIp}) không khớp với WiFi cửa hàng. Vui lòng kết nối WiFi quán để chấm công.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var now = DateTime.Now;
            var status = "Present";
            
            // Check late using exact Date + Time
            var shiftStartTime = workSchedule.WorkDate.Date.Add(workSchedule.Shift.StartTime);
            if (now > shiftStartTime)
            {
                status = "Late";
            }

            var attendance = new Attendance
            {
                WorkScheduleId = workSchedule.Id,
                CheckInTime = now,
                CheckInIpAddress = clientIp,
                Status = status
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Check-in thành công lúc " + now.ToString("HH:mm");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int attendanceId)
        {
            if (!IsStaff()) return Forbid();

            var attendance = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu chấm công.";
                return RedirectToAction(nameof(Index));
            }

            if (attendance.CheckOutTime.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn đã check-out rồi.";
                return RedirectToAction(nameof(Index));
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var shopSetting = await _context.ShopSettings.FirstOrDefaultAsync(s => s.Key == "ShopWifiIpAddress");

            if (shopSetting != null && !string.IsNullOrEmpty(shopSetting.Value))
            {
                if (clientIp != shopSetting.Value && clientIp != "::1" && clientIp != "127.0.0.1")
                {
                    TempData["ErrorMessage"] = $"Lỗi: IP mạng của bạn ({clientIp}) không khớp với WiFi cửa hàng. Vui lòng kết nối WiFi quán để check-out.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var now = DateTime.Now;
            attendance.CheckOutTime = now;
            attendance.CheckOutIpAddress = clientIp;

            // Tính toán giờ làm thực tế, giới hạn trong thời gian ca làm
            var shiftDate = attendance.WorkSchedule.WorkDate.Date;
            var shiftStartTime = shiftDate.Add(attendance.WorkSchedule.Shift.StartTime);
            var shiftEndTime = shiftDate.Add(attendance.WorkSchedule.Shift.EndTime);
            
            // Xử lý ca làm qua đêm (nếu có)
            if (attendance.WorkSchedule.Shift.EndTime < attendance.WorkSchedule.Shift.StartTime)
            {
                shiftEndTime = shiftEndTime.AddDays(1);
            }

            var checkInTime = attendance.CheckInTime.Value;
            
            // Phạt đi trễ
            var lateMinutes = (checkInTime - shiftStartTime).TotalMinutes;
            double penaltyHours = 0;
            
            if (lateMinutes > 0)
            {
                if (lateMinutes <= 10)
                {
                    penaltyHours = 0.25; // Phạt 15p
                    attendance.Note = "Đi trễ 1-10p, phạt 15p.";
                }
                else
                {
                    penaltyHours = lateMinutes / 60.0;
                    attendance.Note = $"Đi trễ {Math.Round(lateMinutes)}p.";
                }
            }

            // Chỉ tính giờ từ khi bắt đầu ca (không tính giờ check-in sớm)
            var effectiveCheckIn = checkInTime > shiftStartTime ? checkInTime : shiftStartTime;
            
            // Chỉ tính giờ đến khi kết thúc ca (không tính giờ nán lại sau ca)
            var effectiveCheckOut = now < shiftEndTime ? now : shiftEndTime;

            var totalWorkingHours = (effectiveCheckOut - effectiveCheckIn).TotalHours;
            if (totalWorkingHours < 0) totalWorkingHours = 0;

            attendance.CalculatedHours = Math.Max(0, totalWorkingHours - penaltyHours);

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Check-out thành công lúc " + now.ToString("HH:mm");
            return RedirectToAction(nameof(Index));
        }
    }
}
