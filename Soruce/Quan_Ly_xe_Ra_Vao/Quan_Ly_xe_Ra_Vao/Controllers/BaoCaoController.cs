using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize]
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ĐÃ NÂNG CẤP: Thêm tham số selectedDate để nhận lệnh Lọc từ giao diện
        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            // 1. XÁC ĐỊNH NGÀY CẦN BÁO CÁO (Nếu không chọn thì lấy Hôm nay)
            DateTime filterDate = selectedDate ?? DateTime.Today;
            DateTime nextDate = filterDate.AddDays(1); // Tạo khoảng mốc 24h để tìm kiếm SQL cực nhanh

            // Gửi ngày đang xem xuống View để hiển thị
            ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");
            ViewBag.IsToday = filterDate.Date == DateTime.Today;

            // 2. TỔNG QUAN (Toàn bộ logic đếm đều được tính theo filterDate)
            ViewBag.TongVaoRa = await _context.LichSuCheckIns
                .CountAsync(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate);

            ViewBag.KhachNgoai = await _context.DangKyKhachs
                .CountAsync(x => x.ThoiGianHen.Date == filterDate.Date && x.TrangThaiDuyet == "Đã duyệt");

            // Đếm những người bị sai mặt / lạ
            ViewBag.CanhBao = await _context.LichSuCheckIns
                .CountAsync(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate && x.TrangThai != "Thành Công");

            // 3. LOGIC TÍNH TOÁN ĐI TRỄ THÔNG MINH (Giữ nguyên siêu tối ưu của bạn)
            int dungGio = 0;
            int diTre = 0;

            var checkInTrongNgay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate && x.Huong == "Đi Vào" && x.TrangThai == "Thành Công")
                .Select(x => new { x.HoTen, x.ThoiGian }) // Tránh Load Base64 ảnh
                .ToListAsync();

            var danhSachNV = await _context.NhanViens
                .Where(n => !n.ChucVu.Contains("Khách"))
                .Select(n => new { n.HoTen, n.GioVaoCa, n.GioXinDiMuon })
                .ToListAsync();

            foreach (var nv in danhSachNV)
            {
                var logVao = checkInTrongNgay.OrderBy(x => x.ThoiGian).FirstOrDefault(x => x.HoTen == nv.HoTen);

                if (logVao != null)
                {
                    TimeSpan mocGio = nv.GioXinDiMuon ?? nv.GioVaoCa;
                    if (logVao.ThoiGian.TimeOfDay > mocGio) diTre++;
                    else dungGio++;
                }
            }

            int chuaCheckIn = danhSachNV.Count - (dungGio + diTre);

            ViewBag.DungGio = dungGio;
            ViewBag.DiTre = diTre;
            ViewBag.ChuaCheckIn = chuaCheckIn > 0 ? chuaCheckIn : 0;

            // 4. DỮ LIỆU BIỂU ĐỒ BẬC THANG (7 NGÀY GẦN NHẤT tính từ ngày chọn)
            var bayNgayQua = Enumerable.Range(0, 7).Select(i => filterDate.AddDays(-i)).Reverse().ToList();
            var ngayCuNhat = bayNgayQua.First();

            var data7Ngay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= ngayCuNhat && x.ThoiGian < nextDate)
                .Select(x => x.ThoiGian)
                .ToListAsync();

            var dataBieuDoCot = bayNgayQua.Select(ngay =>
                data7Ngay.Count(t => t.Date == ngay.Date)
            ).ToList();

            ViewBag.LabelsCot = string.Join(",", bayNgayQua.Select(d => $"'{d:dd/MM}'"));
            ViewBag.DataCot = string.Join(",", dataBieuDoCot);

            // ======================================================
            // 5. MỚI: LẤY CHI TIẾT BẢNG RA VÀO ĐỂ HIỆN Ở DƯỚI & XUẤT EXCEL
            // ======================================================
            var chiTiet = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            // Truyền danh sách chi tiết này xuống cho View dưới dạng Model
            return View(chiTiet);
        }
    }
}