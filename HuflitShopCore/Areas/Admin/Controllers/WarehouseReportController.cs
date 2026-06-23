using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class WarehouseReportController : Controller
    {
        private readonly ReportService _reportService;
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly AppDbContext _context;

        public WarehouseReportController(
            ReportService reportService,
            CategoryService categoryService,
            ProductService productService,
            AppDbContext context)
        {
            _reportService = reportService;
            _categoryService = categoryService;
            _productService = productService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? year, int? month, string? supplierId, string? categoryId, string? productId)
        {
            int selectedYear = year ?? 2026; // Default year matching Excel image
            int selectedMonth = month ?? 3;   // Default month matching Excel image

            // Categories list for filter
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CategoriesList = categories;

            // Products list for filter
            var products = await _productService.GetAllProductsAsync();
            ViewBag.ProductsList = products;

            // Suppliers list for filter from database
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
            ViewBag.SuppliersList = suppliers;

            var reportData = await _reportService.GetWarehouseReportDataAsync(
                selectedYear, 
                selectedMonth, 
                supplierId, 
                categoryId, 
                productId
            );

            return View(reportData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedData()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. CLEANUP OLD TEST DATA
                // Clean old suppliers containing test keywords
                var oldSuppliers = _context.Suppliers.AsEnumerable()
                    .Where(s => s.SupplierName.Contains("Tracy") || s.SupplierName.Contains("Nh\u0103 cung c\u1ea5") || s.SupplierName.Contains("Ho\u00e0ng Long") || s.SupplierName.Contains("S\u00e0i G\u00f2n"))
                    .ToList();
                var oldSupplierIds = oldSuppliers.Select(s => s.Id).ToList();

                var testProductNames = new[] { 
                    "Kem ch\u1ed1ng n\u1eafng SPF50", "Serum Vitamin C", "Combo d\u01b0\u1ee1ng da Mini", "S\u1eefa r\u1eeda m\u1eb7t tr\u00e0 xanh", "Toner c\u1ea5p \u1ea9m", "M\u1eb7t n\u1ea1 ph\u1ee5c h\u1ed3i",
                    "\u00c1o thun Polo Classic", "\u00c1o s\u01a1 mi l\u1ee5a c\u00f4ng s\u1edf", "Qu\u1ea7n Jeans \u1ed1ng su\u00f4ng", "Ch\u00e2n v\u00e1y ch\u1eef A H\u00e0n Qu\u1ed1c", "\u0110\u1ea7m hoa nh\u00ed tay ph\u1ed3ng", "V\u00e1y d\u1ea1 h\u1ed9i Luxury d\u00e1ng d\u00e0i"
                };
                var oldProducts = _context.Products.Where(p => testProductNames.Contains(p.ProductName)).ToList();
                var oldProductIds = oldProducts.Select(p => p.Id).ToList();

                var oldVariants = _context.ProductVariants.Where(v => oldProductIds.Contains(v.ProductId)).ToList();
                var oldVariantIds = oldVariants.Select(v => v.Id).ToList();

                // Remove Inventory Transactions
                var oldTxs = _context.InventoryTransactions.Where(t => oldVariantIds.Contains(t.ProductVariantId)).ToList();
                _context.InventoryTransactions.RemoveRange(oldTxs);

                // Remove Order Details
                var oldOrderDetails = _context.OrderDetails.Where(d => oldVariantIds.Contains(d.ProductVariantId)).ToList();
                _context.OrderDetails.RemoveRange(oldOrderDetails);

                // Remove Orders containing đại lý / test
                var oldOrders = _context.Orders.Where(o => o.ShippingFullName.Contains("\u0110\u1ea1i l\u00fd") || o.ShippingFullName.Contains("Kh\u00e1ch h\u00e0ng Test")).ToList();
                var oldOrderIds = oldOrders.Select(o => o.Id).ToList();
                var remainingDetails = _context.OrderDetails.Where(d => oldOrderIds.Contains(d.OrderId)).ToList();
                _context.OrderDetails.RemoveRange(remainingDetails);
                _context.Orders.RemoveRange(oldOrders);

                // Remove Stock Received Details
                var oldReceiptDetails = _context.StockReceivedDetails.Where(d => oldVariantIds.Contains(d.ProductVariantId)).ToList();
                _context.StockReceivedDetails.RemoveRange(oldReceiptDetails);

                // Remove Stock Receiveds
                var oldReceipts = _context.StockReceiveds.Where(r => oldSupplierIds.Contains(r.SupplierId)).ToList();
                var remainingReceiptDetails = _context.StockReceivedDetails.Where(d => oldReceipts.Select(x => x.Id).Contains(d.StockReceivedId)).ToList();
                _context.StockReceivedDetails.RemoveRange(remainingReceiptDetails);
                _context.StockReceiveds.RemoveRange(oldReceipts);

                // Remove variants and products
                _context.ProductVariants.RemoveRange(oldVariants);
                _context.Products.RemoveRange(oldProducts);

                // Remove old suppliers
                _context.Suppliers.RemoveRange(oldSuppliers);

                await _context.SaveChangesAsync();

                // 2. CREATE NEW CATEGORIES
                var cat1 = _context.Categories.FirstOrDefault(c => c.CategoryName == "\u00c1o Nam & N\u1eef")
                    ?? new Category { Id = Guid.NewGuid().ToString(), CategoryName = "\u00c1o Nam & N\u1eef" };
                var cat2 = _context.Categories.FirstOrDefault(c => c.CategoryName == "Qu\u1ea7n & Ch\u00e2n V\u00e1y")
                    ?? new Category { Id = Guid.NewGuid().ToString(), CategoryName = "Qu\u1ea7n & Ch\u00e2n V\u00e1y" };
                var cat3 = _context.Categories.FirstOrDefault(c => c.CategoryName == "\u0110\u1ea7m & V\u00e1y Thi\u1ebft K\u1ebf")
                    ?? new Category { Id = Guid.NewGuid().ToString(), CategoryName = "\u0110\u1ea7m & V\u00e1y Thi\u1ebft K\u1ebf" };

                if (_context.Entry(cat1).State == EntityState.Detached) _context.Categories.Add(cat1);
                if (_context.Entry(cat2).State == EntityState.Detached) _context.Categories.Add(cat2);
                if (_context.Entry(cat3).State == EntityState.Detached) _context.Categories.Add(cat3);

                // 3. CREATE NEW SUPPLIERS
                var sup1 = new Supplier
                {
                    Id = Guid.NewGuid().ToString(),
                    SupplierName = "T\u1ed5ng kho May m\u1eb7c Ho\u00e0ng Long",
                    ContactName = "Ho\u00e0ng Long",
                    Phone = "0987654321",
                    Email = "hoanglong@supplier.com",
                    Address = "H\u00e0 N\u1ed9i"
                };
                var sup2 = new Supplier
                {
                    Id = Guid.NewGuid().ToString(),
                    SupplierName = "Th\u1eddi trang xu\u1ea5t kh\u1ea9u S\u00e0i G\u00f2n",
                    ContactName = "Nguy\u1ec5n Minh",
                    Phone = "0912345678",
                    Email = "saigonfashion@supplier.com",
                    Address = "TP. H\u1ed3 Ch\u00ed Minh"
                };
                var sup3 = new Supplier
                {
                    Id = Guid.NewGuid().ToString(),
                    SupplierName = "Nh\u1eadp kh\u1ea9u D\u1ec7t may Tracy",
                    ContactName = "Tracy Nguy\u1ec5n",
                    Phone = "0909090909",
                    Email = "tracy@supplier.com",
                    Address = "\u0110\u00e0 N\u1eb5ng"
                };
                _context.Suppliers.AddRange(sup1, sup2, sup3);

                // 4. CREATE COLORS & SIZES
                var colors = new List<Color>();
                var colorNames = new[] { "\u0110en", "Tr\u1eafng", "Xanh Navy", "\u0110\u1ecf \u0111\u00f4", "Beige" };
                var hexCodes = new[] { "#000000", "#FFFFFF", "#000080", "#800020", "#F5F5DC" };
                for (int i = 0; i < colorNames.Length; i++)
                {
                    var name = colorNames[i];
                    var col = _context.Colors.FirstOrDefault(c => c.ColorName == name);
                    if (col == null)
                    {
                        col = new Color { Id = Guid.NewGuid().ToString(), ColorName = name, HexCode = hexCodes[i] };
                        _context.Colors.Add(col);
                    }
                    colors.Add(col);
                }

                var sizes = new List<Size>();
                var sizeNames = new[] { "S", "M", "L", "XL" };
                foreach (var name in sizeNames)
                {
                    var sz = _context.Sizes.FirstOrDefault(s => s.SizeName == name);
                    if (sz == null)
                    {
                        sz = new Size { Id = Guid.NewGuid().ToString(), SizeName = name, SizeType = "Clothing" };
                        _context.Sizes.Add(sz);
                    }
                    sizes.Add(sz);
                }

                await _context.SaveChangesAsync();

                // 5. CREATE PRODUCTS
                var p1 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "\u00c1o thun Polo Classic", CategoryId = cat1.Id, CurrentPrice = 250000m, CreatedAt = new DateTime(2026, 1, 1) };
                var p2 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "\u00c1o s\u01a1 mi l\u1ee5a c\u00f4ng s\u1edf", CategoryId = cat1.Id, CurrentPrice = 350000m, CreatedAt = new DateTime(2026, 1, 1) };
                var p3 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "Qu\u1ea7n Jeans \u1ed1ng su\u00f4ng", CategoryId = cat2.Id, CurrentPrice = 400000m, CreatedAt = new DateTime(2026, 1, 1) };
                var p4 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "Ch\u00e2n v\u00e1y ch\u1eef A H\u00e0n Qu\u1ed1c", CategoryId = cat2.Id, CurrentPrice = 180000m, CreatedAt = new DateTime(2026, 1, 1) };
                var p5 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "\u0110\u1ea7m hoa nh\u00ed tay ph\u1ed3ng", CategoryId = cat3.Id, CurrentPrice = 380000m, CreatedAt = new DateTime(2026, 1, 1) };
                var p6 = new Product { Id = Guid.NewGuid().ToString(), ProductName = "V\u00e1y d\u1ea1 h\u1ed9i Luxury d\u00e1ng d\u00e0i", CategoryId = cat3.Id, CurrentPrice = 750000m, CreatedAt = new DateTime(2026, 1, 1) };
                _context.Products.AddRange(p1, p2, p3, p4, p5, p6);

                await _context.SaveChangesAsync();

                // Helper local functions to find color/size object by name
                Func<string, Color> getCol = name => colors.First(c => c.ColorName == name);
                Func<string, Size> getSz = name => sizes.First(s => s.SizeName == name);

                // 6. CREATE PRODUCT VARIANTS
                var variants = new List<ProductVariant>();

                // Polo Classic: Black M/L, White M/L
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p1.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p1.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p1.Id, ColorId = getCol("Tr\u1eafng").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p1.Id, ColorId = getCol("Tr\u1eafng").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });

                // Sơ mi lụa: White S/M/L, Beige M/L
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p2.Id, ColorId = getCol("Tr\u1eafng").Id, SizeId = getSz("S").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p2.Id, ColorId = getCol("Tr\u1eafng").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p2.Id, ColorId = getCol("Tr\u1eafng").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p2.Id, ColorId = getCol("Beige").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p2.Id, ColorId = getCol("Beige").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });

                // Quần Jeans: Navy M/L/XL, Black L/XL
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p3.Id, ColorId = getCol("Xanh Navy").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p3.Id, ColorId = getCol("Xanh Navy").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p3.Id, ColorId = getCol("Xanh Navy").Id, SizeId = getSz("XL").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p3.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p3.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("XL").Id, StockQuantity = 0, AverageCostPrice = 0 });

                // Chân váy: Beige S/M, Black S/M
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p4.Id, ColorId = getCol("Beige").Id, SizeId = getSz("S").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p4.Id, ColorId = getCol("Beige").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p4.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("S").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p4.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });

                // Đầm hoa nhí: Đỏ đô S/M/L, Beige M/L
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p5.Id, ColorId = getCol("\u0110\u1ecf \u0111\u00f4").Id, SizeId = getSz("S").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p5.Id, ColorId = getCol("\u0110\u1ecf \u0111\u00f4").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p5.Id, ColorId = getCol("\u0110\u1ecf \u0111\u00f4").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p5.Id, ColorId = getCol("Beige").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p5.Id, ColorId = getCol("Beige").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });

                // Váy dạ hội: Đỏ đô M/L, Black M/L
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p6.Id, ColorId = getCol("\u0110\u1ecf \u0111\u00f4").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p6.Id, ColorId = getCol("\u0110\u1ecf \u0111\u00f4").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p6.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("M").Id, StockQuantity = 0, AverageCostPrice = 0 });
                variants.Add(new ProductVariant { Id = Guid.NewGuid().ToString(), ProductId = p6.Id, ColorId = getCol("\u0110en").Id, SizeId = getSz("L").Id, StockQuantity = 0, AverageCostPrice = 0 });

                _context.ProductVariants.AddRange(variants);
                await _context.SaveChangesAsync();

                // Find active user
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? _context.Users.Select(u => u.Id).FirstOrDefault() 
                    ?? string.Empty;

                // 7. SEED STOCK RECEIPTS (IN)
                // Receipt 1 (January 5, 2026) - Supplier 1 (Hoàng Long)
                var rec1 = new StockReceived { Id = Guid.NewGuid().ToString(), SupplierId = sup1.Id, UserId = userId, ReceivedDate = new DateTime(2026, 1, 5, 10, 0, 0), TotalCost = 0 };
                _context.StockReceiveds.Add(rec1);
                
                // Add details for Polo & Sơ mi in Jan
                var poloAndSomiVariants = variants.Where(v => v.ProductId == p1.Id || v.ProductId == p2.Id).ToList();
                decimal rec1Total = 0;
                foreach (var v in poloAndSomiVariants)
                {
                    int qty = 150;
                    decimal price = v.ProductId == p1.Id ? 120000m : 170000m; // Polo cost 120k, Sơ mi cost 170k
                    _context.StockReceivedDetails.Add(new StockReceivedDetail { Id = Guid.NewGuid().ToString(), StockReceivedId = rec1.Id, ProductVariantId = v.Id, Quantity = qty, UnitPrice = price });
                    
                    v.StockQuantity = qty;
                    v.AverageCostPrice = price;
                    _context.ProductVariants.Update(v);
                    
                    _context.InventoryTransactions.Add(new InventoryTransaction { Id = Guid.NewGuid().ToString(), ProductVariantId = v.Id, TransactionType = "IN", QuantityChange = qty, RemainingStock = qty, TransactionDate = rec1.ReceivedDate, ReferenceId = rec1.Id, Note = "Nh\u1eadp h\u00e0ng \u0111\u1ea7u n\u0103m 2026" });
                    rec1Total += qty * price;
                }
                rec1.TotalCost = rec1Total;

                // Receipt 2 (February 5, 2026) - Supplier 2 (Sài Gòn)
                var rec2 = new StockReceived { Id = Guid.NewGuid().ToString(), SupplierId = sup2.Id, UserId = userId, ReceivedDate = new DateTime(2026, 2, 5, 11, 0, 0), TotalCost = 0 };
                _context.StockReceiveds.Add(rec2);
                
                // Add details for Jeans & Chân váy in Feb
                var jeansAndSkirtVariants = variants.Where(v => v.ProductId == p3.Id || v.ProductId == p4.Id).ToList();
                decimal rec2Total = 0;
                foreach (var v in jeansAndSkirtVariants)
                {
                    int qty = 120;
                    decimal price = v.ProductId == p3.Id ? 190000m : 90000m; // Jeans cost 190k, Chân váy cost 90k
                    _context.StockReceivedDetails.Add(new StockReceivedDetail { Id = Guid.NewGuid().ToString(), StockReceivedId = rec2.Id, ProductVariantId = v.Id, Quantity = qty, UnitPrice = price });
                    
                    v.StockQuantity = qty;
                    v.AverageCostPrice = price;
                    _context.ProductVariants.Update(v);
                    
                    _context.InventoryTransactions.Add(new InventoryTransaction { Id = Guid.NewGuid().ToString(), ProductVariantId = v.Id, TransactionType = "IN", QuantityChange = qty, RemainingStock = qty, TransactionDate = rec2.ReceivedDate, ReferenceId = rec2.Id, Note = "Nh\u1eadp h\u00e0ng th\u00e1ng 2" });
                    rec2Total += qty * price;
                }
                rec2.TotalCost = rec2Total;

                // Receipt 3 (March 1, 2026) - Supplier 3 (Tracy)
                var rec3 = new StockReceived { Id = Guid.NewGuid().ToString(), SupplierId = sup3.Id, UserId = userId, ReceivedDate = new DateTime(2026, 3, 1, 10, 0, 0), TotalCost = 0 };
                _context.StockReceiveds.Add(rec3);
                
                // Add details for Đầm & Váy dạ hội in March
                var dressAndGownVariants = variants.Where(v => v.ProductId == p5.Id || v.ProductId == p6.Id).ToList();
                decimal rec3Total = 0;
                foreach (var v in dressAndGownVariants)
                {
                    int qty = 80;
                    decimal price = v.ProductId == p5.Id ? 180000m : 350000m; // Đầm cost 180k, Dạ hội cost 350k
                    _context.StockReceivedDetails.Add(new StockReceivedDetail { Id = Guid.NewGuid().ToString(), StockReceivedId = rec3.Id, ProductVariantId = v.Id, Quantity = qty, UnitPrice = price });
                    
                    v.StockQuantity = qty;
                    v.AverageCostPrice = price;
                    _context.ProductVariants.Update(v);
                    
                    _context.InventoryTransactions.Add(new InventoryTransaction { Id = Guid.NewGuid().ToString(), ProductVariantId = v.Id, TransactionType = "IN", QuantityChange = qty, RemainingStock = qty, TransactionDate = rec3.ReceivedDate, ReferenceId = rec3.Id, Note = "Nh\u1eadp h\u00e0ng th\u00e1ng 3" });
                    rec3Total += qty * price;
                }
                rec3.TotalCost = rec3Total;

                // Receipt 4 (March 15, 2026) - Supplier 1 (Hoàng Long) - Price Variation for MAC!
                var rec4 = new StockReceived { Id = Guid.NewGuid().ToString(), SupplierId = sup1.Id, UserId = userId, ReceivedDate = new DateTime(2026, 3, 15, 14, 0, 0), TotalCost = 0 };
                _context.StockReceiveds.Add(rec4);
                
                // Re-import Polo Classic (Black M and White M) at a HIGHER price
                var testVariantsForMAC = variants.Where(v => v.ProductId == p1.Id && v.SizeId == getSz("M").Id).ToList();
                decimal rec4Total = 0;
                foreach (var v in testVariantsForMAC)
                {
                    int qty = 50;
                    decimal newPrice = 135000m; // higher than old price 120,000 VND!
                    _context.StockReceivedDetails.Add(new StockReceivedDetail { Id = Guid.NewGuid().ToString(), StockReceivedId = rec4.Id, ProductVariantId = v.Id, Quantity = qty, UnitPrice = newPrice });
                    
                    int currentStock = v.StockQuantity;
                    decimal currentAvg = v.AverageCostPrice;
                    v.AverageCostPrice = Math.Round(((currentStock * currentAvg) + (qty * newPrice)) / (currentStock + qty), 2);
                    v.StockQuantity += qty;
                    _context.ProductVariants.Update(v);
                    
                    _context.InventoryTransactions.Add(new InventoryTransaction { Id = Guid.NewGuid().ToString(), ProductVariantId = v.Id, TransactionType = "IN", QuantityChange = qty, RemainingStock = v.StockQuantity, TransactionDate = rec4.ReceivedDate, ReferenceId = rec4.Id, Note = "Nh\u1eadp b\u1ed5 sung gi\u00e1 m\u1edbi (MAC test)" });
                    rec4Total += qty * newPrice;
                }
                rec4.TotalCost = rec4Total;

                await _context.SaveChangesAsync();

                // 8. SEED ORDERS (OUT)
                var rand = new Random(2026);
                var paymentMethod = _context.PaymentMethods.FirstOrDefault() 
                    ?? new PaymentMethod { Id = "pm-cod", MethodName = "Ti\u1ec1n m\u1eb7t (COD)" };
                if (_context.Entry(paymentMethod).State == EntityState.Detached) _context.PaymentMethods.Add(paymentMethod);

                // We will create orders spread across Jan, Feb, and March 2026
                int orderIdCounter = 1;

                // January Orders: 5 orders
                for (int i = 0; i < 5; i++)
                {
                    var orderDate = new DateTime(2026, 1, 10 + i * 3, 11, 0, 0);
                    await CreateMockOrderAsync(orderDate, poloAndSomiVariants, p1, p2, paymentMethod, userId, orderIdCounter++);
                }

                // February Orders: 8 orders
                var febrVariants = variants.Where(v => v.StockQuantity > 0).ToList();
                for (int i = 0; i < 8; i++)
                {
                    var orderDate = new DateTime(2026, 2, 8 + i * 2, 14, 0, 0);
                    await CreateMockOrderAsync(orderDate, febrVariants, p1, p2, paymentMethod, userId, orderIdCounter++);
                }

                // March Orders: 15 orders (lots of sales activity in March!)
                var marchVariants = variants.Where(v => v.StockQuantity > 0).ToList();
                for (int i = 0; i < 15; i++)
                {
                    var orderDate = new DateTime(2026, 3, 2 + i * 1, 15, 0, 0);
                    await CreateMockOrderAsync(orderDate, marchVariants, p1, p2, paymentMethod, userId, orderIdCounter++);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Kh\u1edfi t\u1ea1o d\u1eef li\u1ec7u qu\u1ea7n \u00e1o m\u1eabu v\u00e0 nh\u00e0 cung c\u1ea5p th\u00e0nh c\u00f4ng!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "L\u1ed7i kh\u1edfi t\u1ea1o: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task CreateMockOrderAsync(DateTime date, List<ProductVariant> availableVariants, Product p1, Product p2, PaymentMethod pm, string userId, int counter)
        {
            var activeVariants = availableVariants.Where(v => v.StockQuantity > 5).ToList();
            if (!activeVariants.Any()) return;

            var rand = new Random(counter * 73);
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                PaymentMethodId = pm.Id,
                OrderDate = date,
                OrderStatus = 3, // Completed
                PaymentStatus = 1, // Paid
                ShippingFullName = $"Kh\u00e1ch h\u00e0ng Test {counter}",
                ShippingAddress = $"S\u1ed1 {counter} \u0110\u01b0\u1eddng L\u00ea L\u1ee3i",
                ShippingCity = rand.Next(2) == 0 ? "H\u00e0 N\u1ed9i" : "H\u1ed3 Ch\u00ed Minh",
                ShippingDistrict = "Qu\u1eadn 1",
                TotalAmount = 0,
                FinalAmount = 0
            };
            _context.Orders.Add(order);

            int itemCount = rand.Next(1, 3); // 1 to 2 items per order
            decimal total = 0;

            for (int k = 0; k < itemCount; k++)
            {
                var v = activeVariants[rand.Next(activeVariants.Count)];
                if (v.StockQuantity <= 2) continue;

                int qty = rand.Next(1, 4); // 1 to 3 items
                v.StockQuantity -= qty;
                _context.ProductVariants.Update(v);

                var product = await _context.Products.FindAsync(v.ProductId);
                decimal sellPrice = product?.CurrentPrice ?? 200000m;

                var detail = new OrderDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    ProductVariantId = v.Id,
                    Quantity = qty,
                    PurchasedPrice = sellPrice,
                    CostPrice = v.AverageCostPrice,
                    ProductNameSnapshot = product?.ProductName ?? "S\u1ea3n ph\u1ea9m",
                    SizeNameSnapshot = _context.Sizes.Find(v.SizeId)?.SizeName ?? "M\u1eb7c \u0111\u1ecbnh",
                    ColorNameSnapshot = _context.Colors.Find(v.ColorId)?.ColorName ?? "M\u1eb7c \u0111\u1ecbnh"
                };
                _context.OrderDetails.Add(detail);

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductVariantId = v.Id,
                    TransactionType = "OUT",
                    QuantityChange = -qty,
                    RemainingStock = v.StockQuantity,
                    TransactionDate = order.OrderDate,
                    ReferenceId = order.Id,
                    Note = $"Xu\u1ea5t kho b\u00e1n h\u00e0ng \u0111\u01a1n: {order.Id}"
                });

                total += qty * sellPrice;
            }

            order.TotalAmount = total;
            order.FinalAmount = total;
        }
    }
}
