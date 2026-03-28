using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class KhachNgoaiController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Bơm Database vào Controller
        public KhachNgoaiController(ApplicationDbContext context)
        {
            _context = context;
        }
  
        // 1. MỞ TRANG DANH SÁCH DUYỆT KHÁCH (Nâng cấp Async chống Timeout)
        public async Task<IActionResult> Index()
        {
            // Dùng await và ToListAsync() để Database tải ngầm, không bị đơ Server
            var danhSachChoDuyet = await _context.KhachNgoais
                                                 .Where(k => k.TrangThai == 0)
                                                 .ToListAsync();
            return View(danhSachChoDuyet);
        }

        // 2. MỞ FORM ĐĂNG KÝ
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ LƯU DỮ LIỆU KHI KHÁCH BẤM NÚT "GỬI YÊU CẦU"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachNgoai khach)
        {
            // Bỏ qua kiểm tra cột FaceDataPath từ ngoài Form
            ModelState.Remove("FaceDataPath");

            if (ModelState.IsValid)
            {
                khach.ThoiGianDangKy = DateTime.Now;
                khach.TrangThai = 0;

                // ---- THÊM ĐÚNG DÒNG NÀY ĐỂ FIX LỖI ----
                khach.FaceDataPath = "chua_co_anh.jpg"; // Gán giá trị tạm để SQL Server không báo lỗi
                // ---------------------------------------

                _context.Add(khach);
                await _context.SaveChangesAsync();   // Lưu thẳng vào CSDL

                return RedirectToAction("Index");
            }

            return View(khach);
        }

        // 4. HÀM XỬ LÝ NÚT "DUYỆT"
        public async Task<IActionResult> Duyet(int id)
        {
            // Tìm khách trong Database dựa vào ID
            var khach = await _context.KhachNgoais.FindAsync(id);
            if (khach == null) return NotFound();

            // Chuyển trạng thái thành 1 (Đã duyệt)
            khach.TrangThai = 1;

            // ĐỈNH CAO Ở ĐÂY: Copy khách này sang bảng Nhân Viên để AI nhận diện
            var nhanVienMoi = new NhanVien
            {
                MaNV = "KH-" + khach.Id.ToString() + "-" + DateTime.Now.ToString("ddMM"), // Tạo mã ID tự động (VD: KH-1-2403)
                HoTen = khach.HoTen,
                ChucVu = "Khách (Gặp: " + khach.PhongBanCanGap + ")", // Ghi chú rõ là Khách
                FaceDataPath = khach.FaceDataPath, // Dùng lại khuôn mặt khách đã chụp
                HasFingerprint = false // Khách thì chưa có vân tay
            };

            _context.NhanViens.Add(nhanVienMoi); // Lưu vào bảng Sinh Trắc Học

            await _context.SaveChangesAsync(); // Chốt lưu toàn bộ CSDL

            return RedirectToAction("Index"); // Tải lại trang danh sách
        }

        // 5. HÀM XỬ LÝ NÚT "TỪ CHỐI"
        public async Task<IActionResult> TuChoi(int id)
        {
            var khach = await _context.KhachNgoais.FindAsync(id);
            if (khach != null)
            {
                khach.TrangThai = 2; // 2 = Bị từ chối
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}