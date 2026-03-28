using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Bắt buộc phải có để dùng Async

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // BIẾN HÀM THÀNH ASYNC ĐỂ KHÔNG BỊ TREO SERVER
        public async Task<IActionResult> Index()
        {
            var homNay = DateTime.Today;
            var ngayMai = homNay.AddDays(1); // Mẹo: Dùng khoảng thời gian để SQL tìm cực nhanh

            // 1. TỔNG QUAN HÔM NAY (Đếm nhanh không kéo dữ liệu về RAM)
            ViewBag.TongRaVao = await _context.LichSuCheckIns
                .CountAsync(x => x.ThoiGian >= homNay && x.ThoiGian < ngayMai);

            ViewBag.KhachNgoai = await _context.LichSuCheckIns
                .CountAsync(x => x.ThoiGian >= homNay && x.ThoiGian < ngayMai && x.LoaiDoiTuong.Contains("Khách"));

            ViewBag.CanhBao = await _context.LichSuCheckIns
                .CountAsync(x => x.ThoiGian >= homNay && x.ThoiGian < ngayMai && x.TrangThai == "Không hợp lệ");

            // 2. LOGIC TÍNH TOÁN ĐI TRỄ THÔNG MINH
            int dungGio = 0;
            int diTre = 0;

            // CỰC KỲ QUAN TRỌNG: Dùng .Select() để KHÔNG lấy cột Hình Ảnh (chuỗi Base64 siêu nặng)
            var checkInHomNay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= homNay && x.ThoiGian < ngayMai && x.Huong == "Đi Vào" && x.TrangThai == "Thành Công")
                .Select(x => new { x.HoTen, x.ThoiGian }) // Chỉ lấy đúng Tên và Giờ
                .ToListAsync();

            // Cũng bỏ qua cột Ảnh của Nhân viên cho nhẹ
            var danhSachNV = await _context.NhanViens
                .Where(n => !n.ChucVu.Contains("Khách"))
                .Select(n => new { n.HoTen, n.GioVaoCa, n.GioXinDiMuon })
                .ToListAsync();

            foreach (var nv in danhSachNV)
            {
                var logVao = checkInHomNay.OrderBy(x => x.ThoiGian).FirstOrDefault(x => x.HoTen == nv.HoTen);

                if (logVao != null)
                {
                    TimeSpan mốcGiờ = nv.GioXinDiMuon ?? nv.GioVaoCa;
                    if (logVao.ThoiGian.TimeOfDay > mốcGiờ)
                    {
                        diTre++;
                    }
                    else
                    {
                        dungGio++;
                    }
                }
            }

            int chuaCheckIn = danhSachNV.Count - (dungGio + diTre);

            ViewBag.DungGio = dungGio;
            ViewBag.DiTre = diTre;
            ViewBag.ChuaCheckIn = chuaCheckIn > 0 ? chuaCheckIn : 0;

            // 3. DỮ LIỆU BIỂU ĐỒ BẬC THANG (7 NGÀY GẦN NHẤT)
            var bayNgayQua = Enumerable.Range(0, 7).Select(i => homNay.AddDays(-i)).Reverse().ToList();

            // Lấy 1 lần duy nhất danh sách mốc thời gian của 7 ngày để vẽ biểu đồ cho lẹ
            var ngayCuNhat = bayNgayQua.First();
            var data7Ngay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= ngayCuNhat && x.ThoiGian < ngayMai)
                .Select(x => x.ThoiGian)
                .ToListAsync();

            var dataBieuDoCot = bayNgayQua.Select(ngay =>
                data7Ngay.Count(t => t.Date == ngay.Date)
            ).ToList();

            ViewBag.LabelsCot = string.Join(",", bayNgayQua.Select(d => $"'{d:dd/MM}'"));
            ViewBag.DataCot = string.Join(",", dataBieuDoCot);

            return View();
        }
    }
}