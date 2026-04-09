using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models; // Bổ sung thư viện Models nếu cần
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize] // Bảo mật: Chỉ người có tài khoản mới được xem Log
    public class NhatKyHeThongController : Controller
    {
        private readonly ApplicationDbContext _context;

        // TIÊM DEPENDENCY INJECTION ĐỂ KẾT NỐI DATABASE
        public NhatKyHeThongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. HIỂN THỊ GIAO DIỆN NHẬT KÝ HỆ THỐNG (AUDIT LOG)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // XỬ LÝ MÚI GIỜ VIỆT NAM (UTC+7)
            // Giải quyết triệt để lỗi lệch ngày khi máy chủ ở múi giờ khác hoặc qua 12h đêm
            TimeZoneInfo vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);

            // Gửi ngày hiện tại chuẩn VN sang View để set mặc định cho ô chọn Date
            ViewBag.CurrentDate = vnTime.ToString("yyyy-MM-dd");

            // LẤY DỮ LIỆU TỪ DATABASE
            // Lấy 2000 dòng sự kiện mới nhất (Tránh load quá nhiều làm treo trình duyệt)
            var logs = await _context.NhatKyHeThongs
                                     .OrderByDescending(n => n.ThoiGian)
                                     .Take(2000)
                                     .ToListAsync();

            return View(logs);
        }

        // ========================================================
        // 2. API: HỨNG DỮ LIỆU LOG TỪ GIAO DIỆN (AJAX / FETCH)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> GhiLogTuDong([FromBody] LogRequestModel req)
        {
            if (req == null) return BadRequest();

            // Xác định người thực hiện (nếu chưa đăng nhập thì ghi là Hệ thống / Tổ Bảo Vệ)
            var nguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Tổ Bảo Vệ";

            // Xử lý múi giờ Việt Nam (UTC+7) cho sự kiện mới
            TimeZoneInfo vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);

            // Tạo bản ghi mới
            var logMoi = new NhatKyHeThong
            {
                ThoiGian = vnTime,
                MucDo = req.MucDo,
                NguoiThucHien = nguoiThucHien,
                PhanHe = req.PhanHe,
                ChiTietThaoTac = req.ChiTietThaoTac
            };

            // Lưu vào Database
            _context.NhatKyHeThongs.Add(logMoi);
            await _context.SaveChangesAsync();

            return Ok(); // Báo về cho Javascript biết là đã lưu thành công
        }
    }

    // ========================================================
    // CLASS PHỤ ĐỂ HỨNG GÓI TIN JSON TỪ JAVASCRIPT GỬI LÊN
    // ========================================================
    public class LogRequestModel
    {
        public string MucDo { get; set; }
        public string PhanHe { get; set; }
        public string ChiTietThaoTac { get; set; }
    }
}