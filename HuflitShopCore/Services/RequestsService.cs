using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public class RequestsService : IRequestsService
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<HuflitShopCore.Hubs.ChatHub> _hubContext;

        public RequestsService(AppDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<HuflitShopCore.Hubs.ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<StaffRequest>> GetMyRequestsAsync(string staffId)
        {
            return await _context.StaffRequests
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetRecentWorkSchedulesAsync(string staffId, int limit = 30)
        {
            return await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.StaffId == staffId)
                .OrderByDescending(w => w.WorkDate)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateRequestAsync(string staffId, int workScheduleId, string requestType, string reason)
        {
            var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(w => w.Id == workScheduleId && w.StaffId == staffId);
            if (schedule == null)
            {
                return (false, "Không tìm thấy ca làm việc này.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return (false, "Vui lòng nhập lý do.");
            }

            // Kiểm tra xem đã có đơn nào pending cho ca này chưa
            var existingRequest = await _context.StaffRequests
                .FirstOrDefaultAsync(r => r.WorkScheduleId == workScheduleId && r.RequestType == requestType && r.Status == "Pending");
            if (existingRequest != null)
            {
                return (false, "Bạn đã gửi 1 đơn tương tự cho ca này và đang chờ duyệt.");
            }

            var request = new StaffRequest
            {
                StaffId = staffId,
                WorkScheduleId = workScheduleId,
                RequestType = requestType,
                Reason = reason,
                Status = "Pending",
                AdminNote = "", // Tránh lỗi NOT NULL trong database
                CreatedAt = DateTime.Now
            };

            _context.StaffRequests.Add(request);
            await _context.SaveChangesAsync();

            // Real-time Notification
            try
            {
                var staffUser = await _context.Users.FindAsync(staffId);
                var staffName = staffUser?.FullName ?? staffUser?.UserName ?? "Nhân viên";
                var typeLabel = requestType == "Leave" ? "xin nghỉ phép" : "yêu cầu check-in bổ sung";
                
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveSystemNotification", new
                {
                    title = "Đơn từ mới chờ duyệt!",
                    message = $"Nhân viên {staffName} vừa gửi đơn {typeLabel}.",
                    icon = "info"
                });
            }
            catch { /* Ignore error to ensure transaction safety */ }

            return (true, "Đã gửi đơn thành công. Vui lòng chờ quản lý duyệt.");
        }

        public async Task<List<StaffRequest>> GetPendingRequestsAsync()
        {
            return await _context.StaffRequests
                .Include(r => r.Staff)
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<StaffRequest>> GetProcessedRequestsHistoryAsync()
        {
            return await _context.StaffRequests
                .Include(r => r.Staff)
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(r => r.Status != "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> ProcessRequestAsync(int requestId, string actionType, string adminNote)
        {
            var request = await _context.StaffRequests
                .Include(r => r.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return (false, "Không tìm thấy đơn.");
            }

            if (request.Status != "Pending")
            {
                return (false, "Đơn này đã được xử lý.");
            }

            if (actionType == "Approve")
            {
                request.Status = "Approved";
                
                // XỬ LÝ LOGIC TỰ ĐỘNG THEO LOẠI ĐƠN
                if (request.RequestType == "Leave")
                {
                    // Xin nghỉ phép -> tạo/cập nhật Lịch chấm công vắng mặt
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
                        if (!existingAttendance.Note.Contains("Đã xin nghỉ phép"))
                        {
                            existingAttendance.Note += " | Đã xin nghỉ phép (Có phép)";
                        }
                        _context.Attendances.Update(existingAttendance);
                    }
                }
                else if (request.RequestType == "ForgotCheckIn")
                {
                    // Báo quên Check-in -> Tự sinh chấm công đầy đủ ca làm
                    var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a => a.WorkScheduleId == request.WorkScheduleId);
                    
                    var shiftDuration = (request.WorkSchedule.Shift.EndTime - request.WorkSchedule.Shift.StartTime).TotalHours;
                    if (shiftDuration < 0)
                    {
                        shiftDuration += 24; // Ca làm qua đêm
                    }

                    var checkoutDate = request.WorkSchedule.WorkDate;
                    if (request.WorkSchedule.Shift.EndTime < request.WorkSchedule.Shift.StartTime)
                    {
                        checkoutDate = checkoutDate.AddDays(1);
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
                        existingAttendance.CheckOutTime = checkoutDate.Add(request.WorkSchedule.Shift.EndTime);
                        existingAttendance.CalculatedHours = shiftDuration > 0 ? shiftDuration : 0;
                        if (!existingAttendance.Note.Contains("Quản lý bổ sung giờ"))
                        {
                            existingAttendance.Note += " | Quản lý bổ sung giờ.";
                        }
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

            return (true, $"Đã { (actionType == "Approve" ? "duyệt" : "từ chối") } đơn thành công.");
        }
    }
}
