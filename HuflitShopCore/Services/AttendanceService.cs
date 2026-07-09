using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkSchedule>> GetTodayAndYesterdayOvernightSchedulesAsync(string userId)
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            return await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.StaffId == userId && 
                       (w.WorkDate.Date == today || 
                       (w.WorkDate.Date == yesterday && w.Shift.EndTime < w.Shift.StartTime)))
                .OrderBy(w => w.WorkDate)
                .ThenBy(w => w.Shift.StartTime)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetAttendancesForSchedulesAsync(List<int> scheduleIds)
        {
            return await _context.Attendances
                .Where(a => scheduleIds.Contains(a.WorkScheduleId))
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetPastAttendancesAsync(string userId, int limit = 30)
        {
            return await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .Where(a => a.WorkSchedule.StaffId == userId && a.CheckOutTime != null)
                .OrderByDescending(a => a.CheckInTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CheckInAsync(string userId, int workScheduleId, string clientIp)
        {
            var workSchedule = await _context.WorkSchedules
                .Include(w => w.Shift)
                .FirstOrDefaultAsync(w => w.Id == workScheduleId && w.StaffId == userId);

            if (workSchedule == null)
            {
                return (false, "Lịch làm việc không tồn tại.");
            }

            var today = DateTime.Today;
            var isToday = workSchedule.WorkDate.Date == today;
            var isYesterdayOvernight = workSchedule.WorkDate.Date == today.AddDays(-1) 
                                       && workSchedule.Shift.EndTime < workSchedule.Shift.StartTime;

            if (!isToday && !isYesterdayOvernight)
            {
                return (false, "Lịch làm việc không hợp lệ (đã quá hạn hoặc chưa tới).");
            }

            var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a => a.WorkScheduleId == workScheduleId);
            if (existingAttendance != null)
            {
                return (false, "Bạn đã check-in rồi.");
            }

            // Xác thực Wifi IP cửa hàng
            var shopSetting = await _context.ShopSettings.FirstOrDefaultAsync(s => s.Key == "ShopWifiIpAddress");
            if (shopSetting != null && !string.IsNullOrEmpty(shopSetting.Value))
            {
                if (clientIp != shopSetting.Value && clientIp != "::1" && clientIp != "127.0.0.1")
                {
                    return (false, $"Lỗi: IP mạng của bạn ({clientIp}) không khớp với WiFi cửa hàng. Vui lòng kết nối WiFi quán để chấm công.");
                }
            }

            var now = DateTime.Now;
            var status = "Present";
            
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

            return (true, "Check-in thành công lúc " + now.ToString("HH:mm"));
        }

        public async Task<(bool Success, string Message)> CheckOutAsync(string userId, int attendanceId, string clientIp)
        {
            var attendance = await _context.Attendances
                .Include(a => a.WorkSchedule)
                .ThenInclude(w => w.Shift)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                return (false, "Không tìm thấy dữ liệu chấm công.");
            }

            // Bảo mật: Đảm bảo nhân viên chỉ check-out lịch của chính mình
            if (attendance.WorkSchedule.StaffId != userId)
            {
                return (false, "Lịch làm việc không thuộc quyền sở hữu của bạn.");
            }

            if (attendance.CheckOutTime.HasValue)
            {
                return (false, "Bạn đã check-out rồi.");
            }

            // Xác thực Wifi IP
            var shopSetting = await _context.ShopSettings.FirstOrDefaultAsync(s => s.Key == "ShopWifiIpAddress");
            if (shopSetting != null && !string.IsNullOrEmpty(shopSetting.Value))
            {
                if (clientIp != shopSetting.Value && clientIp != "::1" && clientIp != "127.0.0.1")
                {
                    return (false, $"Lỗi: IP mạng của bạn ({clientIp}) không khớp với WiFi cửa hàng. Vui lòng kết nối WiFi quán để check-out.");
                }
            }

            var now = DateTime.Now;
            attendance.CheckOutTime = now;
            attendance.CheckOutIpAddress = clientIp;

            // Tính toán giờ làm thực tế, giới hạn trong ca làm
            var shiftDate = attendance.WorkSchedule.WorkDate.Date;
            var shiftStartTime = shiftDate.Add(attendance.WorkSchedule.Shift.StartTime);
            var shiftEndTime = shiftDate.Add(attendance.WorkSchedule.Shift.EndTime);
            
            // Xử lý ca qua đêm
            if (attendance.WorkSchedule.Shift.EndTime < attendance.WorkSchedule.Shift.StartTime)
            {
                shiftEndTime = shiftEndTime.AddDays(1);
            }

            var checkInTime = attendance.CheckInTime.Value;
            
            // Tính số phút đi trễ để phạt
            var lateMinutes = (checkInTime - shiftStartTime).TotalMinutes;
            double penaltyHours = 0;
            
            if (lateMinutes > 0)
            {
                if (lateMinutes <= 10)
                {
                    penaltyHours = 0.25; // Phạt 15 phút
                    attendance.Note = "Đi trễ 1-10p, phạt 15p.";
                }
                else
                {
                    penaltyHours = lateMinutes / 60.0;
                    attendance.Note = $"Đi trễ {Math.Round(lateMinutes)}p.";
                }
            }

            // Chỉ tính giờ từ khi bắt đầu ca
            var effectiveCheckIn = checkInTime > shiftStartTime ? checkInTime : shiftStartTime;
            
            // Chỉ tính giờ đến khi kết thúc ca
            var effectiveCheckOut = now < shiftEndTime ? now : shiftEndTime;

            var totalWorkingHours = (effectiveCheckOut - effectiveCheckIn).TotalHours;
            if (totalWorkingHours < 0) totalWorkingHours = 0;

            attendance.CalculatedHours = Math.Max(0, totalWorkingHours - penaltyHours);

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            return (true, "Check-out thành công lúc " + now.ToString("HH:mm"));
        }
    }
}
