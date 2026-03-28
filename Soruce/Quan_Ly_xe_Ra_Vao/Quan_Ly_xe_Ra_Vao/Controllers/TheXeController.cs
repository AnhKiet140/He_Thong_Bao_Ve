using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class TheXeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TheXeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH & BỘ LỌC
        public IActionResult Index(string searchLoaiThe, int? searchTrangThai)
        {
            var query = _context.TheXes.AsQueryable();

            if (!string.IsNullOrEmpty(searchLoaiThe))
                query = query.Where(t => t.LoaiThe == searchLoaiThe);

            if (searchTrangThai.HasValue)
                query = query.Where(t => t.TrangThai == searchTrangThai.Value);

            ViewData["CurrentLoaiThe"] = searchLoaiThe;
            ViewData["CurrentTrangThai"] = searchTrangThai;

            return View(query.ToList());
        }

        // 2. MỞ FORM TẠO THẺ
        public IActionResult Create()
        {
            return View();
        }

        // LƯU DỮ LIỆU TẠO THẺ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TheXe theXe)
        {
            ModelState.Remove("GhiChu");
            ModelState.Remove("NguoiGiu");

            if (ModelState.IsValid)
            {
                // THUỐC GIẢI LỖI NULL LÀ ĐÂY: Gán chuỗi rỗng nếu người dùng không nhập
                theXe.GhiChu = theXe.GhiChu ?? "";
                theXe.NguoiGiu = theXe.NguoiGiu ?? "";

                _context.Add(theXe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(theXe);
        }

        // 3. MỞ FORM CHỈNH SỬA
        public async Task<IActionResult> Edit(int id)
        {
            var theXe = await _context.TheXes.FindAsync(id);
            if (theXe == null) return NotFound();
            return View(theXe);
        }

        // LƯU DỮ LIỆU SỬA THẺ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TheXe theXe)
        {
            if (id != theXe.Id) return NotFound();

            ModelState.Remove("GhiChu");
            ModelState.Remove("NguoiGiu");

            if (ModelState.IsValid)
            {
                // THUỐC GIẢI LỖI NULL CHO HÀM EDIT:
                theXe.GhiChu = theXe.GhiChu ?? "";
                theXe.NguoiGiu = theXe.NguoiGiu ?? "";

                _context.Update(theXe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(theXe);
        }

        // 4. XÓA THẺ
        public async Task<IActionResult> Delete(int id)
        {
            var theXe = await _context.TheXes.FindAsync(id);
            if (theXe != null)
            {
                _context.TheXes.Remove(theXe);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}