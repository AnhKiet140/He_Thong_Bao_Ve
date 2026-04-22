using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel; // Thêm thư viện xuất Excel

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

        // =======================================================
        // TÍNH NĂNG XUẤT EXCEL CHUYÊN NGHIỆP CÓ ĐỊNH DẠNG & CHÈN ẢNH FACE ID
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string searchString) // Đã gỡ bỏ tham số phongBan gây lỗi
        {
            var query = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(n => n.HoTen.Contains(searchString) || n.MaNV.Contains(searchString));
            }

            var nhanViens = await query.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Dữ Liệu Sinh Trắc Học");

                // -------------------------------------------------------------
                // 1. THIẾT KẾ PHẦN ĐẦU BÁO CÁO
                // -------------------------------------------------------------
                var titleCompany = worksheet.Range("A1:F1");
                titleCompany.Merge().Value = "HỆ THỐNG KIỂM SOÁT AN NINH CHK-IN PRO";
                titleCompany.Style.Font.Bold = true;
                titleCompany.Style.Font.FontSize = 11;

                var titleReport = worksheet.Range("A3:F3");
                titleReport.Merge().Value = "BẢNG KÊ DỮ LIỆU SINH TRẮC HỌC NHÂN VIÊN";
                titleReport.Style.Font.Bold = true;
                titleReport.Style.Font.FontSize = 16;
                titleReport.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var titleFilter = worksheet.Range("A4:F4");
                titleFilter.Merge().Value = $"Tất cả nhân sự  |  Ngày trích xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                titleFilter.Style.Font.Italic = true;
                titleFilter.Style.Font.FontSize = 10;
                titleFilter.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // -------------------------------------------------------------
                // 2. THIẾT KẾ CỘT TIÊU ĐỀ
                // -------------------------------------------------------------
                int headerRowIdx = 6;
                worksheet.Cell(headerRowIdx, 1).Value = "STT";
                worksheet.Cell(headerRowIdx, 2).Value = "MÃ NV/KHÁCH";
                worksheet.Cell(headerRowIdx, 3).Value = "HỌ VÀ TÊN";
                worksheet.Cell(headerRowIdx, 4).Value = "CHỨC VỤ / GHI CHÚ";
                worksheet.Cell(headerRowIdx, 5).Value = "DỮ LIỆU KHUÔN MẶT";
                worksheet.Cell(headerRowIdx, 6).Value = "DỮ LIỆU VÂN TAY";

                var headerRange = worksheet.Range($"A{headerRowIdx}:F{headerRowIdx}");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(198, 239, 206);
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Row(headerRowIdx).Height = 25;

                // -------------------------------------------------------------
                // 3. ĐỔ DỮ LIỆU VÀ CHÈN ẢNH VÀO BẢNG
                // -------------------------------------------------------------
                int row = headerRowIdx + 1;
                int stt = 1;
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                foreach (var nv in nhanViens)
                {
                    worksheet.Cell(row, 1).Value = stt++;
                    worksheet.Cell(row, 2).Value = nv.MaNV;
                    worksheet.Cell(row, 3).Value = nv.HoTen;

                    // FIX: Đã gỡ biến BienSoXe (nếu Model không có), chỉ lấy ChucVu
                    worksheet.Cell(row, 4).Value = nv.ChucVu;

                    // XỬ LÝ CHÈN ẢNH FACE ID CHUẨN XÁC
                    worksheet.Row(row).Height = 55;

                    if (!string.IsNullOrEmpty(nv.FaceDataPath))
                    {
                        try
                        {
                            string relativePath = nv.FaceDataPath.StartsWith("/") ? nv.FaceDataPath.Substring(1) : nv.FaceDataPath;
                            string imgPath = Path.Combine(webRootPath, relativePath.Replace("/", "\\"));

                            if (System.IO.File.Exists(imgPath))
                            {
                                worksheet.Cell(row, 5).Value = "";

                                // FIX: Đã gỡ 'new Point' để chống lỗi CS0246, căn ảnh trực tiếp vào ô
                                var picture = worksheet.AddPicture(imgPath)
                                    .MoveTo(worksheet.Cell(row, 5))
                                    .WithSize(60, 60);
                            }
                            else
                            {
                                worksheet.Cell(row, 5).Value = "Đã Nạp";
                                worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.SeaGreen;
                                worksheet.Cell(row, 5).Style.Font.Bold = true;
                            }
                        }
                        catch
                        {
                            worksheet.Cell(row, 5).Value = "Lỗi File";
                            worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
                        }
                    }
                    else
                    {
                        worksheet.Cell(row, 5).Value = "Chưa Nạp";
                        worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.DarkOrange;
                    }

                    // FIX: Đã gỡ nv.FingerprintData do Model không có, gắn text tĩnh
                    worksheet.Cell(row, 6).Value = "Chưa nạp";
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.DarkOrange;

                    // Căn giữa toàn bộ dòng
                    var rowRange = worksheet.Range($"A{row}:F{row}");
                    rowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    row++;
                }

                // Kẻ khung
                if (nhanViens.Any())
                {
                    var dataRange = worksheet.Range($"A{headerRowIdx + 1}:F{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                worksheet.Columns(1, 4).AdjustToContents();
                worksheet.Column(5).Width = 14;
                worksheet.Column(6).Width = 18;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string fileName = $"DuLieuSinhTracHoc_{DateTime.Now:ddMMyyyy}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
