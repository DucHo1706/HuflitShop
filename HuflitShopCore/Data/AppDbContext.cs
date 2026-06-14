using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Models;

namespace HuflitShopCore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Bảng trung gian UserRoles (cấu hình khóa chính phức hợp)
            builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Cấu hình Unique Constraint cho ProductVariant (Tránh trùng lặp biến thể cùng Product, Size, Color)
            builder.Entity<ProductVariant>()
                .HasIndex(pv => new { pv.ProductId, pv.SizeId, pv.ColorId })
                .IsUnique();
        }

        // Nhóm 1: Identity & Users
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // Nhóm 2: Danh mục & Sản phẩm
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        // Nhóm 3: Kho & Nhà cung cấp
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<StockReceived> StockReceiveds { get; set; }
        public DbSet<StockReceivedDetail> StockReceivedDetails { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        // Nhóm 4: Đơn hàng, Giỏ hàng & Khuyến mãi
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Nhóm 5: Tương tác, Lịch sử xem & Chat (Logs)
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ProductViewsLog> ProductViewsLogs { get; set; }
    }
}