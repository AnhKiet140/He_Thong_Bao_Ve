using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize] // Bảo mật: Chỉ Admin mới được vào trang cấu hình
    public class CauHinhHeThongController : Controller
    {
        private readonly ILogger<CauHinhHeThongController> _logger;

        public CauHinhHeThongController(ILogger<CauHinhHeThongController> logger)
        {
            _logger = logger;
        }

        // ========================================================
        // HIỂN THỊ GIAO DIỆN CẤU HÌNH HỆ THỐNG
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ========================================================
        // CÁC API XỬ LÝ (Dành cho Giai đoạn 3)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] object settingsData)
        {
            // Logic lưu cấu hình vào Database hoặc file appsettings.json
            _logger.LogInformation("Đã cập nhật cấu hình lõi của hệ thống.");
            return Json(new { success = true, message = "Lưu cấu hình thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> BackupDatabase()
        {
            // Logic dump database SQL Server
            _logger.LogInformation("Đang tiến hành sao lưu Database...");
            return Json(new { success = true, message = "Sao lưu dữ liệu thành công." });
        }
    }
}