﻿using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EmployeeController : Controller
    {
        private readonly EmployeeService _employeeService;

        public EmployeeController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _employeeService.GetAllEmployeesAsync();
            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            }

            if (ModelState.IsValid)
            {
                var (success, message) = await _employeeService.CreateEmployeeAsync(dto);
                if (success)
                {
                    TempData["SuccessMessage"] = message;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, message);
            }

            return View(dto);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var dto = await _employeeService.GetEmployeeByIdAsync(id);
            if (dto == null) return NotFound();

            return View(dto);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var dto = await _employeeService.GetEmployeeByIdAsync(id);
            if (dto == null) return NotFound();

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeDTO dto)
        {
            // Bỏ qua xác thực mật khẩu khi chỉnh sửa thông tin
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (ModelState.IsValid)
            {
                var success = await _employeeService.UpdateEmployeeAsync(dto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Cập nhật thất bại. Không tìm thấy nhân viên.");
            }

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _employeeService.DeleteEmployeeAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Đã khóa tài khoản nhân viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Thao tác thất bại, không tìm thấy nhân viên.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== Address =====
        [HttpGet]
        public async Task<IActionResult> AddAddress(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            ViewBag.UserId = id;

            var existing = await _employeeService.GetAddressByUserIdAsync(id);
            var model = existing ?? new Address { UserId = id };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(string id, Address model)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            ViewBag.UserId = id;

            // Đảm bảo đúng nhân viên
            model.UserId = id;

            if (ModelState.IsValid)
            {
                var success = await _employeeService.UpsertAddressAsync(model);
                if (success)
                {
                    ViewBag.Message = true;
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Message = false;
            }

            return View(model);
        }
    }
}

