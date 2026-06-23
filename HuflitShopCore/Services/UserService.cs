using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HuflitShopCore.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> AuthenticateAsync(string email, string password)
        {
            // Tìm user theo email (Lưu ý: Thực tế nên hash password để so sánh)
            return await _context.Users //
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password); //
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ConfirmEmailAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            // Tạm thời set true (trong thực tế cần kiểm tra token)
            // user.EmailConfirmed = true; 
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            
            user.PasswordHash = newPassword; // Lưu ý: Nên hash mật khẩu
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RegisterAsync(RegisterDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return false;

            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(), // Tuân thủ Rule 3
                FullName = dto.Name, //
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = dto.Password, //
                UserName = dto.Email,
                Avatar = "", //
                Role = "Customer" // Mặc định là khách hàng
            };

            _context.Users.Add(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<AppUser?> GetUserByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> UpdateProfileAsync(string id, string fullName, string phoneNumber, int? gender, DateTime? dateOfBirth)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Gender = gender;
            user.DateOfBirth = dateOfBirth;

            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
        }
    }
}