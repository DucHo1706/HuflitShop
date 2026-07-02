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
    [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
    public class WorkSchedulesController : Controller
    {
        private readonly AppDbContext _context;

        public WorkSchedulesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/WorkSchedules
        public async Task<IActionResult> Index(DateTime? dateFilter)
        {
            var query = _context.WorkSchedules
                .Include(w => w.Staff)
                .Include(w => w.Shift)
                .AsQueryable();

            if (dateFilter.HasValue)
            {
                query = query.Where(w => w.WorkDate.Date == dateFilter.Value.Date);
                ViewBag.DateFilter = dateFilter.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                // Mặc định hiện từ hôm nay
                query = query.Where(w => w.WorkDate.Date >= DateTime.Today);
            }

            var schedules = await query
                .OrderBy(w => w.WorkDate)
                .ThenBy(w => w.Shift.StartTime)
                .ToListAsync();

            var scheduleIds = schedules.Select(w => w.Id).ToList();
            var attendances = await _context.Attendances
                .Where(a => scheduleIds.Contains(a.WorkScheduleId))
                .ToDictionaryAsync(a => a.WorkScheduleId);

            ViewBag.Attendances = attendances;

            return View(schedules);
        }
    }
}
