using System;
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
    public class CameraLogPayload
    {
        public string? HoTen { get; set; }
        public string? LoaiDoiTuong { get; set; }
        public string? BienSoXe { get; set; }
        public string? PhuongThuc { get; set; }
        public string? Huong { get; set; }
        public string? TrangThai { get; set; }
        public string? HinhAnh { get; set; }
        public string? ViTriDo { get; set; }
        public string? LoaiXe { get; set; }
    }

    [Authorize]
    public class LichSuCheckInController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LichSuCheckInController(ApplicationDbContext context) { _context = context; }

        // =======================================================
        // 1. TRANG LỊCH SỬ TỔNG QUÁT
        // =======================================================
        public async Task<IActionResult> Index(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName) || x.BienSoXe.Contains(searchName));
            }

            ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.SearchName = searchName;

            return View(await query.OrderByDescending(x => x.ThoiGian).ToListAsync());
        }

        // =======================================================
        // 2. TRANG SỔ TRỰC BAN
        // =======================================================
        [Authorize(Roles = "Admin,BaoVe")]
        public async Task<IActionResult> TrucBan(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName));
            }

            ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.SearchName = searchName;

            return View(await query.OrderByDescending(x => x.ThoiGian).ToListAsync());
        }

        // =======================================================
        // 3. API GHI NHẬT KÝ TỪ TRẠM GIÁM SÁT AI
        // =======================================================
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> GhiNhatKy([FromBody] CameraLogPayload data)
        {
            try
            {
                if (data == null || string.IsNullOrEmpty(data.HoTen))
                    return BadRequest("Dữ liệu không đầy đủ");

                // --- LOGIC 1: XOAY VÒNG HƯỚNG RA/VÀO ---
                string huongTuDong = "Đi Vào";
                var lastRecord = await _context.LichSuCheckIns
                    .Where(x => x.HoTen == data.HoTen && x.ThoiGian.Date == DateTime.Today)
                    .OrderByDescending(x => x.ThoiGian)
                    .FirstOrDefaultAsync();

                if (lastRecord != null && lastRecord.Huong == "Đi Vào")
                {
                    huongTuDong = "Đi Ra";
                }

                // --- LOGIC 2: PHÂN LOẠI PHƯƠNG TIỆN & FIX BIỂN SỐ ---
                bool isXeMay = (data.BienSoXe == "undefined" || string.IsNullOrEmpty(data.BienSoXe) || data.LoaiXe == "XeMay");
                string finalLoaiXe = isXeMay ? "Xe máy" : "Ô tô";
                string finalBienSo = isXeMay ? "---" : data.BienSoXe!;

                // --- LOGIC 3: XỬ LÝ VỊ TRÍ ĐỖ ---
                string? viTriThucTe = (finalLoaiXe == "Ô tô" && huongTuDong == "Đi Vào") ? data.ViTriDo : null;

                // --- LOGIC 4: TẠO BẢN GHI ---
                var record = new LichSuCheckIn
                {
                    HoTen = data.HoTen,
                    LoaiDoiTuong = "Nhân viên",
                    LoaiXe = finalLoaiXe,
                    BienSoXe = finalBienSo,
                    PhuongThuc = "AI FaceID + ALPR",
                    Huong = huongTuDong,
                    TrangThai = "Thành Công",
                    HinhAnh = data.HinhAnh,
                    ViTriDo = viTriThucTe,
                    ThoiGian = DateTime.Now
                };

                _context.LichSuCheckIns.Add(record);
                await _context.SaveChangesAsync();

                return Json(new { success = true, huong = huongTuDong, loaiXe = finalLoaiXe });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi Server: {ex.Message}");
            }
        }

        // =======================================================
        // 4. XUẤT EXCEL SỔ TRỰC BAN CHUYÊN NGHIỆP (KHÔNG KÈM ẢNH)
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> ExportTrucBanExcel(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            string filterText = $"Ngày trực: {targetDate:dd/MM/yyyy}";

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName));
                filterText += $" | Lọc theo tên: {searchName}";
            }

            var logs = await query.OrderByDescending(x => x.ThoiGian).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sổ Trực Ban");

                var titleCompany = worksheet.Range("A1:G1");
                titleCompany.Merge().Value = "HỆ THỐNG KIỂM SOÁT AN NINH CHK-IN PRO";
                titleCompany.Style.Font.Bold = true;
                titleCompany.Style.Font.FontSize = 11;
                titleCompany.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                var titleReport = worksheet.Range("A3:G3");
                titleReport.Merge().Value = "BÁO CÁO SỔ TRỰC BAN & LỊCH SỬ RA VÀO";
                titleReport.Style.Font.Bold = true;
                titleReport.Style.Font.FontSize = 16;
                titleReport.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var titleFilter = worksheet.Range("A4:G4");
                titleFilter.Merge().Value = $"{filterText}  |  Ngày trích xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                titleFilter.Style.Font.Italic = true;
                titleFilter.Style.Font.FontSize = 10;
                titleFilter.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int headerRowIdx = 6;
                worksheet.Cell(headerRowIdx, 1).Value = "STT";
                worksheet.Cell(headerRowIdx, 2).Value = "THÔNG TIN NHÂN VIÊN";
                worksheet.Cell(headerRowIdx, 3).Value = "LOẠI XE";
                worksheet.Cell(headerRowIdx, 4).Value = "BIỂN SỐ XE";
                worksheet.Cell(headerRowIdx, 5).Value = "THỜI GIAN QUÉT";
                worksheet.Cell(headerRowIdx, 6).Value = "HƯỚNG RA/VÀO";
                worksheet.Cell(headerRowIdx, 7).Value = "TRẠNG THÁI";

                var headerRange = worksheet.Range($"A{headerRowIdx}:G{headerRowIdx}");
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

                foreach (var item in logs)
                {
                    worksheet.Cell(row, 1).Value = stt++;

                    string hoTen = string.IsNullOrEmpty(item.HoTen) ? "Khách vãng lai" : item.HoTen;
                    string loaiDoiTuong = string.IsNullOrEmpty(item.LoaiDoiTuong) ? "NHÂN VIÊN" : item.LoaiDoiTuong;
                    worksheet.Cell(row, 2).Value = $"{hoTen} ({loaiDoiTuong})";

                    worksheet.Cell(row, 3).Value = string.IsNullOrEmpty(item.LoaiXe) ? "---" : item.LoaiXe;
                    worksheet.Cell(row, 4).Value = (string.IsNullOrEmpty(item.BienSoXe) || item.BienSoXe == "---") ? "N/A" : item.BienSoXe;
                    worksheet.Cell(row, 5).Value = item.ThoiGian.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cell(row, 6).Value = item.Huong;
                    worksheet.Cell(row, 7).Value = item.TrangThai;

                    worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (item.Huong == "Đi Vào") worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.SeaGreen;
                    else worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.DarkOrange;

                    if (item.TrangThai == "Thành Công" || item.TrangThai == "Hợp lệ") worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.SeaGreen;
                    else worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.Red;

                    row++;
                }

                if (logs.Any())
                {
                    var dataRange = worksheet.Range($"A{headerRowIdx + 1}:G{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                worksheet.Columns(1, 7).AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SoTrucBan_{targetDate:ddMMyyyy}.xlsx");
                }
            }
        }

        // =======================================================
        // 5. XUẤT EXCEL LỊCH SỬ RA VÀO (Dành cho trang Index)
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? selectedDate, string searchName)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var query = _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate.Date);

            string filterText = $"Ngày truy xuất: {targetDate:dd/MM/yyyy}";

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(x => x.HoTen.Contains(searchName) || x.BienSoXe.Contains(searchName));
                filterText += $" | Lọc theo: {searchName}";
            }

            var logs = await query.OrderByDescending(x => x.ThoiGian).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Lịch Sử Ra Vào");

                // 1. THIẾT KẾ HEADER BÁO CÁO (8 Cột)
                var titleCompany = worksheet.Range("A1:H1");
                titleCompany.Merge().Value = "HỆ THỐNG KIỂM SOÁT AN NINH CHK-IN PRO";
                titleCompany.Style.Font.Bold = true;
                titleCompany.Style.Font.FontSize = 11;
                titleCompany.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                var titleReport = worksheet.Range("A3:H3");
                titleReport.Merge().Value = "BÁO CÁO NHẬT KÝ LỊCH SỬ RA VÀO";
                titleReport.Style.Font.Bold = true;
                titleReport.Style.Font.FontSize = 16;
                titleReport.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var titleFilter = worksheet.Range("A4:H4");
                titleFilter.Merge().Value = $"{filterText}  |  Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
                titleFilter.Style.Font.Italic = true;
                titleFilter.Style.Font.FontSize = 10;
                titleFilter.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 2. THIẾT KẾ CỘT TIÊU ĐỀ
                int headerRowIdx = 6;
                worksheet.Cell(headerRowIdx, 1).Value = "STT";
                worksheet.Cell(headerRowIdx, 2).Value = "THỜI GIAN";
                worksheet.Cell(headerRowIdx, 3).Value = "ĐỐI TƯỢNG KIỂM SOÁT";
                worksheet.Cell(headerRowIdx, 4).Value = "LOẠI XE";
                worksheet.Cell(headerRowIdx, 5).Value = "BIỂN SỐ XE";
                worksheet.Cell(headerRowIdx, 6).Value = "PHƯƠNG THỨC";
                worksheet.Cell(headerRowIdx, 7).Value = "HƯỚNG";
                worksheet.Cell(headerRowIdx, 8).Value = "TRẠNG THÁI";

                var headerRange = worksheet.Range($"A{headerRowIdx}:H{headerRowIdx}");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.Black;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(198, 239, 206); // Nền xanh lá chuyên nghiệp
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Row(headerRowIdx).Height = 25;

                // 3. ĐỔ DỮ LIỆU
                int row = headerRowIdx + 1;
                int stt = 1;

                if (logs.Any())
                {
                    foreach (var item in logs)
                    {
                        worksheet.Cell(row, 1).Value = stt++;
                        worksheet.Cell(row, 2).Value = item.ThoiGian.ToString("dd/MM/yyyy HH:mm:ss");

                        string hoTen = string.IsNullOrEmpty(item.HoTen) ? "Khách vãng lai" : item.HoTen;
                        string loaiDoiTuong = string.IsNullOrEmpty(item.LoaiDoiTuong) ? "NHÂN VIÊN" : item.LoaiDoiTuong;
                        worksheet.Cell(row, 3).Value = $"{hoTen} ({loaiDoiTuong})";

                        worksheet.Cell(row, 4).Value = string.IsNullOrEmpty(item.LoaiXe) ? "---" : item.LoaiXe;
                        worksheet.Cell(row, 5).Value = (string.IsNullOrEmpty(item.BienSoXe) || item.BienSoXe == "---") ? "N/A" : item.BienSoXe;
                        worksheet.Cell(row, 6).Value = string.IsNullOrEmpty(item.PhuongThuc) ? "Hệ thống tự động" : item.PhuongThuc;
                        worksheet.Cell(row, 7).Value = item.Huong;
                        worksheet.Cell(row, 8).Value = item.TrangThai;

                        // Căn giữa
                        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Tô màu
                        if (item.Huong == "Đi Vào") worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.SeaGreen;
                        else worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.DarkOrange;

                        if (item.TrangThai == "Thành Công" || item.TrangThai == "Hợp lệ") worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.SeaGreen;
                        else worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.Red;

                        row++;
                    }

                    // Kẻ khung
                    var dataRange = worksheet.Range($"A{headerRowIdx + 1}:H{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    // Xử lý báo cáo rỗng gọn gàng
                    worksheet.Range($"A{row}:H{row}").Merge().Value = "Không có dữ liệu lịch sử ra vào trong ngày này.";
                    worksheet.Range($"A{row}:H{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range($"A{row}:H{row}").Style.Font.Italic = true;
                    worksheet.Range($"A{row}:H{row}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                worksheet.Columns(1, 8).AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string fileName = $"NhatKy_RaVao_{targetDate:ddMMyyyy}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}