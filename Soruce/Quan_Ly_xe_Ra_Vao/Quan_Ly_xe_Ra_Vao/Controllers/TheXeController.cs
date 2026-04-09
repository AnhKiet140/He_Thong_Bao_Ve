using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System;
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

        // HÀM HỖ TRỢ: TỰ ĐỘNG TÍNH TOÁN MÃ THẺ TIẾP THEO (KH-001, NV-001)
        private void PrepareNextCardCodes()
        {
            var lastKH = _context.TheXes.Where(x => x.MaThe.StartsWith("KH-")).OrderByDescending(x => x.MaThe).FirstOrDefault();
            int nextKh = 1;
            if (lastKH != null && lastKH.MaThe.Length >= 6 && int.TryParse(lastKH.MaThe.Substring(3), out int numKh)) nextKh = numKh + 1;
            ViewBag.NextKH = $"KH-{nextKh:D3}";

            var lastNV = _context.TheXes.Where(x => x.MaThe.StartsWith("NV-")).OrderByDescending(x => x.MaThe).FirstOrDefault();
            int nextNv = 1;
            if (lastNV != null && lastNV.MaThe.Length >= 6 && int.TryParse(lastNV.MaThe.Substring(3), out int numNv)) nextNv = numNv + 1;
            ViewBag.NextNV = $"NV-{nextNv:D3}";
        }

        public IActionResult Index(int? searchTrangThai)
        {
            var query = _context.TheXes.AsQueryable();
            if (searchTrangThai.HasValue) query = query.Where(t => t.TrangThai == searchTrangThai.Value);
            ViewData["CurrentTrangThai"] = searchTrangThai;

            return View(query.ToList());
        }

        public IActionResult Create()
        {
            PrepareNextCardCodes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TheXe theXe, string HinhAnhBase64)
        {
            var isExist = _context.TheXes.Any(x => x.MaThe == theXe.MaThe);
            if (isExist)
            {
                TempData["Error"] = "Thẻ này bị trùng! Vui lòng chọn lại loại thẻ để lấy mã mới.";
                PrepareNextCardCodes();
                return View(theXe);
            }

            ModelState.Remove("GhiChu"); ModelState.Remove("NguoiGiu"); ModelState.Remove("HinhAnh");

            if (ModelState.IsValid)
            {
                theXe.GhiChu = theXe.GhiChu ?? "";
                theXe.NguoiGiu = theXe.NguoiGiu ?? "";
                if (!string.IsNullOrEmpty(HinhAnhBase64)) theXe.HinhAnh = HinhAnhBase64;

                _context.Add(theXe);
                await _context.SaveChangesAsync();

                // GHI NHẬT KÝ
                _context.NhatKyHeThongs.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    MucDo = "Thông tin",
                    NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Hệ Thống",
                    PhanHe = "Quản Lý Thẻ Cứng",
                    ChiTietThaoTac = $"Tạo mới thẻ vật lý thành công: {theXe.MaThe}"
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tạo thẻ " + theXe.MaThe + " thành công!";
                return RedirectToAction(nameof(Index));
            }

            PrepareNextCardCodes();
            return View(theXe);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var theXe = await _context.TheXes.FindAsync(id);
            if (theXe == null) return NotFound();
            return View(theXe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TheXe theXe, string HinhAnhBase64)
        {
            if (id != theXe.Id) return NotFound();

            var isExist = _context.TheXes.Any(x => x.MaThe == theXe.MaThe && x.Id != id);
            if (isExist)
            {
                TempData["Error"] = "Mã thẻ mới bị trùng với một thẻ khác đang có trên hệ thống!";
                return View(theXe);
            }

            ModelState.Remove("GhiChu"); ModelState.Remove("NguoiGiu"); ModelState.Remove("HinhAnh");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingThe = await _context.TheXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    theXe.GhiChu = theXe.GhiChu ?? "";
                    theXe.NguoiGiu = theXe.NguoiGiu ?? "";
                    theXe.HinhAnh = !string.IsNullOrEmpty(HinhAnhBase64) ? HinhAnhBase64 : existingThe.HinhAnh;

                    _context.Update(theXe);
                    await _context.SaveChangesAsync();

                    // GHI NHẬT KÝ
                    _context.NhatKyHeThongs.Add(new NhatKyHeThong
                    {
                        ThoiGian = DateTime.Now,
                        MucDo = "Cảnh báo",
                        NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Hệ Thống",
                        PhanHe = "Quản Lý Thẻ Cứng",
                        ChiTietThaoTac = $"Cập nhật thông tin thẻ: {theXe.MaThe}"
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật dữ liệu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TheXes.Any(e => e.Id == theXe.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(theXe);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var theXe = await _context.TheXes.FindAsync(id);
            if (theXe != null)
            {
                _context.TheXes.Remove(theXe);
                await _context.SaveChangesAsync();

                // GHI NHẬT KÝ
                _context.NhatKyHeThongs.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    MucDo = "Nguy hiểm",
                    NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Hệ Thống",
                    PhanHe = "Quản Lý Thẻ Cứng",
                    ChiTietThaoTac = $"Xóa thẻ vật lý khỏi hệ thống: {theXe.MaThe}"
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa thẻ khỏi hệ thống.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}