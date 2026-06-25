using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace HuflitShopCore.Services
{
    public class StockReceiptService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StockReceiptService> _logger;

        public StockReceiptService(AppDbContext context, ILogger<StockReceiptService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<StockReceiptDTO>> GetAllReceiptsAsync()
        {
            var receipts = await _context.StockReceiveds
                .Include(r => r.Supplier)
                .Include(r => r.User)
                .Include(r => r.StockReceivedDetails)
                .AsNoTracking()
                .OrderByDescending(r => r.ReceivedDate)
                .ToListAsync();

            return receipts.Select(r => new StockReceiptDTO
            {
                Id = r.Id,
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier?.SupplierName,
                UserName = r.User?.FullName,
                ReceiptDate = r.ReceivedDate,
                TotalAmount = r.TotalCost,
                ItemCount = r.StockReceivedDetails?.Count ?? 0
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

        public async Task<(bool Success, string? ReceiptId, string? ErrorMessage)> CreateReceiptWithBulkDetailsAsync(StockReceiptDTO dto, string userId)
        {
            // Lọc ra những dòng có số lượng nhập > 0
            var validDetails = dto.Details?.Where(d => d.Quantity > 0).ToList();

            if (validDetails == null || !validDetails.Any())
            {
                return (false, null, "Phiếu nhập phải có ít nhất một sản phẩm.");
            }

            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
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

                        // 3. Cập nhật tồn kho & Giá vốn bình quân (MAC)
                        var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                        if (variant != null)
                        {
                            int currentStock = variant.StockQuantity;
                            decimal currentAvgCost = variant.AverageCostPrice;
                            int receivedQty = item.Quantity;
                            decimal receivedUnitPrice = item.UnitPrice;

                            if (currentStock + receivedQty > 0)
                            {
                                if (currentStock <= 0)
                                {
                                    variant.AverageCostPrice = receivedUnitPrice;
                                }
                                else
                                {
                                    variant.AverageCostPrice = Math.Round(((currentStock * currentAvgCost) + (receivedQty * receivedUnitPrice)) / (currentStock + receivedQty), 2);
                                }
                            }
                            else
                            {
                                variant.AverageCostPrice = 0;
                            }

                            variant.StockQuantity += receivedQty;
                            _context.ProductVariants.Update(variant);

                            // 4. Ghi log giao dịch kho
                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                Id = Guid.NewGuid().ToString(),
                                ProductVariantId = item.ProductVariantId,
                                TransactionType = "IN",
                                QuantityChange = item.Quantity,
                                RemainingStock = variant.StockQuantity,
                                TransactionDate = DateTime.Now,
                                ReferenceId = receipt.Id,
                                Note = $"Nhập kho từ phiếu: {receipt.Id}"
                            });

                            // 5. Tạo lô hàng FIFO
                            _context.InventoryLots.Add(new InventoryLot
                            {
                                Id = Guid.NewGuid().ToString(),
                                ProductVariantId = item.ProductVariantId,
                                StockReceivedDetailId = detail.Id,
                                OriginalQuantity = item.Quantity,
                                RemainingQuantity = item.Quantity,
                                UnitCost = item.UnitPrice,
                                ReceivedDate = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, receipt.Id, (string?)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[StockReceipt] CreateReceiptWithBulkDetailsAsync failed. SupplierId={SupplierId}, UserId={UserId}, DetailsCount={DetailsCount}",
                        dto?.SupplierId, userId, dto?.Details?.Count ?? 0);
                    await transaction.RollbackAsync();
                    return (false, (string?)null, "Đã xảy ra lỗi hệ thống khi tạo phiếu nhập.");
                }
            });
        }

        public async Task<bool> AddBulkReceiptDetailsAsync(List<StockReceiptDetailDTO> details, string receiptId)
        {
            var validDetails = details.Where(d => d.Quantity > 0).ToList();
            if (!validDetails.Any()) return false;

            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
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
                            int currentStock = variant.StockQuantity;
                            decimal currentAvgCost = variant.AverageCostPrice;
                            int receivedQty = dto.Quantity;
                            decimal receivedUnitPrice = dto.UnitPrice;

                            if (currentStock + receivedQty > 0)
                            {
                                if (currentStock <= 0)
                                {
                                    variant.AverageCostPrice = receivedUnitPrice;
                                }
                                else
                                {
                                    variant.AverageCostPrice = Math.Round(((currentStock * currentAvgCost) + (receivedQty * receivedUnitPrice)) / (currentStock + receivedQty), 2);
                                }
                            }
                            else
                            {
                                variant.AverageCostPrice = 0;
                            }

                            variant.StockQuantity += receivedQty;
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

                            // Tạo lô hàng FIFO
                            _context.InventoryLots.Add(new InventoryLot
                            {
                                Id = Guid.NewGuid().ToString(),
                                ProductVariantId = dto.ProductVariantId,
                                StockReceivedDetailId = detail.Id,
                                OriginalQuantity = dto.Quantity,
                                RemainingQuantity = dto.Quantity,
                                UnitCost = dto.UnitPrice,
                                ReceivedDate = DateTime.Now
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
            });
        }

        public async Task<List<StockReceiptDetailDTO>> GetDetailsByReceiptIdAsync(string receiptId)
        {
            var details = await _context.StockReceivedDetails
                .Include(d => d.ProductVariant).ThenInclude(v => v.Product)
                .Include(d => d.ProductVariant).ThenInclude(v => v.Color)
                .Include(d => d.ProductVariant).ThenInclude(v => v.Size)
                .AsNoTracking()
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