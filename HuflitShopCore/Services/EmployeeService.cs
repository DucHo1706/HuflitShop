using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class EmployeeService
    {
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public async Task<List<EmployeeDTO>> GetAllEmployeesAsync()
        {
            var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == "ROLE-EMPLOYEE");
            if (employeeRole == null)
            {
                return new List<EmployeeDTO>();
            }

            var employeeUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == employeeRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var employeesQuery = from u in _context.Users
                                  join a in _context.Addresses
                                      on u.Id equals a.UserId into addr
                                  from a in addr.DefaultIfEmpty()
                                  where employeeUserIds.Contains(u.Id)
                                  orderby u.FullName
                                  select new { u, a };

            var rows = await employeesQuery.ToListAsync();

            // nếu có nhiều Address theo UserId thì sẽ bị lặp; hiện tại model không enforce 1-1
            // nên chọn Address đầu tiên theo City/District/SpecificAddress.
            return rows
                .GroupBy(x => x.u.Id)
                .Select(g =>
                {
                    var u = g.First().u;
                    var a = g.First().a;
                    return new EmployeeDTO
                    {
                        Id = u.Id,
                        FullName = u.FullName ?? string.Empty,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        IsActive = u.IsActive,
                        FullAddress = a == null ? string.Empty : $"{a.City}, {a.District}, {a.SpecificAddress}"
                    };
                })
                .ToList();
        }

        public async Task<EmployeeDTO?> GetEmployeeByIdAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == id);

            return new EmployeeDTO
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                FullAddress = address == null ? string.Empty : $"{address.City}, {address.District}, {address.SpecificAddress}",
                City = address?.City ?? string.Empty,
                District = address?.District ?? string.Empty,
                SpecificAddress = address?.SpecificAddress ?? string.Empty
            };
        }

        public async Task<(bool Success, string Message)> CreateEmployeeAsync(EmployeeDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return (false, "Email này đã được sử dụng.");
            }

            var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == "ROLE-EMPLOYEE");
            if (employeeRole == null)
            {
                return (false, "Không tìm thấy vai trò 'ROLE-EMPLOYEE'. Vui lòng thêm vai trò này vào hệ thống.");
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = dto.FullName,
                UserName = dto.Email,
                Email = dto.Email,

                // theo yêu cầu: bỏ mã hóa, lưu trực tiếp như LoginService đang làm
                PasswordHash = dto.Password ?? string.Empty,

                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                IsActive = true,

                // DB đang NOT NULL
                AvatarPublicId = dto.Email,
                AvatarVersion = "1"
            };
            _context.Users.Add(user);

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = employeeRole.Id
            };
            _context.UserRoles.Add(userRole);

            // Thêm địa chỉ mới nếu có nhập
            if (!string.IsNullOrWhiteSpace(dto.City) && !string.IsNullOrWhiteSpace(dto.District) && !string.IsNullOrWhiteSpace(dto.SpecificAddress))
            {
                var address = new Address
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    City = dto.City,
                    District = dto.District,
                    SpecificAddress = dto.SpecificAddress
                };
                _context.Addresses.Add(address);
            }

            await _context.SaveChangesAsync();
            return (true, "Tạo tài khoản nhân viên thành công.");
        }

        public async Task<bool> UpdateEmployeeAsync(EmployeeDTO dto)
        {
            var user = await _context.Users.FindAsync(dto.Id);
            if (user == null) return false;

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Gender = dto.Gender;
            user.DateOfBirth = dto.DateOfBirth;
            user.IsActive = dto.IsActive;

            _context.Users.Update(user);

            // Cập nhật hoặc thêm mới địa chỉ
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == dto.Id);
            if (!string.IsNullOrWhiteSpace(dto.City) && !string.IsNullOrWhiteSpace(dto.District) && !string.IsNullOrWhiteSpace(dto.SpecificAddress))
            {
                if (address == null)
                {
                    address = new Address
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = dto.Id,
                        City = dto.City,
                        District = dto.District,
                        SpecificAddress = dto.SpecificAddress
                    };
                    _context.Addresses.Add(address);
                }
                else
                {
                    address.City = dto.City;
                    address.District = dto.District;
                    address.SpecificAddress = dto.SpecificAddress;
                    _context.Addresses.Update(address);
                }
            }
            else if (address != null)
            {
                // Nếu người dùng xóa sạch thông tin địa chỉ trên form, xóa luôn record Address trong DB
                _context.Addresses.Remove(address);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEmployeeAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = false; // Soft delete: Chỉ cho nghỉ việc thay vì xóa cứng
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // ===== Address APIs =====
        public async Task<Address?> GetAddressByUserIdAsync(string userId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<bool> UpsertAddressAsync(Address address)
        {
            if (address == null) return false;
            if (string.IsNullOrEmpty(address.UserId)) return false;

            var existing = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == address.UserId);

            if (existing == null)
            {
                // tạo mới
                address.Id = string.IsNullOrWhiteSpace(address.Id) ? Guid.NewGuid().ToString() : address.Id;
                _context.Addresses.Add(address);
            }
            else
            {
                existing.City = address.City;
                existing.District = address.District;
                existing.SpecificAddress = address.SpecificAddress;

                _context.Addresses.Update(existing);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

