using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class SuCoCanhBaoController : Controller
    {
        private readonly ILogger<SuCoCanhBaoController> _logger;

        public SuCoCanhBaoController(ILogger<SuCoCanhBaoController> logger)
        {
            _logger = logger;
        }

        // ========================================================
        // HIỂN THỊ GIAO DIỆN TRUNG TÂM CẢNH BÁO S.O.S
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ========================================================
        // API KÍCH HOẠT KHẨN CẤP (Dành cho Giai đoạn 3 kết nối IoT)
        // ========================================================

        [HttpPost]
        public async Task<IActionResult> TriggerSOS()
        {
            _logger.LogCritical("BÁO ĐỘNG ĐỎ: Hệ thống S.O.S đã được kích hoạt toàn cơ sở!");
            // Thêm logic: Gửi SMS, Email, hú còi, lưu Database...
            return Json(new { success = true, message = "Đã kích hoạt còi báo động và quy trình sơ tán!" });
        }

        [HttpPost]
        public async Task<IActionResult> OverrideDoors([FromBody] string mode)
        {
            if (mode == "UNLOCK_ALL")
            {
                _logger.LogWarning("OVERRIDE: Đã mở khóa toàn bộ cửa từ thoát hiểm.");
                return Json(new { success = true, message = "Toàn bộ cửa thoát hiểm đã mở chốt." });
            }
            return Json(new { success = false });
        }
    }
}