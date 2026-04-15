using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
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

            if (userRole != "Admin" && userRole != "BaoVe")
            {
                if (userRole == "GiamDoc")
                    query = query.Where(x => x.BoPhanCanGap == "BOD" || x.BoPhanCanGap == "Ban Giám Đốc" || x.NhanVienCanGap == currentName);
                else if (userRole == "NhanSu")
                    query = query.Where(x => x.BoPhanCanGap == "HR" || x.BoPhanCanGap == "Phòng Nhân Sự" || x.NhanVienCanGap == currentName);
                else if (userRole == "KeToan")
                    query = query.Where(x => x.BoPhanCanGap == "ACCOUNTING" || x.BoPhanCanGap == "Phòng Kế Toán" || x.NhanVienCanGap == currentName);
                else
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

            bool coQuyenDuyet = false;

            if (userRole == "Admin" || userRole == "BaoVe")
                coQuyenDuyet = true;
            else if (khach.NhanVienCanGap == currentName)
                coQuyenDuyet = true;
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

        // =======================================================
        // TÍNH NĂNG XUẤT EXCEL CHUYÊN NGHIỆP CÓ ĐỊNH DẠNG & CHÈN ẢNH FACE ID
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string searchName, DateTime? searchDate)
        {
            var query = _context.DangKyKhachs.AsQueryable();
            string filterText = "Tất cả thời gian";

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(k => k.HoTen.Contains(searchName));
            }
            if (searchDate.HasValue)
            {
                query = query.Where(k => k.ThoiGianHen.Date == searchDate.Value.Date);
                filterText = $"Ngày: {searchDate.Value:dd/MM/yyyy}";
            }

            var khachs = await query.OrderByDescending(k => k.ThoiGianHen).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Hồ Sơ Khách Hàng");

                // -------------------------------------------------------------
                // 1. THIẾT KẾ PHẦN ĐẦU BÁO CÁO (HEADER REPORT)
                // -------------------------------------------------------------
                var titleCompany = worksheet.Range("A1:J1"); // Kéo dài đến cột J
                titleCompany.Merge().Value = "HỆ THỐNG KIỂM SOÁT AN NINH CHK-IN PRO";
                titleCompany.Style.Font.Bold = true;
                titleCompany.Style.Font.FontSize = 11;
                titleCompany.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                var titleReport = worksheet.Range("A3:J3");
                titleReport.Merge().Value = "BÁO CÁO DANH SÁCH KHÁCH HẸN VÀO RA";
                titleReport.Style.Font.Bold = true;
                titleReport.Style.Font.FontSize = 16;
                titleReport.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var titleFilter = worksheet.Range("A4:J4");
                titleFilter.Merge().Value = $"Kỳ báo cáo: {filterText}  |  Ngày trích xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                titleFilter.Style.Font.Italic = true;
                titleFilter.Style.Font.FontSize = 10;
                titleFilter.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // -------------------------------------------------------------
                // 2. THIẾT KẾ CỘT TIÊU ĐỀ DỮ LIỆU
                // -------------------------------------------------------------
                int headerRowIdx = 6;
                worksheet.Cell(headerRowIdx, 1).Value = "STT";
                worksheet.Cell(headerRowIdx, 2).Value = "HỌ VÀ TÊN KHÁCH";
                worksheet.Cell(headerRowIdx, 3).Value = "SỐ LƯỢNG";
                worksheet.Cell(headerRowIdx, 4).Value = "THỜI GIAN HẸN";
                worksheet.Cell(headerRowIdx, 5).Value = "PHÒNG BAN GẶP";
                worksheet.Cell(headerRowIdx, 6).Value = "NGƯỜI CẦN GẶP";
                worksheet.Cell(headerRowIdx, 7).Value = "LÝ DO CHI TIẾT";
                worksheet.Cell(headerRowIdx, 8).Value = "PHƯƠNG TIỆN / BIỂN SỐ";
                worksheet.Cell(headerRowIdx, 9).Value = "TRẠNG THÁI";
                worksheet.Cell(headerRowIdx, 10).Value = "ẢNH FACE ID"; // THÊM CỘT ẢNH

                var headerRange = worksheet.Range($"A{headerRowIdx}:J{headerRowIdx}"); // Tới cột J
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.Black;
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

                // Lấy đường dẫn gốc của thư mục wwwroot để tìm file ảnh
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                foreach (var k in khachs)
                {
                    worksheet.Cell(row, 1).Value = stt++;
                    worksheet.Cell(row, 2).Value = k.HoTen;
                    worksheet.Cell(row, 3).Value = k.SoLuongNguoi;
                    worksheet.Cell(row, 4).Value = k.ThoiGianHen.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 5).Value = k.BoPhanCanGap;
                    worksheet.Cell(row, 6).Value = k.NhanVienCanGap ?? "---";
                    worksheet.Cell(row, 7).Value = k.LyDo;

                    string phuongTien = !string.IsNullOrEmpty(k.BienSoXe) ? $"{k.LoaiXe} - {k.BienSoXe}" : "Không đi xe";
                    worksheet.Cell(row, 8).Value = phuongTien;

                    worksheet.Cell(row, 9).Value = k.TrangThaiDuyet;

                    // XỬ LÝ CHÈN ẢNH FACE ID VÀO CỘT 10
                    if (!string.IsNullOrEmpty(k.FaceDataPath))
                    {
                        try
                        {
                            // Chuyển đổi đường dẫn web (/uploads/...) thành đường dẫn vật lý (C:\...\wwwroot\uploads\...)
                            string relativePath = k.FaceDataPath.StartsWith("/") ? k.FaceDataPath.Substring(1) : k.FaceDataPath;
                            string imgPath = Path.Combine(webRootPath, relativePath.Replace("/", "\\"));

                            if (System.IO.File.Exists(imgPath))
                            {
                                // Chèn ảnh vào ô và chỉnh kích thước
                                var picture = worksheet.AddPicture(imgPath).MoveTo(worksheet.Cell(row, 10));
                                picture.Width = 45;
                                picture.Height = 45;

                                worksheet.Row(row).Height = 35; // Nới rộng chiều cao hàng để chứa vừa ảnh
                                worksheet.Cell(row, 10).Value = ""; // Để trống chữ
                            }
                            else
                            {
                                worksheet.Cell(row, 10).Value = "(Có link ảnh)";
                            }
                        }
                        catch
                        {
                            worksheet.Cell(row, 10).Value = "(Dữ liệu ảnh)";
                        }
                    }
                    else
                    {
                        worksheet.Cell(row, 10).Value = "Không có ảnh";
                        worksheet.Cell(row, 10).Style.Font.Italic = true;
                        worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Gray;
                    }

                    // Căn giữa cho các cột dữ liệu
                    worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Đổi màu chữ trạng thái 
                    if (k.TrangThaiDuyet == "Đã duyệt") worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.SeaGreen;
                    else if (k.TrangThaiDuyet == "Từ chối") worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.Red;
                    else worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.DarkOrange;

                    row++;
                }

                // Kẻ toàn bộ khung viền (Border) tới cột J
                if (khachs.Any())
                {
                    var dataRange = worksheet.Range($"A{headerRowIdx + 1}:J{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Tự động giãn cột 1 đến 9 cho vừa chữ. (Riêng cột 10 để cố định cho ảnh đỡ bị méo)
                worksheet.Columns(1, 9).AdjustToContents();
                worksheet.Column(10).Width = 12;

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string fileName = searchDate.HasValue ? $"BaoCao_KhachHen_{searchDate.Value:ddMMyyyy}.xlsx" : "BaoCao_HoSoKhach_ToanBo.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}