using System;
using System.Linq;
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
    public class WorkSchedulesController : Controller
    {
        private readonly AppDbContext _context;

        public WorkSchedulesController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: Admin/WorkSchedules
        public async Task<IActionResult> Index(DateTime? startOfWeek)
        {
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

            var shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            ViewBag.Shifts = shifts;

            var schedules = await _context.WorkSchedules
                .Include(w => w.Staff)
                .Include(w => w.Shift)
                .Where(w => w.WorkDate >= monday && w.WorkDate <= sunday)
                .OrderBy(w => w.WorkDate)
                .ThenBy(w => w.Shift.StartTime)
                .ToListAsync();

            var registrations = await _context.ShiftRegistrations
                .Include(r => r.Staff)
                .Include(r => r.Shift)
                .Where(r => r.RegistrationDate >= monday && r.RegistrationDate <= sunday && r.Status == "Pending")
                .ToListAsync();
            ViewBag.Registrations = registrations;

            var scheduleIds = schedules.Select(w => w.Id).ToList();
            var attendances = await _context.Attendances
                .Where(a => scheduleIds.Contains(a.WorkScheduleId))
                .ToDictionaryAsync(a => a.WorkScheduleId);

            ViewBag.Attendances = attendances;

            // Lấy danh sách nhân viên để gán trực tiếp
            var staffList = await _context.Users
                .Where(u => u.IsActive && (u.Role == "Employee" || u.Role == "Admin" || u.Role == "ROLE-EMPLOYEE" || u.Role == "ROLE-ADMIN" || u.Role == "1" || u.Role == "2"))
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.StaffList = staffList;

            return View(schedules);
        }

        // POST: Admin/WorkSchedules/AssignStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> AssignStaff(DateTime date, int shiftId, System.Collections.Generic.List<string> staffIds, DateTime? startOfWeek)
        {
            if (staffIds == null || !staffIds.Any())
            {
                string errMsg = "Chưa chọn nhân viên nào.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["ErrorMessage"] = errMsg;
                return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
            }

            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return NotFound();

            var currentCount = await _context.WorkSchedules
                .CountAsync(w => w.WorkDate.Date == date.Date && w.ShiftId == shiftId);
            
            int addedCount = 0;
            var errors = new System.Collections.Generic.List<string>();
            var newAssignments = new System.Collections.Generic.List<object>();

            foreach (var staffId in staffIds)
            {
                var exists = await _context.WorkSchedules
                    .AnyAsync(w => w.WorkDate.Date == date.Date && w.ShiftId == shiftId && w.StaffId == staffId);
                if (exists)
                {
                    errors.Add($"Nhân viên này đã được phân ca này.");
                    continue;
                }

                if (currentCount + addedCount >= shift.MaxStaff)
                {
                    errors.Add($"Ca này đã đạt giới hạn tối đa ({shift.MaxStaff} người).");
                    break;
                }

                var ws = new WorkSchedule
                {
                    StaffId = staffId,
                    ShiftId = shiftId,
                    WorkDate = date.Date,
                    CreatedAt = DateTime.Now
                };
                _context.WorkSchedules.Add(ws);

                var reg = await _context.ShiftRegistrations
                    .FirstOrDefaultAsync(r => r.StaffId == staffId && r.ShiftId == shiftId && r.RegistrationDate.Date == date.Date && r.Status == "Pending");
                if (reg != null)
                {
                    reg.Status = "Approved";
                    _context.ShiftRegistrations.Update(reg);
                }

                addedCount++;
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                
                // Fetch newly assigned schedules for AJAX DOM updating
                var newWSs = await _context.WorkSchedules
                    .Include(w => w.Staff)
                    .Where(w => w.WorkDate.Date == date.Date && w.ShiftId == shiftId && staffIds.Contains(w.StaffId))
                    .ToListAsync();
                
                foreach (var ws in newWSs)
                {
                    newAssignments.Add(new {
                        id = ws.Id,
                        staffId = ws.StaffId,
                        fullName = ws.Staff.FullName,
                        userName = ws.Staff.UserName,
                        initials = string.IsNullOrEmpty(ws.Staff.FullName) ? "?" : ws.Staff.FullName.Substring(0, 1).ToUpper()
                    });
                }
            }

            string msg = $"Đã phân lịch thành công cho {addedCount} nhân viên.";
            if (errors.Any())
            {
                msg += " " + string.Join(" ", errors);
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = addedCount > 0, message = msg, data = newAssignments });
            }

            if (addedCount > 0)
                TempData["SuccessMessage"] = msg;
            else
                TempData["ErrorMessage"] = msg;

            return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
        }

        // POST: Admin/WorkSchedules/RemoveStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> RemoveStaff(int id, DateTime? startOfWeek)
        {
            var schedule = await _context.WorkSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            var hasRequests = await _context.StaffRequests.AnyAsync(r => r.WorkScheduleId == schedule.Id);
            var hasAttendance = await _context.Attendances.AnyAsync(a => a.WorkScheduleId == schedule.Id);

            if (hasRequests || hasAttendance)
            {
                string errMsg = "Không thể xóa phân ca này vì đã có dữ liệu chấm công hoặc đơn từ liên quan.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["ErrorMessage"] = errMsg;
                return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
            }

            var reg = await _context.ShiftRegistrations
                .FirstOrDefaultAsync(r => r.StaffId == schedule.StaffId && r.ShiftId == schedule.ShiftId && r.RegistrationDate.Date == schedule.WorkDate.Date && r.Status == "Approved");
            if (reg != null)
            {
                reg.Status = "Rejected";
                _context.ShiftRegistrations.Update(reg);
            }

            _context.WorkSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            string successMsg = "Đã xóa phân lịch thành công!";
            if (IsAjaxRequest()) return Json(new { success = true, message = successMsg });
            TempData["SuccessMessage"] = successMsg;
            return RedirectToAction(nameof(Index), new { startOfWeek = startOfWeek?.ToString("yyyy-MM-dd") });
        }
    }
}
