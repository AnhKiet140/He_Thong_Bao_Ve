using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize] // KHÓA TỔNG: Bắt buộc đăng nhập để vào Controller này
    public class DangKyKhachController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DangKyKhachController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. KHU VỰC DÀNH CHO KHÁCH NGOÀI (Không cần đăng nhập)
        // =======================================================

        [AllowAnonymous] // MỞ KHÓA: Cho phép khách lạ (chưa đăng nhập) vào xem trang Form QR
        public IActionResult DangKyOnline()
        {
            return View();
        }

        [AllowAnonymous] // MỞ KHÓA: Cho phép khách lạ bấm gửi Form
        [HttpPost]
        public async Task<IActionResult> SubmitDangKy([FromBody] DangKyKhach model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FaceDataPath))
                {
                    return Json(new { success = false, message = "Lỗi: Chưa có ảnh nhận diện khuôn mặt!" });
                }

                model.TrangThaiDuyet = "Chờ duyệt"; // Mặc định phải chờ duyệt

                _context.DangKyKhachs.Add(model);
                await _context.SaveChangesAsync();

                // LƯU Ý QUAN TRỌNG: Trả về KhachId để Frontend có dữ liệu vẽ mã QR!
                // Phải đảm bảo model.Id (hoặc tên cột khóa chính) tồn tại và được SQL tự sinh ra
                return Json(new
                {
                    success = true,
                    message = "Đăng ký thành công! Vui lòng chờ bộ phận liên quan phê duyệt.",
                    khachId = model.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }


        // =======================================================
        // 2. KHU VỰC DÀNH CHO NỘI BỘ (Phải đăng nhập)
        // =======================================================

        // GET: Màn hình hiển thị danh sách khách đang chờ
        public IActionResult DanhSachChoDuyet(string searchName, string filterBoPhan)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var homNay = DateTime.Today;

            // 1. Lấy khách "Chờ duyệt" VÀ "Đã duyệt" của ngày hôm nay trở đi
            var query = _context.DangKyKhachs.Where(x =>
                (x.TrangThaiDuyet == "Chờ duyệt" || x.TrangThaiDuyet == "Đã duyệt") &&
                x.ThoiGianHen.Date >= homNay);

            // 2. PHÂN LUỒNG THÔNG BÁO TỚI ĐÚNG NGƯỜI
            if (userRole == "GiamDoc") query = query.Where(x => x.BoPhanCanGap == "Ban Giám Đốc");
            else if (userRole == "NhanSu") query = query.Where(x => x.BoPhanCanGap == "Phòng Nhân Sự");
            else if (userRole == "KeToan") query = query.Where(x => x.BoPhanCanGap == "Phòng Kế Toán");
            else if (userRole == "BaoVe") query = query.Where(x => false); // Bảo Vệ bị ẩn danh sách này

            // 3. Lấy danh sách các phòng ban đang có khách để đổ vào Dropdown lọc
            ViewBag.BoPhanList = query.Select(x => x.BoPhanCanGap).Distinct().ToList();

            // 4. XỬ LÝ TÌM KIẾM & LỌC THEO YÊU CẦU
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName)); // Tìm theo tên
            }
            if (!string.IsNullOrEmpty(filterBoPhan))
            {
                query = query.Where(x => x.BoPhanCanGap == filterBoPhan); // Lọc theo bộ phận
            }

            var danhSach = query.OrderByDescending(x => x.ThoiGianHen).ToList();

            // Lưu lại các từ khóa để hiển thị lại trên giao diện
            ViewBag.UserRole = userRole;
            ViewBag.SearchName = searchName;
            ViewBag.FilterBoPhan = filterBoPhan;

            return View(danhSach);
        }

        // POST: Nút bấm Duyệt hoặc Từ chối
        [HttpPost]
        public async Task<IActionResult> XuLyDuyet(int id, string hanhDong)
        {
            var khach = await _context.DangKyKhachs.FindAsync(id);
            if (khach == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hồ sơ khách!" });
            }

            // BẢO MẬT KÉP - CHỐNG HACK API CHÉO PHÒNG BAN
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole == "BaoVe" && khach.BoPhanCanGap == "Ban Giám Đốc")
                return Json(new { success = false, message = "CẢNH BÁO: Bạn không có thẩm quyền duyệt khách của Ban Giám Đốc!" });

            if (userRole == "GiamDoc" && khach.BoPhanCanGap != "Ban Giám Đốc")
                return Json(new { success = false, message = "Lỗi: Không thể thao tác hồ sơ của phòng ban khác!" });

            if (userRole == "NhanSu" && khach.BoPhanCanGap != "Phòng Nhân Sự")
                return Json(new { success = false, message = "Lỗi: Không thể thao tác hồ sơ của phòng ban khác!" });

            if (userRole == "KeToan" && khach.BoPhanCanGap != "Phòng Kế Toán")
                return Json(new { success = false, message = "Lỗi: Không thể thao tác hồ sơ của phòng ban khác!" });

            // Xử lý trạng thái
            if (hanhDong == "Duyet")
            {
                khach.TrangThaiDuyet = "Đã duyệt";
            }
            else
            {
                khach.TrangThaiDuyet = "Từ chối";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xử lý thành công!" });
        }

        // =======================================================
        // BẢO VỆ XÁC NHẬN KHÁCH VÀO CỔNG (CHUYỂN DỮ LIỆU SANG SỔ TRỰC BAN)
        // =======================================================
        [HttpPost]
        [Authorize(Roles = "Admin,BaoVe")] // Chỉ Bảo vệ và Admin mới được phép bấm
        public async Task<IActionResult> CheckInTaiCong(int id)
        {
            try
            {
                // 1. Tìm thông tin khách hẹn
                var khach = await _context.DangKyKhachs.FindAsync(id);
                if (khach == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu khách!" });

                if (khach.TrangThaiDuyet != "Đã duyệt")
                    return Json(new { success = false, message = "Khách chưa được duyệt, không thể cho vào!" });

                // 2. Tạo bản ghi mới bên Sổ Trực Ban (Lịch Sử Check-in)
                var nhatKy = new LichSuCheckIn
                {
                    HoTen = khach.HoTen,
                    ThoiGian = DateTime.Now,
                    BienSoXe = khach.BienSoXe,
                    HinhAnh = khach.FaceDataPath,
                    LoaiDoiTuong = "Khách Hẹn (" + khach.BoPhanCanGap + ")",
                    PhuongThuc = "Bảo vệ mở",
                    Huong = "Đi Vào",
                    TrangThai = "Thành Công"
                };

                // 3. Đổi trạng thái của khách
                khach.TrangThaiDuyet = "Đã vào bãi";

                // 4. Lưu cả 2 thay đổi vào Database
                _context.LichSuCheckIns.Add(nhatKy);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã chuyển dữ liệu sang Sổ Trực Ban thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // =======================================================
        // LỌC DANH SÁCH HỒ SƠ THEO ROLE TẠI DANH BẠ TỔNG
        // =======================================================
        // GET: Bảng danh sách toàn bộ khách hẹn
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, DateTime? selectedDate)
        {
            var query = _context.DangKyKhachs.AsQueryable();
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            // Lọc dữ liệu hiển thị theo Role
            if (userRole == "GiamDoc") query = query.Where(x => x.BoPhanCanGap == "Ban Giám Đốc");
            else if (userRole == "NhanSu") query = query.Where(x => x.BoPhanCanGap == "Phòng Nhân Sự");
            else if (userRole == "KeToan") query = query.Where(x => x.BoPhanCanGap == "Phòng Kế Toán");

            // Lọc theo Ngày
            if (selectedDate.HasValue)
            {
                query = query.Where(k => k.ThoiGianHen.Date == selectedDate.Value.Date);
                ViewBag.SelectedDate = selectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                query = query.Where(k => k.ThoiGianHen.Date >= DateTime.Today);
            }

            // Lọc theo Tên Khách
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(k => k.HoTen.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            var result = await query.OrderByDescending(k => k.ThoiGianHen).ToListAsync();
            return View(result);
        }

        // GET: Xem chi tiết 1 hồ sơ
        public IActionResult Details(int id)
        {
            var khach = _context.DangKyKhachs.Find(id);
            if (khach == null) return NotFound();
            return View(khach);
        }

        // Xóa hồ sơ khách 
        public async Task<IActionResult> Delete(int id)
        {
            var khach = await _context.DangKyKhachs.FindAsync(id);
            if (khach != null)
            {
                _context.DangKyKhachs.Remove(khach);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}