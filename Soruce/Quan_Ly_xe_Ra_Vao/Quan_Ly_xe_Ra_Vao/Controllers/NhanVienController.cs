using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NhanVienController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH & TÌM KIẾM
        // 1. DANH SÁCH & TÌM KIẾM CÓ BỘ LỌC CHỨC VỤ
        public IActionResult Index(string searchString, string searchChucVu)
        {
            var danhSach = _context.NhanViens.AsQueryable();

            // Lọc theo Tên hoặc Mã
            if (!string.IsNullOrEmpty(searchString))
            {
                danhSach = danhSach.Where(n => n.HoTen.Contains(searchString) || n.MaNV.Contains(searchString));
            }

            // Lọc theo Chức vụ / Khách ngoài
            if (!string.IsNullOrEmpty(searchChucVu))
            {
                if (searchChucVu == "Khach")
                {
                    danhSach = danhSach.Where(n => n.ChucVu.Contains("Khách"));
                }
                else
                {
                    danhSach = danhSach.Where(n => n.ChucVu == searchChucVu);
                }
            }

            // Lấy danh sách các chức vụ (loại bỏ từ Khách để gộp chung vào 1 mục)
            var listChucVu = _context.NhanViens.Where(n => !n.ChucVu.Contains("Khách"))
                                                .Select(n => n.ChucVu).Distinct().ToList();
            ViewBag.ListChucVu = listChucVu;

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentChucVu"] = searchChucVu;

            return View(danhSach.ToList());
        }

        // 2. MỞ FORM TẠO MỚI NHÂN VIÊN
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ LƯU KHI BẤM NÚT TẠO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            ModelState.Remove("FaceDataPath");

            if (ModelState.IsValid)
            {
                nhanVien.FaceDataPath = "chua_co_anh.jpg";
                nhanVien.HasFingerprint = false;

                _context.Add(nhanVien);
                await _context.SaveChangesAsync();

                // Nhảy thẳng sang trang Lấy sinh trắc học sau khi tạo xong
                return RedirectToAction("Edit", new { id = nhanVien.Id });
            }
            return View(nhanVien);
        }

        // 4. MỞ TRANG THU NHẬN SINH TRẮC HỌC (EDIT)
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // 5. LƯU ẢNH VÀ VÂN TAY VÀO DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVien)
        {
            if (id != nhanVien.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(nhanVien);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); // Quay lại danh sách
            }
            return View(nhanVien);
        }

        // 6. XEM HỒ SƠ ĐỊNH DANH (DETAILS)
        public async Task<IActionResult> Details(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // 7. XÓA NHÂN VIÊN / KHÁCH (NÚT XÓA)
        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index"); // Xóa xong load lại trang
        }
    }
}