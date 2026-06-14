using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models; // Giả định Models: StockReceipt, StockReceiptDetail, InventoryTransaction, ProductVariant
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class StockReceiptService
    {
        private readonly AppDbContext _context;

        public StockReceiptService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockReceiptDTO>> GetAllReceiptsAsync()
        {
            var receipts = await _context.StockReceiveds
                .Include(r => r.Supplier)
                .OrderByDescending(r => r.ReceivedDate)
                .ToListAsync();

            return receipts.Select(r => new StockReceiptDTO
            {
                Id = r.Id,
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier?.SupplierName,
                ReceiptDate = r.ReceivedDate,
                TotalAmount = r.TotalCost
            }).ToList();
        }

        public async Task<StockReceiptDTO?> GetReceiptByIdAsync(string id)
        {
            var r = await _context.StockReceiveds
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id);
                
            if (r == null) return null;

            return new StockReceiptDTO
            {
                Id = r.Id,
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier?.SupplierName,
                ReceiptDate = r.ReceivedDate,
                TotalAmount = r.TotalCost
            };
        }

        public async Task<string> CreateReceiptAsync(StockReceiptDTO dto, string userId)
        {
            var receipt = new StockReceived
            {
                Id = Guid.NewGuid().ToString(),
                SupplierId = dto.SupplierId,
                ReceivedDate = DateTime.Now,
                TotalCost = 0
            };

            _context.StockReceiveds.Add(receipt);
            await _context.SaveChangesAsync();
            return receipt.Id;
        }

        public async Task<(bool Success, string? ReceiptId, string? ErrorMessage)> CreateReceiptWithBulkDetailsAsync(StockReceiptDTO dto, string userId)
        {
            // Lọc ra những dòng có số lượng nhập > 0
            var validDetails = dto.Details?.Where(d => d.Quantity > 0).ToList();

            if (validDetails == null || !validDetails.Any())
            {
                return (false, null, "Phiếu nhập phải có ít nhất một sản phẩm.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalCost = validDetails.Sum(d => d.Quantity * d.UnitPrice);

                // 1. Tạo Phiếu nhập
                var receipt = new StockReceived
                {
                    Id = Guid.NewGuid().ToString(),
                    SupplierId = dto.SupplierId,
                    UserId = userId,
                    ReceivedDate = DateTime.Now,
                    TotalCost = totalCost
                };
                _context.StockReceiveds.Add(receipt);

                foreach (var item in validDetails)
                {
                    // 2. Tạo Chi tiết
                    var detail = new StockReceivedDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockReceivedId = receipt.Id,
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.StockReceivedDetails.Add(detail);

                    // 3. Cập nhật tồn kho
                    var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity += item.Quantity;
                        _context.ProductVariants.Update(variant);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, receipt.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Ghi log lỗi (ex) ở đây nếu cần
                return (false, null, "Đã xảy ra lỗi hệ thống khi tạo phiếu nhập.");
            }
        }

        public async Task<bool> AddBulkReceiptDetailsAsync(List<StockReceiptDetailDTO> details, string receiptId)
        {
            var validDetails = details.Where(d => d.Quantity > 0).ToList();
            if (!validDetails.Any()) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var receipt = await _context.StockReceiveds.FindAsync(receiptId);
                if (receipt == null) return false;

                foreach (var dto in validDetails)
                {
                    var detail = new StockReceivedDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockReceivedId = receiptId,
                        ProductVariantId = dto.ProductVariantId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice
                    };
                    _context.StockReceivedDetails.Add(detail);

                    receipt.TotalCost += (dto.Quantity * dto.UnitPrice);

                    var variant = await _context.ProductVariants.FindAsync(dto.ProductVariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity += dto.Quantity;
                        _context.ProductVariants.Update(variant);

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProductVariantId = dto.ProductVariantId,
                            TransactionType = "IN",
                            QuantityChange = dto.Quantity,
                            RemainingStock = variant.StockQuantity,
                            TransactionDate = DateTime.Now,
                            ReferenceId = receiptId,
                            Note = $"Nhập kho bổ sung từ phiếu: {receiptId}"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<List<StockReceiptDetailDTO>> GetDetailsByReceiptIdAsync(string receiptId)
        {
            var details = await _context.StockReceivedDetails
                .Include(d => d.ProductVariant).ThenInclude(v => v.Product)
                .Include(d => d.ProductVariant).ThenInclude(v => v.Color)
                .Include(d => d.ProductVariant).ThenInclude(v => v.Size)
                .Where(d => d.StockReceivedId == receiptId)
                .ToListAsync();

            return details.Select(d => new StockReceiptDetailDTO 
            { 
                Id = d.Id, 
                StockReceiptId = d.StockReceivedId, 
                ProductVariantId = d.ProductVariantId, 
                Quantity = d.Quantity, 
                UnitPrice = d.UnitPrice,
                ProductName = $"{d.ProductVariant?.Product?.ProductName} - {d.ProductVariant?.Color?.ColorName} - {d.ProductVariant?.Size?.SizeName}"
            }).ToList();
        }
    }
}