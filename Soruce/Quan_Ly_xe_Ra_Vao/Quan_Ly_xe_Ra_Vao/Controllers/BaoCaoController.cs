using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using Quan_Ly_xe_Ra_Vao.Data;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize]
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- TRANG HIỂN THỊ ---
        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            DateTime filterDate = selectedDate ?? DateTime.Today;
            DateTime nextDate = filterDate.AddDays(1);

            ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");

            // Tính toán số liệu (Dùng chung logic cho cả Index và Export)
            await CalculateReportStats(filterDate);

            var chiTiet = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            return View(chiTiet);
        }

        // --- CHỨC NĂNG XUẤT EXCEL CAO CẤP (BỐ CỤC CHUẨN MỰC, KHÔNG BỊ TRẮNG BIỂU ĐỒ) ---
        public async Task<IActionResult> ExportExcel(DateTime? selectedDate)
        {
            ExcelPackage.License.SetNonCommercialPersonal("CHK-IN PRO Admin");

            DateTime filterDate = selectedDate ?? DateTime.Today;
            DateTime nextDate = filterDate.AddDays(1);

            // 1. LẤY DỮ LIỆU
            var logs = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            int tongVaoRa = logs.Count;
            int khachNgoai = await _context.DangKyKhachs.CountAsync(x => x.ThoiGianHen.Date == filterDate.Date && x.TrangThaiDuyet == "Đã duyệt");
            int canhBao = logs.Count(x => x.TrangThai != "Thành Công" && x.TrangThai != "Hợp lệ");

            var (dungGio, diTre, chuaCheckIn) = await GetAttendanceStats(filterDate);

            // Dữ liệu 7 ngày qua
            var bayNgayQua = Enumerable.Range(0, 7).Select(i => filterDate.AddDays(-i)).Reverse().ToList();
            var data7Ngay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= bayNgayQua.First() && x.ThoiGian < nextDate)
                .Select(x => x.ThoiGian)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                // TẠO 2 SHEET: 1 Sheet hiển thị, 1 Sheet ẩn chứa data biểu đồ để chống lỗi trắng bóc
                var ws = package.Workbook.Worksheets.Add("BaoCaoSystem");
                var wsData = package.Workbook.Worksheets.Add("DataNgam");
                wsData.Hidden = eWorkSheetHidden.Hidden;

                // ==========================================
                // PHẦN DATA NGẦM (TRUYỀN SỐ LIỆU CHO BIỂU ĐỒ)
                // ==========================================
                wsData.Cells["A1"].Value = "Mục"; wsData.Cells["B1"].Value = "Số lượng";
                wsData.Cells["A2"].Value = "Đúng giờ"; wsData.Cells["B2"].Value = dungGio;
                wsData.Cells["A3"].Value = "Đi trễ"; wsData.Cells["B3"].Value = diTre;
                wsData.Cells["A4"].Value = "Chưa Check-in";
                // Nếu không có ai, ép biến Chưa Check-in = 1 để nó vẽ vòng tròn xám
                wsData.Cells["B4"].Value = (dungGio == 0 && diTre == 0 && chuaCheckIn == 0) ? 1 : chuaCheckIn;

                wsData.Cells["C1"].Value = "Ngày"; wsData.Cells["D1"].Value = "Lượt";
                for (int i = 0; i < 7; i++)
                {
                    wsData.Cells[i + 2, 3].Value = bayNgayQua[i].ToString("dd/MM");
                    wsData.Cells[i + 2, 4].Value = data7Ngay.Count(t => t.Date == bayNgayQua[i].Date);
                }

                // ==========================================
                // PHẦN 1: HEADER & TIÊU ĐỀ (Canh lề cực chuẩn)
                // ==========================================
                ws.Cells["A1:H1"].Merge = true;
                ws.Cells["A1"].Value = "HỆ THỐNG KIỂM SOÁT AN NINH CHK-IN PRO";
                ws.Cells["A1"].Style.Font.Bold = true;

                ws.Cells["C3:H3"].Merge = true;
                ws.Cells["C3"].Value = "BÁO CÁO TỔNG QUAN HỆ THỐNG";
                ws.Cells["C3"].Style.Font.Size = 14;
                ws.Cells["C3"].Style.Font.Bold = true;
                ws.Cells["C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["C4:H4"].Merge = true;
                ws.Cells["C4"].Value = $"Kỳ báo cáo: {filterDate:dd/MM/yyyy} | Ngày trích xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Cells["C4"].Style.Font.Italic = true;
                ws.Cells["C4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // ==========================================
                // PHẦN 2: THỐNG KÊ SỐ LIỆU (Đóng khung từng ô y hệt mẫu)
                // ==========================================
                ws.Cells["B6"].Value = "I. THỐNG KÊ TRONG NGÀY";
                ws.Cells["B6"].Style.Font.Bold = true;

                // Bảng 1: An Ninh
                ws.Cells["B7"].Value = "Tổng lượt xe ra vào:"; ws.Cells["C7"].Value = tongVaoRa;
                ws.Cells["B8"].Value = "Khách ngoài đăng ký:"; ws.Cells["C8"].Value = khachNgoai;
                ws.Cells["B9"].Value = "Cảnh báo an ninh:"; ws.Cells["C9"].Value = canhBao;

                ws.Cells["B7:B9"].Style.Font.Bold = true;
                ws.Cells["C9"].Style.Font.Color.SetColor(Color.Red);
                ws.Cells["C9"].Style.Font.Bold = true;

                // Kẻ khung toàn bộ B7:C9
                var box1 = ws.Cells["B7:C9"];
                box1.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                box1.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                box1.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                box1.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                // Bảng 2: Chấm Công
                ws.Cells["E7"].Value = "Nhân viên đúng giờ:"; ws.Cells["F7"].Value = dungGio;
                ws.Cells["E8"].Value = "Nhân viên đi trễ:"; ws.Cells["F8"].Value = diTre;
                ws.Cells["E9"].Value = "Chưa Check-in:"; ws.Cells["F9"].Value = chuaCheckIn;

                ws.Cells["E7:E9"].Style.Font.Bold = true;

                // Kẻ khung toàn bộ E7:F9
                var box2 = ws.Cells["E7:F9"];
                box2.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                box2.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                box2.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                box2.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                // ==========================================
                // PHẦN 3: VẼ 2 BIỂU ĐỒ ĐỘC LẬP TẠI DÒNG 12
                // ==========================================
                ws.Cells["B11"].Value = "II. BIỂU ĐỒ TRỰC QUAN";
                ws.Cells["B11"].Style.Font.Bold = true;

                // Biểu đồ Đường (Lấy từ Sheet Ngầm)
                var lineChart = ws.Drawings.AddChart("LineChart", eChartType.LineMarkers);
                lineChart.Title.Text = "LƯU LƯỢNG 7 NGÀY QUA";
                lineChart.SetPosition(11, 0, 1, 0); // Bắt đầu ở Cột B
                lineChart.SetSize(450, 260);
                lineChart.Series.Add(wsData.Cells["D2:D8"], wsData.Cells["C2:C8"]);
                lineChart.Legend.Remove();

                // Biểu đồ Tròn (Lấy từ Sheet Ngầm)
                var pieChart = ws.Drawings.AddChart("AttendanceChart", eChartType.Pie);
                pieChart.Title.Text = "TỶ LỆ CHẤM CÔNG";
                pieChart.SetPosition(11, 0, 7, 0); // Dời xa sang Cột H, tách biệt hoàn toàn
                pieChart.SetSize(350, 260);
                var pieSeries = (ExcelPieChartSerie)pieChart.Series.Add(wsData.Cells["B2:B4"], wsData.Cells["A2:A4"]);
                pieSeries.DataLabel.ShowPercent = true;

                // ==========================================
                // PHẦN 4: BẢNG CHI TIẾT RA VÀO
                // ==========================================
                int startRow = 28; // Căn y hệt dòng 27-28 trong ảnh của bạn
                ws.Cells[$"A{startRow - 1}"].Value = "III. CHI TIẾT LỊCH SỬ RA VÀO";
                ws.Cells[$"A{startRow - 1}"].Style.Font.Bold = true;

                string[] headers = { "STT", "THỜI GIAN", "HỌ TÊN", "ĐỐI TƯỢNG", "PHƯƠNG THỨC / BIỂN SỐ", "HƯỚNG", "TRẠNG THÁI" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cells[startRow, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(226, 239, 218));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int rowIdx = startRow + 1;
                int stt = 1;
                foreach (var log in logs)
                {
                    ws.Cells[rowIdx, 1].Value = stt++;
                    ws.Cells[rowIdx, 2].Value = log.ThoiGian.ToString("HH:mm:ss");
                    ws.Cells[rowIdx, 3].Value = log.HoTen;
                    ws.Cells[rowIdx, 4].Value = log.LoaiDoiTuong;
                    ws.Cells[rowIdx, 5].Value = string.IsNullOrEmpty(log.BienSoXe) || log.BienSoXe == "---" ? log.PhuongThuc : $"{log.PhuongThuc} ({log.BienSoXe})";
                    ws.Cells[rowIdx, 6].Value = log.Huong;
                    ws.Cells[rowIdx, 7].Value = log.TrangThai;

                    if (log.TrangThai != "Thành Công" && log.TrangThai != "Hợp lệ")
                        ws.Cells[rowIdx, 7].Style.Font.Color.SetColor(Color.Red);

                    for (int c = 1; c <= 7; c++) ws.Cells[rowIdx, c].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    rowIdx++;
                }

                // Căn chỉnh độ rộng cột chuẩn mực
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                ws.Column(1).Width = 6;  // STT
                ws.Column(2).Width = 24; // Cột Thời gian / Bảng 1
                ws.Column(5).Width = 28; // Phương thức
                ws.Column(6).Width = 15; // Cột Hướng / Bảng 2
                ws.Column(7).Width = 15;

                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_AnNinh_{filterDate:ddMMyyyy}.xlsx");
            }
        }

        // --- HÀM HỖ TRỢ TÍNH TOÁN ---
        private async Task CalculateReportStats(DateTime filterDate)
        {
            DateTime nextDate = filterDate.AddDays(1);
            ViewBag.TongVaoRa = await _context.LichSuCheckIns.CountAsync(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate);
            ViewBag.KhachNgoai = await _context.DangKyKhachs.CountAsync(x => x.ThoiGianHen.Date == filterDate.Date && x.TrangThaiDuyet == "Đã duyệt");
            ViewBag.CanhBao = await _context.LichSuCheckIns.CountAsync(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate && x.TrangThai != "Thành Công");

            var (dungGio, diTre, chuaCheckIn) = await GetAttendanceStats(filterDate);
            ViewBag.DungGio = dungGio;
            ViewBag.DiTre = diTre;
            ViewBag.ChuaCheckIn = chuaCheckIn;

            // Biểu đồ 7 ngày
            var bayNgayQua = Enumerable.Range(0, 7).Select(i => filterDate.AddDays(-i)).Reverse().ToList();
            var data7Ngay = await _context.LichSuCheckIns.Where(x => x.ThoiGian >= bayNgayQua.First() && x.ThoiGian < nextDate).Select(x => x.ThoiGian).ToListAsync();
            ViewBag.LabelsCot = string.Join(",", bayNgayQua.Select(d => $"'{d:dd/MM}'"));
            ViewBag.DataCot = string.Join(",", bayNgayQua.Select(ngay => data7Ngay.Count(t => t.Date == ngay.Date)));
        }

        private async Task<(int dungGio, int diTre, int chuaCheckIn)> GetAttendanceStats(DateTime filterDate)
        {
            DateTime nextDate = filterDate.AddDays(1);
            int dungGio = 0; int diTre = 0;
            var checkInTrongNgay = await _context.LichSuCheckIns
                .Where(x => x.ThoiGian >= filterDate && x.ThoiGian < nextDate && x.Huong == "Đi Vào" && x.TrangThai == "Thành Công")
                .Select(x => new { x.HoTen, x.ThoiGian }).ToListAsync();

            var danhSachNV = await _context.NhanViens.Where(n => !n.ChucVu.Contains("Khách")).Select(n => new { n.HoTen, n.GioVaoCa, n.GioXinDiMuon }).ToListAsync();

            foreach (var nv in danhSachNV)
            {
                var logVao = checkInTrongNgay.OrderBy(x => x.ThoiGian).FirstOrDefault(x => x.HoTen == nv.HoTen);
                if (logVao != null)
                {
                    if (logVao.ThoiGian.TimeOfDay > (nv.GioXinDiMuon ?? nv.GioVaoCa)) diTre++;
                    else dungGio++;
                }
            }
            int chuaCheckIn = Math.Max(0, danhSachNV.Count - (dungGio + diTre));
            return (dungGio, diTre, chuaCheckIn);
        }
    }
}