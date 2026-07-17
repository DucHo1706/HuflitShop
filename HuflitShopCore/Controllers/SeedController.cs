using Microsoft.AspNetCore.Mvc;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Controllers
{
    public class SeedController : Controller
    {
        private readonly AppDbContext _context;

        public SeedController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("seed-employee")]
        public async Task<IActionResult> SeedEmployee()
        {
            try
            {
                // Kiểm tra role
                if (!_context.Roles.Any(r => r.Id == "2" || r.Name == "Employee"))
                {
                    _context.Roles.Add(new Role { Id = "2", Name = "Employee", Description = "Nhân viên" });
                    await _context.SaveChangesAsync();
                }

                var role = _context.Roles.FirstOrDefault(r => r.Name == "Employee" || r.Id == "2");
                var email = "nhanvien@gmail.com";
                
                var userExists = _context.Users.Any(u => u.Email == email);
                if (!userExists)
                {
                    var newId = System.Guid.NewGuid().ToString();
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Users (Id, FullName, Email, UserName, PasswordHash, Role, Avatar, AvatarPublicId, AvatarVersion, IsActive, JoinedDate, HourlyRate) " +
                        "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
                        newId, "Nhân Viên Test", email, email, "123", "Employee", "", "none", "none", true, System.DateTime.Now, 0);

                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO UserRoles (UserId, RoleId) VALUES ({0}, {1})",
                        newId, role.Id);
                }

                return Content("Thêm nhân viên thành công! Tài khoản: nhanvien@gmail.com | Mật khẩu: 123");
            }
            catch(System.Exception ex)
            {
                return Content("Error: " + ex.Message + "\n" + ex.InnerException?.Message);
            }
        }
    }
}
