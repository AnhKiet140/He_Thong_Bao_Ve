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

            // ĐÃ FIX: Bỏ điều kiện (x.ThoiGianHen.Date >= homNay) để hệ thống 
            // hiển thị toàn bộ đơn chờ duyệt bị tồn đọng từ các ngày trước (trong quá khứ)
            var query = _context.DangKyKhachs.Where(x =>
                x.TrangThaiDuyet == "Chờ duyệt" || x.TrangThaiDuyet == "Đã duyệt");

            if (userRole != "Admin" && userRole != "BaoVe")
            {
                if (userRole == "GiamDoc")
                {
                    query = query.Where(x => x.BoPhanCanGap == "Ban Giám Đốc" || x.BoPhanCanGap == "BOD" || x.NhanVienCanGap == currentName);
                }
                else if (userRole == "NhanSu")
                {
                    query = query.Where(x => x.BoPhanCanGap == "Phòng Nhân Sự" || x.BoPhanCanGap == "HR" || x.NhanVienCanGap == currentName);
                }
                else if (userRole == "KeToan")
                {
                    query = query.Where(x => x.BoPhanCanGap == "Phòng Kế Toán" || x.BoPhanCanGap == "ACC" || x.NhanVienCanGap == currentName);
                }
                else
                {
                    query = query.Where(x => x.NhanVienCanGap == currentName);
                }
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

            // Kiểm tra phân quyền duyệt dựa trên Role của người đang đăng nhập
            // VÀ giá trị Bộ Phận mà khách đã đăng ký
            if (khach.NhanVienCanGap == currentName)
                coQuyenDuyet = true;
            else if (userRole == "GiamDoc" && (khach.BoPhanCanGap == "Ban Giám Đốc" || khach.BoPhanCanGap == "BOD"))
                coQuyenDuyet = true;
            else if (userRole == "NhanSu" && (khach.BoPhanCanGap == "Phòng Nhân Sự" || khach.BoPhanCanGap == "HR"))
                coQuyenDuyet = true;
            else if (userRole == "KeToan" && (khach.BoPhanCanGap == "Phòng Kế Toán" || khach.BoPhanCanGap == "ACC"))
                coQuyenDuyet = true;
            else if (userRole == "BaoVe")
                coQuyenDuyet = true;

            // Admin chỉ được xem, KHÔNG được bấm duyệt thay phòng ban
            if (userRole == "Admin" && khach.NhanVienCanGap != currentName)
            {
                coQuyenDuyet = false;
            }

            if (!coQuyenDuyet)
                return Json(new { success = false, message = "Quyền truy cập bị từ chối: Admin chỉ có chức năng giám sát, không được phê duyệt thay phòng ban!" });

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

                var nhatKy = new LichSuCheckIn
                {
                    HoTen = khach.HoTen,
                    ThoiGian = DateTime.Now,
                    BienSoXe = khach.BienSoXe,
                    HinhAnh = khach.FaceDataPath,
                    LoaiDoiTuong = "Khách Hẹn (" + khach.BoPhanCanGap + ")", // Lấy nguyên gốc tên phòng ban đã lưu
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
                    query = query.Where(x => x.BoPhanCanGap == "Ban Giám Đốc" || x.BoPhanCanGap == "BOD" || x.NhanVienCanGap == currentName);
                else if (userRole == "NhanSu")
                    query = query.Where(x => x.BoPhanCanGap == "Phòng Nhân Sự" || x.BoPhanCanGap == "HR" || x.NhanVienCanGap == currentName);
                else if (userRole == "KeToan")
                    query = query.Where(x => x.BoPhanCanGap == "Phòng Kế Toán" || x.BoPhanCanGap == "ACC" || x.NhanVienCanGap == currentName);
                else
                    query = query.Where(x => x.NhanVienCanGap == currentName);
            }

            // ĐÃ FIX: Cho phép hiển thị toàn bộ lịch sử nếu không chọn ngày cụ thể
            if (selectedDate.HasValue)
            {
                query = query.Where(k => k.ThoiGianHen.Date == selectedDate.Value.Date);
            }
            // (Đã xóa đoạn else chặn ngày trong quá khứ ở đây)

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

                var titleCompany = worksheet.Range("A1:J1");
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
                worksheet.Cell(headerRowIdx, 10).Value = "ẢNH FACE ID";

                var headerRange = worksheet.Range($"A{headerRowIdx}:J{headerRowIdx}");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.Black;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(198, 239, 206);
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Row(headerRowIdx).Height = 25;

                int row = headerRowIdx + 1;
                int stt = 1;
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

                    // ==========================================
                    // ĐÃ CẬP NHẬT: Định dạng Hàng & Cỡ ảnh như hình mẫu
                    // ==========================================
                    worksheet.Row(row).Height = 90; // Mở rộng hàng cực to để chứa ảnh chân dung

                    if (!string.IsNullOrEmpty(k.FaceDataPath))
                    {
                        try
                        {
                            worksheet.Cell(row, 10).Value = "";

                            // Tọa độ căn giữa ô (Cách trái 11px, Cách trên 5px), Kích thước ảnh 75x110
                            if (k.FaceDataPath.StartsWith("data:image"))
                            {
                                string base64Data = k.FaceDataPath.Substring(k.FaceDataPath.IndexOf(",") + 1);
                                byte[] imageBytes = Convert.FromBase64String(base64Data);
                                using (var ms = new MemoryStream(imageBytes))
                                {
                                    worksheet.AddPicture(ms).MoveTo(worksheet.Cell(row, 10), 11, 5).WithSize(75, 110);
                                }
                            }
                            else
                            {
                                string relativePath = k.FaceDataPath.StartsWith("/") ? k.FaceDataPath.Substring(1) : k.FaceDataPath;
                                string imgPath = Path.Combine(webRootPath, relativePath.Replace("/", "\\"));

                                if (System.IO.File.Exists(imgPath))
                                {
                                    worksheet.AddPicture(imgPath).MoveTo(worksheet.Cell(row, 10), 11, 5).WithSize(75, 110);
                                }
                                else
                                {
                                    worksheet.Cell(row, 10).Value = "(Có link ảnh)";
                                }
                            }
                        }
                        catch
                        {
                            worksheet.Cell(row, 10).Value = "(Lỗi ảnh)";
                            worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Red;
                        }
                    }
                    else
                    {
                        worksheet.Cell(row, 10).Value = "Không có ảnh";
                        worksheet.Cell(row, 10).Style.Font.Italic = true;
                        worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Gray;
                    }

                    var rowRange = worksheet.Range($"A{row}:J{row}");
                    rowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (k.TrangThaiDuyet == "Đã duyệt") worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.SeaGreen;
                    else if (k.TrangThaiDuyet == "Từ chối") worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.Red;
                    else worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.DarkOrange;

                    row++;
                }

                if (khachs.Any())
                {
                    var dataRange = worksheet.Range($"A{headerRowIdx + 1}:J{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                // Chỉnh cột cho vừa vặn
                worksheet.Columns(1, 9).AdjustToContents();
                worksheet.Column(10).Width = 14; // Cố định bề ngang cột ảnh

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