using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerDTO>> GetAllCustomersAsync()
        {
            // Tạm thời lấy danh sách tất cả Users
            // Thực tế có thể thêm logic join bảng Roles để chỉ lấy Role = Customer
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new CustomerDTO
            {
                Id = u.Id,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Gender = u.Gender,
                DateOfBirth = u.DateOfBirth,
                JoinedDate = u.JoinedDate,
                IsActive = u.IsActive
            }).ToList();
        }

        public async Task<CustomerDTO?> GetCustomerByIdAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            return new CustomerDTO
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                JoinedDate = user.JoinedDate,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> ToggleCustomerStatusAsync(string id, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = isActive;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CustomerOrderDTO>> GetCustomerOrdersAsync(string userId)
        {
            // Sẽ xử lý sau khi tạo xong Model Đơn Hàng (Order)
            // Hiện tại trả về List trống để View không bị lỗi
            return new List<CustomerOrderDTO>();
        }
    }
}