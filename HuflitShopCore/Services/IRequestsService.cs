using System.Collections.Generic;
using System.Threading.Tasks;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public interface IRequestsService
    {
        Task<List<StaffRequest>> GetMyRequestsAsync(string staffId);
        Task<List<WorkSchedule>> GetRecentWorkSchedulesAsync(string staffId, int limit = 30);
        Task<(bool Success, string Message)> CreateRequestAsync(string staffId, int workScheduleId, string requestType, string reason);
        Task<List<StaffRequest>> GetPendingRequestsAsync();
        Task<List<StaffRequest>> GetProcessedRequestsHistoryAsync();
        Task<(bool Success, string Message)> ProcessRequestAsync(int requestId, string actionType, string adminNote);
    }
}
