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
    public class RequestsController : Controller
    {
        private readonly AppDbContext _context;

        public RequestsController(AppDbContext context)
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

        // ============================================
        // TÍNH NĂNG DÀNH CHO NHÂN VIÊN
        // ============================================

        // GET: Admin/Requests/MyRequests
        public async Task<IActionResult> MyRequests()
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var requests = await _context.StaffRequests
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.StaffId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Admin/Requests/Create
        public async Task<IActionResult> Create()
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            // Chỉ cho phép chọn ca từ hôm nay trở về trước (đối với quên chấm công) 
            // hoặc ca trong tương lai (đối với xin nghỉ)
            var schedules = await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.StaffId == userId)
                .OrderByDescending(w => w.WorkDate)
                .Take(30) // Lấy 30 ca gần nhất
                .ToListAsync();

            ViewBag.Schedules = schedules;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int workScheduleId, string requestType, string reason)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(w => w.Id == workScheduleId && w.StaffId == userId);
            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ca làm việc này.";
                return RedirectToAction(nameof(Create));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do.";
                return RedirectToAction(nameof(Create));
            }

            // Kiểm tra xem đã có đơn nào pending cho ca này chưa
            var existingRequest = await _context.StaffRequests
                .FirstOrDefaultAsync(r => r.WorkScheduleId == workScheduleId && r.RequestType == requestType && r.Status == "Pending");
            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "Bạn đã gửi 1 đơn tương tự cho ca này và đang chờ duyệt.";
                return RedirectToAction(nameof(MyRequests));
            }

            var request = new StaffRequest
            {
                StaffId = userId,
                WorkScheduleId = workScheduleId,
                RequestType = requestType,
                Reason = reason,
                Status = "Pending",
                AdminNote = "", // Fix lỗi DB NOT NULL
                CreatedAt = DateTime.Now
            };

            _context.StaffRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi đơn thành công. Vui lòng chờ quản lý duyệt.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ============================================
        // TÍNH NĂNG DÀNH CHO ADMIN
        // ============================================

        // GET: Admin/Requests/Index
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.StaffRequests
                .Include(r => r.Staff)
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Admin/Requests/History
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> History()
        {
            var requests = await _context.StaffRequests
                .Include(r => r.Staff)
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.Status != "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> ProcessRequest(int requestId, string actionType, string adminNote)
        {
            var request = await _context.StaffRequests
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Đơn này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }

            if (actionType == "Approve")
            {
                request.Status = "Approved";
                
                // --- XỬ LÝ LOGIC TỰ ĐỘNG ---
                if (request.RequestType == "Leave")
                {
                    // Xin nghỉ -> Không xóa WorkSchedule vì sẽ gây lỗi Cascade Delete làm mất đơn từ
                    // Thay vào đó, tạo sẵn một bản ghi Attendance đánh dấu vắng mặt có phép
                    var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a => a.WorkScheduleId == request.WorkScheduleId);
                    if (existingAttendance == null)
                    {
                        var newAttendance = new Attendance
                        {
                            WorkScheduleId = request.WorkScheduleId,
                            Status = "Absent",
                            Note = "Đã xin nghỉ phép (Có phép)",
                            CalculatedHours = 0
                        };
                        _context.Attendances.Add(newAttendance);
                    }
                    else
                    {
                        existingAttendance.Status = "Absent";
                        existingAttendance.Note += " | Đã xin nghỉ phép (Có phép)";
                        _context.Attendances.Update(existingAttendance);
                    }
                }
                else if (request.RequestType == "ForgotCheckIn")
                {
                    // Báo quên Check-in -> Tự sinh Attendance
                    var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a => a.WorkScheduleId == request.WorkScheduleId);
                    
                    var shiftDuration = (request.WorkSchedule.Shift.EndTime - request.WorkSchedule.Shift.StartTime).TotalHours;
                    if (shiftDuration < 0)
                    {
                        shiftDuration += 24; // Xử lý ca qua đêm
                    }

                    var checkoutDate = request.WorkSchedule.WorkDate;
                    if (request.WorkSchedule.Shift.EndTime < request.WorkSchedule.Shift.StartTime)
                    {
                        checkoutDate = checkoutDate.AddDays(1); // Ngày Check-out là ngày hôm sau đối với ca qua đêm
                    }

                    if (existingAttendance == null)
                    {
                        var newAttendance = new Attendance
                        {
                            WorkScheduleId = request.WorkScheduleId,
                            CheckInTime = request.WorkSchedule.WorkDate.Add(request.WorkSchedule.Shift.StartTime),
                            CheckOutTime = checkoutDate.Add(request.WorkSchedule.Shift.EndTime),
                            CheckInIpAddress = "Auto (Forgot CheckIn)",
                            CheckOutIpAddress = "Auto (Forgot CheckIn)",
                            Status = "Present",
                            Note = "Tự động cộng giờ (Quản lý duyệt đơn báo quên)",
                            CalculatedHours = shiftDuration > 0 ? shiftDuration : 0
                        };
                        _context.Attendances.Add(newAttendance);
                    }
                    else
                    {
                        // Nếu lỡ có rồi nhưng thiếu giờ
                        existingAttendance.CheckOutTime = checkoutDate.Add(request.WorkSchedule.Shift.EndTime);
                        existingAttendance.CalculatedHours = shiftDuration > 0 ? shiftDuration : 0;
                        existingAttendance.Note += " | Quản lý bổ sung giờ.";
                        _context.Attendances.Update(existingAttendance);
                    }
                }
            }
            else if (actionType == "Reject")
            {
                request.Status = "Rejected";
            }

            request.AdminNote = adminNote ?? "";
            _context.StaffRequests.Update(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã { (actionType == "Approve" ? "duyệt" : "từ chối") } đơn thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
