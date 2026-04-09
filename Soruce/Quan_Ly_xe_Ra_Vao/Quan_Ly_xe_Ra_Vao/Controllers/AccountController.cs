using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class AccountController : Controller
    {
        // 1. Mở trang Đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì đá về trang chủ
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        // 2. Xử lý khi bấm nút Đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // GIẢ LẬP CÁC TÀI KHOẢN THEO YÊU CẦU CỦA THẦY (Gồm cả Nhân Sự & Kế Toán)
            string role = "";
            string fullName = "";

            if (username == "admin" && password == "123") { role = "Admin"; fullName = "Quản Trị Viên"; }
            else if (username == "baove" && password == "123") { role = "BaoVe"; fullName = "Tổ Bảo Vệ"; }
            else if (username == "giamdoc" && password == "123") { role = "GiamDoc"; fullName = "Giám Đốc Trương"; }
            else if (username == "nhansu" && password == "123") { role = "NhanSu"; fullName = "Trưởng Phòng Nhân Sự"; }
            else if (username == "ketoan" && password == "123") { role = "KeToan"; fullName = "Trưởng Phòng Kế Toán"; }
            else
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không đúng!";
                return View();
            }

            // Tạo chứng minh thư (Cookie) cho người dùng
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("Username", username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Cấp phát Cookie và cho phép vào hệ thống
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        // 3. Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}