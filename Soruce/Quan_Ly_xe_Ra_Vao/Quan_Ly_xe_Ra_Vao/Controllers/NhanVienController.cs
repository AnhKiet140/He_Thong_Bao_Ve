using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NhanVienController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. DANH SÁCH & TÌM KIẾM
        public async Task<IActionResult> Index(string searchString, string searchChucVu)
        {
            var danhSach = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                danhSach = danhSach.Where(n => n.HoTen.Contains(searchString) || n.MaNV.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(searchChucVu))
            {
                danhSach = (searchChucVu == "Khach")
                    ? danhSach.Where(n => n.ChucVu.Contains("Khách"))
                    : danhSach.Where(n => n.ChucVu == searchChucVu);
            }

            ViewBag.ListChucVu = await _context.NhanViens.Where(n => !n.ChucVu.Contains("Khách"))
                                                         .Select(n => n.ChucVu).Distinct().ToListAsync();
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentChucVu"] = searchChucVu;

            return View(await danhSach.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            ModelState.Remove("FaceDataPath");

            if (ModelState.IsValid)
            {
                // --- XỬ LÝ LƯU ẢNH ---
                if (!string.IsNullOrEmpty(nhanVien.FaceDataPath) && nhanVien.FaceDataPath.StartsWith("data:image"))
                {
                    string base64Data = nhanVien.FaceDataPath.Substring(nhanVien.FaceDataPath.IndexOf(",") + 1);
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    string fileName = $"face_{nhanVien.MaNV}_{DateTime.Now.Ticks}.jpg";
                    string uploadFolder = Path.Combine(_env.WebRootPath, "images", "faces");

                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string filePath = Path.Combine(uploadFolder, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    nhanVien.FaceDataPath = $"/images/faces/{fileName}";
                }
                else
                {
                    nhanVien.FaceDataPath = "/images/no-avatar.png";
                }

                nhanVien.HasFingerprint = false;
                if (string.IsNullOrEmpty(nhanVien.LoaiXe)) nhanVien.LoaiXe = "Không có";
                if (string.IsNullOrEmpty(nhanVien.BienSoXe)) nhanVien.BienSoXe = "---";

                _context.Add(nhanVien);
                await _context.SaveChangesAsync();

                // --- GHI NHẬT KÝ ---
                _context.NhatKyHeThongs.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    MucDo = "Thông tin",
                    NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Tổ Bảo Vệ",
                    PhanHe = "SINH TRẮC HỌC NV",
                    ChiTietThaoTac = $"Thêm mới nhân sự: {nhanVien.HoTen} (Mã: {nhanVien.MaNV})"
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm mới nhân sự thành công!";
                return RedirectToAction("Index");
            }
            return View(nhanVien);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();
            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVien)
        {
            if (id != nhanVien.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(nhanVien.FaceDataPath) && nhanVien.FaceDataPath.StartsWith("data:image"))
                    {
                        string base64Data = nhanVien.FaceDataPath.Substring(nhanVien.FaceDataPath.IndexOf(",") + 1);
                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        string fileName = $"face_{nhanVien.MaNV}_{DateTime.Now.Ticks}.jpg";
                        string uploadFolder = Path.Combine(_env.WebRootPath, "images", "faces");

                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                        string filePath = Path.Combine(uploadFolder, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                        nhanVien.FaceDataPath = $"/images/faces/{fileName}";
                    }

                    _context.Update(nhanVien);
                    await _context.SaveChangesAsync();

                    // --- GHI NHẬT KÝ ---
                    _context.NhatKyHeThongs.Add(new NhatKyHeThong
                    {
                        ThoiGian = DateTime.Now,
                        MucDo = "Cảnh báo",
                        NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Tổ Bảo Vệ",
                        PhanHe = "SINH TRẮC HỌC NV",
                        ChiTietThaoTac = $"Cập nhật hồ sơ sinh trắc học nhân viên: {nhanVien.HoTen}"
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            return View(nhanVien);
        }

        public async Task<IActionResult> Details(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();
            return View(nhanVien);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                string tenNV = nhanVien.HoTen;
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();

                // --- GHI NHẬT KÝ ---
                _context.NhatKyHeThongs.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    MucDo = "Nguy hiểm",
                    NguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Tổ Bảo Vệ",
                    PhanHe = "SINH TRẮC HỌC NV",
                    ChiTietThaoTac = $"Xóa nhân sự khỏi hệ thống: {tenNV}"
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa nhân sự khỏi hệ thống!";
            }
            return RedirectToAction("Index");
        }
    }
}