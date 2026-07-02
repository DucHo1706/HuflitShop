using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.AspNetCore.Authorization;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")]
    public class ShiftsController : Controller
    {
        private readonly AppDbContext _context;

        public ShiftsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Shifts
        public async Task<IActionResult> Index()
        {
            return View(await _context.Shifts.ToListAsync());
        }

        // GET: Admin/Shifts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Shifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,StartTime,EndTime,MaxStaff,IsActive")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shift);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo ca làm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        // GET: Admin/Shifts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();

            return View(shift);
        }

        // POST: Admin/Shifts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartTime,EndTime,MaxStaff,IsActive")] Shift shift)
        {
            if (id != shift.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật ca làm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.Id == id);
        }

        // POST: Admin/Shifts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift != null)
            {
                // Vì có Cascade Delete trong DB, xóa Shift sẽ xóa luôn:
                // ShiftRegistration, WorkSchedule, Attendance, StaffRequest liên quan
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa ca làm việc và tất cả dữ liệu liên quan thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy ca làm việc này.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
