using System.Collections.Generic;
using System.Threading.Tasks;
using HuflitShopCore.Models;

namespace HuflitShopCore.Services
{
    public interface ISalaryService
    {
        Task<SalaryViewModel?> GetEmployeeSalaryDetailAsync(string userId, int month, int year);
        Task<List<AdminSalaryStatisticViewModel>> GetAdminSalaryStatisticsAsync(int month, int year);
        Task<(bool Success, string Message)> AddAdjustmentAsync(SalaryAdjustment adjustment);
        Task<(bool Success, string Message)> DeleteAdjustmentAsync(string id);
        Task<List<SalaryAdjustment>> GetAdjustmentsByStaffAsync(string staffId, int month, int year);
    }
}
