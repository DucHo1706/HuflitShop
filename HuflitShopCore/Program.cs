using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using HuflitShopCore.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// 2. Cấu hình DbContext kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => 
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// 3. Cấu hình Cookie Authentication (theo đúng kiến trúc ban đầu của bạn)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.Cookie.Name = "HuflitShopAuth";
    });
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<VnPayService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ColorService>();
builder.Services.AddScoped<SizeService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductVariantService>(); 
builder.Services.AddScoped<ProductImageService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<StockReceiptService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<GrabExpressService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// Initialize ImageRouteHelper
HuflitShopCore.Helpers.ImageRouteHelper.Initialize(app.Environment.WebRootPath);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();

// Centralized authorization for Admin Area
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint != null)
    {
        var area = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.AreaAttribute>()?.RouteValue;
        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var user = context.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect("/Login/Login?returnUrl=" + System.Net.WebUtility.UrlEncode(returnUrl));
                return;
            }

            bool hasAdminOrEmployeeRole = user.Claims
                .Any(c => c.Type == System.Security.Claims.ClaimTypes.Role && 
                          (c.Value == "1" || c.Value == "ROLE-ADMIN" || c.Value == "2" || c.Value == "ROLE-EMPLOYEE"));

            if (!hasAdminOrEmployeeRole)
            {
                context.Response.Redirect("/Login/AccessDenied");
                return;
            }
        }
    }
    await next();
});

app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
