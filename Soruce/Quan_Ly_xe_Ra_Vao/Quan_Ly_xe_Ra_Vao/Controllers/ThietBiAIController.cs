using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize] // Yêu cầu đăng nhập mới được truy cập
    public class ThietBiAIController : Controller
    {
        private readonly ILogger<ThietBiAIController> _logger;

        public ThietBiAIController(ILogger<ThietBiAIController> logger)
        {
            _logger = logger;
        }

        // ========================================================
        // GIAI ĐOẠN 2: HIỂN THỊ GIAO DIỆN QUẢN LÝ THIẾT BỊ
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            // Trả về file Views/ThietBiAI/Index.cshtml
            return View();
        }

        // ========================================================
        // DỰ KIẾN CHO GIAI ĐOẠN 3 (KẾT NỐI PHẦN CỨNG THẬT)
        // Dưới đây là các API "chờ sẵn" để sau này bạn dùng JS gọi Fetch()
        // ========================================================

        [HttpPost]
        public async Task<IActionResult> PingDevice([FromBody] string ipAddress)
        {
            // Logic gửi gói tin ICMP Ping thật đến thiết bị
            _logger.LogInformation($"Đang ping tới thiết bị: {ipAddress}");
            return Json(new { success = true, latency = 12, message = "Kết nối ổn định" });
        }

        [HttpPost]
        public async Task<IActionResult> RebootDevice([FromBody] string deviceName)
        {
            // Logic gửi lệnh SSH / API đến thiết bị để khởi động lại
            _logger.LogWarning($"Đang khởi động lại thiết bị: {deviceName}");
            return Json(new { success = true, message = "Lệnh Reboot đã được gửi thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> OpenGate()
        {
            // Logic điều khiển Rơ-le của Barie qua GPIO hoặc HTTP Request
            _logger.LogInformation("Gửi lệnh mở Barie tạm thời.");
            return Json(new { success = true, message = "Cổng đang mở" });
        }
    }
}