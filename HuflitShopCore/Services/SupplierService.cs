using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models; // Giả sử model của bạn tên là Supplier
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class SupplierService
    {
        private readonly AppDbContext _context;

        public SupplierService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierDTO>> GetAllSuppliersAsync()
        {
            var suppliers = await _context.Suppliers.ToListAsync();

            return suppliers.Select(s => new SupplierDTO
            {
                Id = s.Id,
                Name = s.SupplierName,
                ContactName = s.ContactName,
                PhoneNumber = s.Phone,
                Email = s.Email,
                Address = s.Address,
                IsActive = true // Model Supplier hiện tại không có IsActive, tạm mặc định là true
            }).ToList();
        }

        public async Task<SupplierDTO?> GetSupplierByIdAsync(string id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return null;

            return new SupplierDTO
            {
                Id = s.Id,
                Name = s.SupplierName,
                ContactName = s.ContactName,
                PhoneNumber = s.Phone,
                Email = s.Email,
                Address = s.Address,
                IsActive = true
            };
        }

        public async Task<bool> CreateSupplierAsync(SupplierDTO dto)
        {
            var supplier = new Supplier 
            {
                Id = Guid.NewGuid().ToString(),
                SupplierName = dto.Name ?? string.Empty,
                ContactName = dto.ContactName ?? string.Empty,
                Phone = dto.PhoneNumber ?? string.Empty,
                Email = dto.Email ?? string.Empty,
                Address = dto.Address ?? string.Empty,
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSupplierAsync(SupplierDTO dto)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.Id);
            if (supplier == null) return false;

            supplier.SupplierName = dto.Name ?? string.Empty;
            supplier.ContactName = dto.ContactName ?? string.Empty;
            supplier.Phone = dto.PhoneNumber ?? string.Empty;
            supplier.Email = dto.Email ?? string.Empty;
            supplier.Address = dto.Address ?? string.Empty;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> ToggleStatusAsync(string id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return false;

            // Model Supplier hiện tại không có thuộc tính IsActive để đổi trạng thái
            return false;
        }
    }
}