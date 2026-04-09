using Microsoft.EntityFrameworkCore;
using Quan_Ly_xe_Ra_Vao.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Thư viện để làm chức năng Đăng nhập

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Controllers và Views
builder.Services.AddControllersWithViews();

// 2. Đăng ký Database (DbContext)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Cấu hình Authentication (Bảo mật Đăng nhập)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Sửa lại: Trỏ về AccountController
        options.AccessDeniedPath = "/Account/AccessDenied"; // Sửa lại: Trỏ về AccountController
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Tự động đăng xuất sau 8 tiếng
    });

// 4. Thêm Session (Để lưu thông báo tạm thời, ví dụ: "Check-in thành công!")
builder.Services.AddSession();

var app = builder.Build();

// --- Cấu hình Middleware Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Bắt buộc phải có để load được ảnh khuôn mặt, css, js

app.UseRouting();

app.UseSession(); // Bật Session lên

app.UseAuthentication(); // Bật chức năng kiểm tra vé Đăng nhập (Phải nằm trên Authorization)
app.UseAuthorization();  // Bật chức năng kiểm tra Quyền (Admin/Nhân viên/Giám đốc)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();