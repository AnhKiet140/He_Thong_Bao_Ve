using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Models;
using Quan_Ly_xe_Ra_Vao.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ========================================================
        // 1. TRANG CHỦ MỚI: DASHBOARD THỐNG KÊ (Có bộ lọc ngày)
        // ========================================================
        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            DateTime filterDate = selectedDate ?? DateTime.Today;
            ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");
            ViewBag.IsToday = filterDate.Date == DateTime.Today;

            int hour = DateTime.Now.Hour;
            string greeting = "Chào buổi sáng";
            if (hour >= 12 && hour < 16) greeting = "Chào buổi trưa";
            else if (hour >= 16 && hour < 19) greeting = "Chào buổi chiều";
            else if (hour >= 19) greeting = "Chào buổi tối";

            ViewBag.Greeting = greeting;

            ViewBag.TotalKhachHnay = await _context.DangKyKhachs.CountAsync(x => x.ThoiGianHen.Date == filterDate.Date);
            ViewBag.TotalChoDuyet = await _context.DangKyKhachs.CountAsync(x => x.TrangThaiDuyet == "Chờ duyệt" && x.ThoiGianHen.Date == filterDate.Date);

            ViewBag.TotalNhanVienVao = await _context.LichSuCheckIns.CountAsync(x => x.ThoiGian.Date == filterDate.Date && x.Huong == "Đi Vào");

            var thongBaoGanDay = await _context.DangKyKhachs
                .Where(x => x.ThoiGianHen.Date == filterDate.Date)
                .OrderByDescending(x => x.ThoiGianHen)
                .Take(5)
                .ToListAsync();

            ViewBag.ThongBaoGanDay = thongBaoGanDay;

            return View();
        }

        // ========================================================
        // 2. TRANG GIÁM SÁT: NƠI CHỨA CAMERA AI
        // ========================================================
        public IActionResult Monitoring()
        {
            // BỘ LỌC TỐI THƯỢNG: ĐỒNG BỘ LOGIC ĐẾM 100% VỚI BÃI ĐỖ XE
            var tatCaOTo = _context.LichSuCheckIns
                .Where(x => !string.IsNullOrEmpty(x.LoaiXe)
                         && (x.LoaiXe.Contains("tô") || x.LoaiXe.Contains("oto"))
                         && !string.IsNullOrEmpty(x.BienSoXe)
                         && x.BienSoXe != "undefined"
                         && x.BienSoXe != "null"
                         && x.BienSoXe != "---"
                         && x.BienSoXe != "N/A"
                         && !x.BienSoXe.Contains("máy"))
                .ToList();

            int occupiedOto = tatCaOTo
                .GroupBy(x => x.HoTen + "_" + x.BienSoXe) // Gom theo chủ xe và biển
                .Select(g => g.OrderByDescending(x => x.ThoiGian).FirstOrDefault())
                .Where(last => last != null && last.Huong == "Đi Vào") // Chỉ đếm xe ĐANG TRONG BÃI
                .Count();

            ViewBag.OccupiedOto = occupiedOto; // Gửi số liệu cực chuẩn sang Trạm Giám Sát

            return View();
        }

        // ========================================================
        // API NẠP DỮ LIỆU CHO MẮT THẦN AI (ĐÃ CẬP NHẬT XE CHO KHÁCH HÀNG)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> GetFaceData()
        {
            // 1. DỮ LIỆU NHÂN VIÊN (Đã chuẩn)
            var nhanViens = await _context.NhanViens
                .Where(n => !string.IsNullOrEmpty(n.FaceDataPath) && n.FaceDataPath != "chua_co_anh.jpg")
                .Select(n => new {
                    name = n.HoTen,
                    role = string.IsNullOrEmpty(n.ChucVu) ? "Nhân viên nội bộ" : n.ChucVu,
                    manv = n.MaNV ?? "NV",
                    bienso = string.IsNullOrEmpty(n.BienSoXe) ? "---" : n.BienSoXe,
                    loaixe = string.IsNullOrEmpty(n.LoaiXe) ? "Không có" : n.LoaiXe,
                    gioVaoCa = n.GioVaoCa.ToString(@"hh\:mm"),
                    gioXinDiMuon = n.GioXinDiMuon.HasValue ? n.GioXinDiMuon.Value.ToString(@"hh\:mm") : "",
                    image = n.FaceDataPath
                }).ToListAsync();

            // 2. DỮ LIỆU KHÁCH HẸN (ĐÃ FIX: Lấy Phương tiện & Biển số thực tế từ Database)
            var homNay = DateTime.Today;
            var khachHens = await _context.DangKyKhachs
                .Where(x => x.TrangThaiDuyet == "Đã duyệt"
                         && x.ThoiGianHen.Date >= homNay
                         && !string.IsNullOrEmpty(x.FaceDataPath))
                .Select(x => new {
                    name = x.HoTen,
                    role = "Khách Hẹn (" + x.BoPhanCanGap + ")",
                    manv = "KHACH",

                    // Lấy Biển số và Loại xe thật của Khách hàng
                    bienso = string.IsNullOrEmpty(x.BienSoXe) ? "---" : x.BienSoXe,
                    loaixe = string.IsNullOrEmpty(x.LoaiXe) ? "Không có" : x.LoaiXe,

                    gioVaoCa = x.ThoiGianHen.ToString("HH:mm"),
                    gioXinDiMuon = "",
                    image = x.FaceDataPath
                }).ToListAsync();

            // 3. Gộp chung 2 bộ não lại
            var aiDatabase = nhanViens.Concat(khachHens).ToList();

            return Json(aiDatabase);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Extensions() => View();
        public IActionResult BaiDoXe() => View();
        public IActionResult BanDoAnNinh() => View();
        public IActionResult ThietBiAI() => View();
        public IActionResult SuCoCanhBao() => View();
        public IActionResult NhatKyHeThong() => View();
        public IActionResult CauHinhHeThong() => View();
    }
}