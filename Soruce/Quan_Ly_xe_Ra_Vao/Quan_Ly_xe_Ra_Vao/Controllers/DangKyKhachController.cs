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
    [Authorize]
    public class DangKyKhachController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DangKyKhachController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. KHU VỰC DÀNH CHO KHÁCH NGOÀI (AllowAnonymous)
        // =======================================================

        [AllowAnonymous]
        public IActionResult DangKyOnline()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SubmitDangKy([FromBody] DangKyKhach model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FaceDataPath))
                {
                    return Json(new { success = false, message = "Lỗi: Chưa có ảnh nhận diện khuôn mặt!" });
                }

                model.TrangThaiDuyet = "Chờ duyệt";
                _context.DangKyKhachs.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đăng ký thành công!", khachId = model.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // =======================================================
        // 2. KHU VỰC NỘI BỘ (Phân quyền duyệt linh hoạt)
        // =======================================================

        public IActionResult DanhSachChoDuyet(string searchName, string filterBoPhan)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentName = User.Identity.Name ?? "";
            var homNay = DateTime.Today;

            var query = _context.DangKyKhachs.Where(x =>
                (x.TrangThaiDuyet == "Chờ duyệt" || x.TrangThaiDuyet == "Đã duyệt") &&
                x.ThoiGianHen.Date >= homNay);

            // CẬP NHẬT LOGIC HIỂN THỊ:
            // Bảo vệ thấy tất cả khách trong ngày.
            // Các Sếp thấy khách của phòng mình hoặc khách đích danh gặp mình.
            if (userRole != "Admin" && userRole != "BaoVe")
            {
                if (userRole == "GiamDoc")
                    query = query.Where(x => x.BoPhanCanGap == "BOD" || x.BoPhanCanGap == "Ban Giám Đốc" || x.NhanVienCanGap == currentName);
                else if (userRole == "NhanSu")
                    query = query.Where(x => x.BoPhanCanGap == "HR" || x.BoPhanCanGap == "Phòng Nhân Sự" || x.NhanVienCanGap == currentName);
                else if (userRole == "KeToan")
                    query = query.Where(x => x.BoPhanCanGap == "ACCOUNTING" || x.BoPhanCanGap == "Phòng Kế Toán" || x.NhanVienCanGap == currentName);
                else
                    // Người dùng bình thường chỉ thấy khách hẹn gặp đúng tên mình
                    query = query.Where(x => x.NhanVienCanGap == currentName);
            }

            ViewBag.BoPhanList = query.Select(x => x.BoPhanCanGap).Distinct().ToList();

            if (!string.IsNullOrEmpty(searchName)) query = query.Where(x => x.HoTen.Contains(searchName));
            if (!string.IsNullOrEmpty(filterBoPhan)) query = query.Where(x => x.BoPhanCanGap == filterBoPhan);

            var danhSach = query.OrderByDescending(x => x.ThoiGianHen).ToList();

            ViewBag.UserRole = userRole;
            ViewBag.SearchName = searchName;
            ViewBag.FilterBoPhan = filterBoPhan;

            return View(danhSach);
        }

        [HttpPost]
        public async Task<IActionResult> XuLyDuyet(int id, string hanhDong)
        {
            var khach = await _context.DangKyKhachs.FindAsync(id);
            if (khach == null) return Json(new { success = false, message = "Không tìm thấy hồ sơ khách!" });

            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentName = User.Identity.Name ?? "";

            // CẬP NHẬT LOGIC DUYỆT:
            bool coQuyenDuyet = false;

            if (userRole == "Admin" || userRole == "BaoVe")
                coQuyenDuyet = true; // Bảo vệ có quyền duyệt nhanh tại cổng
            else if (khach.NhanVienCanGap == currentName)
                coQuyenDuyet = true; // Người được hẹn có quyền duyệt khách của mình
            else if (userRole == "GiamDoc" && (khach.BoPhanCanGap == "BOD" || khach.BoPhanCanGap == "Ban Giám Đốc"))
                coQuyenDuyet = true;
            else if (userRole == "NhanSu" && (khach.BoPhanCanGap == "HR" || khach.BoPhanCanGap == "Phòng Nhân Sự"))
                coQuyenDuyet = true;

            if (!coQuyenDuyet)
                return Json(new { success = false, message = "Bạn không có thẩm quyền duyệt hồ sơ này!" });

            khach.TrangThaiDuyet = (hanhDong == "Duyet") ? "Đã duyệt" : "Từ chối";

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xử lý thành công!" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,BaoVe")]
        public async Task<IActionResult> CheckInTaiCong(int id)
        {
            try
            {
                var khach = await _context.DangKyKhachs.FindAsync(id);
                if (khach == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu khách!" });

                if (khach.TrangThaiDuyet != "Đã duyệt")
                    return Json(new { success = false, message = "Khách chưa được duyệt!" });

                string tenPhongBan = khach.BoPhanCanGap == "BOD" ? "Ban Giám Đốc" : (khach.BoPhanCanGap == "HR" ? "Phòng Nhân Sự" : khach.BoPhanCanGap);

                var nhatKy = new LichSuCheckIn
                {
                    HoTen = khach.HoTen,
                    ThoiGian = DateTime.Now,
                    BienSoXe = khach.BienSoXe,
                    HinhAnh = khach.FaceDataPath,
                    LoaiDoiTuong = "Khách Hẹn (" + tenPhongBan + ")",
                    PhuongThuc = "Bảo vệ xác nhận",
                    Huong = "Đi Vào",
                    TrangThai = "Thành Công"
                };

                khach.TrangThaiDuyet = "Đã vào bãi";
                _context.LichSuCheckIns.Add(nhatKy);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xác nhận khách vào cổng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public async Task<IActionResult> Index(string searchString, DateTime? selectedDate)
        {
            var query = _context.DangKyKhachs.AsQueryable();
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentName = User.Identity.Name ?? "";

            // Đồng bộ bộ lọc hiển thị cho trang danh bạ khách
            if (userRole != "Admin" && userRole != "BaoVe")
            {
                if (userRole == "GiamDoc")
                    query = query.Where(x => x.BoPhanCanGap == "BOD" || x.BoPhanCanGap == "Ban Giám Đốc" || x.NhanVienCanGap == currentName);
                else if (userRole == "NhanSu")
                    query = query.Where(x => x.BoPhanCanGap == "HR" || x.BoPhanCanGap == "Phòng Nhân Sự" || x.NhanVienCanGap == currentName);
                else
                    query = query.Where(x => x.NhanVienCanGap == currentName);
            }

            if (selectedDate.HasValue)
                query = query.Where(k => k.ThoiGianHen.Date == selectedDate.Value.Date);
            else
                query = query.Where(k => k.ThoiGianHen.Date >= DateTime.Today);

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(k => k.HoTen.Contains(searchString));

            return View(await query.OrderByDescending(k => k.ThoiGianHen).ToListAsync());
        }

        public IActionResult Details(int id)
        {
            var khach = _context.DangKyKhachs.Find(id);
            if (khach == null) return NotFound();
            return View(khach);
        }

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