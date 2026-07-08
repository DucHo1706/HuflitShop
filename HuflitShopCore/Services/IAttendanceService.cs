using System.Collections.Generic;
using System.Threading.Tasks;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public interface IAttendanceService
    {
        Task<List<WorkSchedule>> GetTodayAndYesterdayOvernightSchedulesAsync(string userId);
        Task<List<Attendance>> GetAttendancesForSchedulesAsync(List<int> scheduleIds);
        Task<List<Attendance>> GetPastAttendancesAsync(string userId, int limit = 30);
        Task<(bool Success, string Message)> CheckInAsync(string userId, int workScheduleId, string clientIp);
        Task<(bool Success, string Message)> CheckOutAsync(string userId, int attendanceId, string clientIp);
    }
}
