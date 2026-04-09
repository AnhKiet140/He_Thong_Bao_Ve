using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class CameraLogPayload
    {
        public string? HoTen { get; set; }
        public string? LoaiDoiTuong { get; set; }
        public string? BienSoXe { get; set; }
        public string? PhuongThuc { get; set; }
        public string? Huong { get; set; }
        public string? TrangThai { get; set; }
        public string? HinhAnh { get; set; }
        public string? ViTriDo { get; set; }
        public string? LoaiXe { get; set; }
    }

    [Authorize]
    public class LichSuCheckInController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LichSuCheckInController(ApplicationDbContext context) { _context = context; }

        // 1. TRANG LỊCH SỬ TỔNG QUÁT
        public async Task<IActionResult> Index(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName) || x.BienSoXe.Contains(searchName));
            }

            ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.SearchName = searchName;

            return View(await query.OrderByDescending(x => x.ThoiGian).ToListAsync());
        }

        // 2. TRANG SỔ TRỰC BAN
        [Authorize(Roles = "Admin,BaoVe")]
        public async Task<IActionResult> TrucBan(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName));
            }

            ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.SearchName = searchName;

            return View(await query.OrderByDescending(x => x.ThoiGian).ToListAsync());
        }

        // 3. API GHI NHẬT KÝ TỪ TRẠM GIÁM SÁT AI (Hàm quan trọng nhất)
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> GhiNhatKy([FromBody] CameraLogPayload data)
        {
            try
            {
                if (data == null || string.IsNullOrEmpty(data.HoTen))
                    return BadRequest("Dữ liệu không đầy đủ");

                // --- LOGIC 1: XOAY VÒNG HƯỚNG RA/VÀO ---
                string huongTuDong = "Đi Vào";
                var lastRecord = await _context.LichSuCheckIns
                    .Where(x => x.HoTen == data.HoTen && x.ThoiGian.Date == DateTime.Today)
                    .OrderByDescending(x => x.ThoiGian)
                    .FirstOrDefaultAsync();

                // Nếu lần gần nhất là "Vào" thì lần này là "Ra"
                if (lastRecord != null && lastRecord.Huong == "Đi Vào")
                {
                    huongTuDong = "Đi Ra";
                }

                // --- LOGIC 2: PHÂN LOẠI PHƯƠNG TIỆN & FIX BIỂN SỐ ---
                bool isXeMay = (data.BienSoXe == "undefined" || string.IsNullOrEmpty(data.BienSoXe) || data.LoaiXe == "XeMay");
                string finalLoaiXe = isXeMay ? "Xe máy" : "Ô tô";
                string finalBienSo = isXeMay ? "---" : data.BienSoXe!;

                // --- LOGIC 3: XỬ LÝ VỊ TRÍ ĐỖ ---
                // Chỉ lưu vị trí đỗ nếu là Ô tô và đang đi VÀO. Các trường hợp khác để null.
                string? viTriThucTe = (finalLoaiXe == "Ô tô" && huongTuDong == "Đi Vào") ? data.ViTriDo : null;

                // --- LOGIC 4: TẠO BẢN GHI ---
                var record = new LichSuCheckIn
                {
                    HoTen = data.HoTen,
                    LoaiDoiTuong = "Nhân viên",
                    LoaiXe = finalLoaiXe,
                    BienSoXe = finalBienSo,
                    PhuongThuc = "AI FaceID + ALPR",
                    Huong = huongTuDong,
                    TrangThai = "Thành Công",
                    HinhAnh = data.HinhAnh,
                    ViTriDo = viTriThucTe,
                    ThoiGian = DateTime.Now
                };

                _context.LichSuCheckIns.Add(record);
                await _context.SaveChangesAsync();

                return Json(new { success = true, huong = huongTuDong, loaiXe = finalLoaiXe });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi Server: {ex.Message}");
            }
        }
    }
}