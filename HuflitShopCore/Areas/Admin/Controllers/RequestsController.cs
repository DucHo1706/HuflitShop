using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HuflitShopCore.Services;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly IRequestsService _requestsService;

        public RequestsController(IRequestsService requestsService)
        {
            _requestsService = requestsService;
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

            var requests = await _requestsService.GetMyRequestsAsync(userId);
            return View(requests);
        }

        // GET: Admin/Requests/Create
        public async Task<IActionResult> Create()
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var schedules = await _requestsService.GetRecentWorkSchedulesAsync(userId, 30);
            ViewBag.Schedules = schedules;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int workScheduleId, string requestType, string reason)
        {
            if (!IsStaff()) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            var (success, message) = await _requestsService.CreateRequestAsync(userId, workScheduleId, requestType, reason);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                if (message.Contains("tương tự"))
                {
                    return RedirectToAction(nameof(MyRequests));
                }
                return RedirectToAction(nameof(Create));
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(MyRequests));
        }

        // ============================================
        // TÍNH NĂNG DÀNH CHO ADMIN
        // ============================================

        // GET: Admin/Requests/Index
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> Index()
        {
            var requests = await _requestsService.GetPendingRequestsAsync();
            return View(requests);
        }

        // GET: Admin/Requests/History
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> History()
        {
            var requests = await _requestsService.GetProcessedRequestsHistoryAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,ROLE-ADMIN,Admin")]
        public async Task<IActionResult> ProcessRequest(int requestId, string actionType, string adminNote)
        {
            var (success, message) = await _requestsService.ProcessRequestAsync(requestId, actionType, adminNote);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
