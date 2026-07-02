using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShiftRegistrationsController : Controller
    {
        private readonly AppDbContext _context;

        public ShiftRegistrationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ShiftRegistrations
        public async Task<IActionResult> Index(DateTime? dateFilter)
        {
            var query = _context.ShiftRegistrations
                .Include(r => r.Staff)
                .Include(r => r.Shift)
                .AsQueryable();

            if (dateFilter.HasValue)
            {
                query = query.Where(r => r.RegistrationDate.Date == dateFilter.Value.Date);
                ViewBag.DateFilter = dateFilter.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                // Mặc định hiện từ hôm nay trở đi
                query = query.Where(r => r.RegistrationDate.Date >= DateTime.Today);
            }

            var registrations = await query.OrderBy(r => r.RegistrationDate).ThenBy(r => r.Shift.StartTime).ToListAsync();
            return View(registrations);
        }

        // POST: Admin/ShiftRegistrations/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var reg = await _context.ShiftRegistrations.Include(r => r.Shift).FirstOrDefaultAsync(r => r.Id == id);
            if (reg == null) return NotFound();

            if (reg.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Ca này đã được duyệt từ trước.";
                return RedirectToAction(nameof(Index));
            }

            if (reg.RegistrationDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Không thể duyệt ca làm việc trong quá khứ.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra số lượng nhân viên đã được duyệt trong ca này ngày này
            var currentApprovedCount = await _context.WorkSchedules
                .CountAsync(w => w.WorkDate.Date == reg.RegistrationDate.Date && w.ShiftId == reg.ShiftId);

            if (currentApprovedCount >= reg.Shift.MaxStaff)
            {
                TempData["ErrorMessage"] = $"Ca này đã đủ người ({reg.Shift.MaxStaff} người). Không thể duyệt thêm!";
                return RedirectToAction(nameof(Index));
            }

            reg.Status = "Approved";
            _context.ShiftRegistrations.Update(reg);

            // Tạo WorkSchedule
            var workSchedule = new WorkSchedule
            {
                StaffId = reg.StaffId,
                ShiftId = reg.ShiftId,
                WorkDate = reg.RegistrationDate.Date,
                CreatedAt = DateTime.Now
            };
            _context.WorkSchedules.Add(workSchedule);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã duyệt và xếp lịch thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ShiftRegistrations/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var reg = await _context.ShiftRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            if (reg.Status == "Approved")
            {
                // Nếu đang Approved mà bị Reject thì phải xóa WorkSchedule
                var schedule = await _context.WorkSchedules
                    .FirstOrDefaultAsync(w => w.StaffId == reg.StaffId && w.ShiftId == reg.ShiftId && w.WorkDate.Date == reg.RegistrationDate.Date);
                if (schedule != null)
                {
                    // Kiểm tra xem ca này đã có đơn từ hoặc điểm danh chưa
                    var hasRequests = await _context.StaffRequests.AnyAsync(r => r.WorkScheduleId == schedule.Id);
                    var hasAttendance = await _context.Attendances.AnyAsync(a => a.WorkScheduleId == schedule.Id);

                    if (hasRequests || hasAttendance)
                    {
                        TempData["ErrorMessage"] = "Không thể hủy ca này vì nhân viên đã gửi đơn từ hoặc có lịch sử điểm danh liên quan. Vui lòng xử lý đơn từ trước.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.WorkSchedules.Remove(schedule);
                }
            }

            reg.Status = "Rejected";
            _context.ShiftRegistrations.Update(reg);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã từ chối đơn đăng ký ca.";
            return RedirectToAction(nameof(Index));
        }
    }
}
