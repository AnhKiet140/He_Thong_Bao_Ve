using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_xe_Ra_Vao.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class SecurityAIController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SecurityAIController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AskAI([FromBody] AIRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Query)) return Json(new { answer = "Dạ, bạn chưa nhập câu hỏi. Bạn cần tôi giúp gì ạ?" });

                string query = request.Query.ToLower().Trim().Normalize(System.Text.NormalizationForm.FormC);

                // Bóc tách ngày tháng linh hoạt
                var today = DateTime.Today;
                DateTime targetDate = ExtractDateFromQuery(query, today);
                string dateLabel = GetDateLabel(targetDate, today);

                var exactWords = query.Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

                if (exactWords.Contains("hi") || exactWords.Contains("hello") || query.StartsWith("chào"))
                    return Json(new { answer = "Xin chào! 👋 Tôi là Security AI. Hệ thống quản lý đã sẵn sàng. Bạn cần tôi truy xuất dữ liệu gì?" });

                if (ContainsAny(query, "clear", "xóa", "reset", "làm mới")) return Json(new { answer = "CLEAR_COMMAND" });

                // =====================================================================
                // 1. NHẬT KÝ (AUDIT LOG) & HOẠT ĐỘNG NGUY HIỂM (TRUY VẤN DB THẬT)
                // =====================================================================
                if (ContainsAny(query, "nguy hiểm", "hoạt động nào nguy hiểm", "hoat động nguy hiểm"))
                {
                    var dangerLogs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && (x.MucDo == "NGUY HIỂM" || x.MucDo.Contains("Khẩn"))).ToListAsync();
                    if (!dangerLogs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ. Hệ thống không ghi nhận bất kỳ hoạt động nguy hiểm nào trong {dateLabel.ToLower()}." });

                    string html = "<ul class='list-group small mt-2 mb-2'>";
                    foreach (var d in dangerLogs) html += $"<li class='list-group-item'><b class='text-danger'>[CẢNH BÁO]</b> {d.ChiTietThaoTac} (Bởi: {d.NguoiThucHien} lúc {d.ThoiGian:HH:mm})</li>";
                    html += "</ul>";
                    return Json(new { answer = $"🚨 **CÓ HOẠT ĐỘNG NGUY HIỂM** trong {dateLabel.ToLower()}:<br/>{html}", isDanger = true });
                }

                if (ContainsAny(query, "chức năng đã dụng", "chức năng đã dùng", "chức năng đã sử dụng"))
                {
                    var funcs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate).Select(x => x.PhanHe).Distinct().ToListAsync();
                    if (!funcs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ chức năng nào được thao tác trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2 mb-2'>";
                    foreach (var f in funcs) html += $"<li class='list-group-item'>- Phân hệ: <b>{f}</b></li>";
                    html += "</ul>";
                    return Json(new { answer = $"Các phân hệ/chức năng đã được sử dụng {dateLabel.ToLower()}:<br/>{html}" });
                }

                if (ContainsAny(query, "những ai sử dụng", "có những ai sử dụng", "ai đã sử dụng"))
                {
                    var logs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate).Select(x => x.NguoiThucHien).Distinct().ToListAsync();
                    if (!logs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai sử dụng hệ thống trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Tài khoản đã đăng nhập/sử dụng hệ thống {dateLabel.ToLower()}: **" + string.Join(", ", logs) + "**." });
                }

                // =====================================================================
                // 2. CẢNH BÁO (S.O.S & THIẾT BỊ)
                // =====================================================================
                if (ContainsAny(query, "phòng nào đang bị quá nhiệt", "quá nhiệt", "nóng", "cháy", "nhiệt độ", "phòng nào đang nóng"))
                {
                    double nhietDoKhoVatTu = 38.5; // (Mock data cảm biến nhiệt độ thực tế)
                    if (nhietDoKhoVatTu >= 38.0) return Json(new { answer = $"🚨 **CÓ! CẢNH BÁO KHẨN CẤP:** Phòng **Kho Vật Tư (Khu B)** đang quá nhiệt: <b class='fs-5 text-danger'>{nhietDoKhoVatTu}°C</b>. Yêu cầu kiểm tra ngay!", isDanger = true });
                    return Json(new { answer = "Dạ KHÔNG CÓ phòng nào bị quá nhiệt." });
                }

                // Lệnh Khẩn Cấp (Truy xuất từ NhatKyHeThong)
                if (ContainsAny(query, "mở khóa cửa khẩn cấp", "khóa cửa"))
                {
                    var logs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && x.ChiTietThaoTac.ToLower().Contains("khóa")).ToListAsync();
                    if (!logs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai mở khóa cửa khẩn cấp trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Dạ CÓ. Lịch sử mở khóa cửa {dateLabel.ToLower()}:<br/>" + string.Join("<br/>", logs.Select(x => $"- {x.NguoiThucHien} lúc {x.ThoiGian:HH:mm}")) });
                }

                if (ContainsAny(query, "bật vòi chữa cháy", "vòi chữa cháy"))
                {
                    var logs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && x.ChiTietThaoTac.ToLower().Contains("vòi")).ToListAsync();
                    if (!logs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai bật vòi chữa cháy trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Dạ CÓ. Lịch sử bật vòi chữa cháy {dateLabel.ToLower()}:<br/>" + string.Join("<br/>", logs.Select(x => $"- {x.NguoiThucHien} lúc {x.ThoiGian:HH:mm}")) });
                }

                if (ContainsAny(query, "test còi", "còi báo động"))
                {
                    var logs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && x.ChiTietThaoTac.ToLower().Contains("còi")).ToListAsync();
                    if (!logs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai test còi báo động trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Dạ CÓ. Lịch sử test còi {dateLabel.ToLower()}:<br/>" + string.Join("<br/>", logs.Select(x => $"- {x.NguoiThucHien} lúc {x.ThoiGian:HH:mm}")) });
                }

                if (ContainsAny(query, "tổng đài 114", "kết đối tổng đài", "kết nối tổng đài"))
                {
                    var logs = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && x.ChiTietThaoTac.ToLower().Contains("tổng đài")).ToListAsync();
                    if (!logs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai kết nối tổng đài 114 trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Dạ CÓ. Lịch sử kết nối tổng đài {dateLabel.ToLower()}:<br/>" + string.Join("<br/>", logs.Select(x => $"- {x.NguoiThucHien} lúc {x.ThoiGian:HH:mm}")) });
                }

                // =====================================================================
                // 3. DANH BẠ KHÁCH HÀNG & PHÊ DUYỆT LỐI VÀO
                // =====================================================================
                if (ContainsAny(query, "chờ duyệt"))
                {
                    var choDuyet = await _context.DangKyKhachs.Where(k => k.ThoiGianHen.Date == targetDate && k.TrangThaiDuyet == "Chờ duyệt").ToListAsync();
                    if (!choDuyet.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ khách hàng nào đang chờ duyệt trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2'>";
                    foreach (var k in choDuyet) html += $"<li class='list-group-item'><b>{k.HoTen}</b> (Gặp: {k.BoPhanCanGap})</li>";
                    html += "</ul>";
                    return Json(new { answer = $"Dạ CÓ **{choDuyet.Count}** yêu cầu đang chờ duyệt {dateLabel.ToLower()}:<br/>{html}" });
                }

                if (ContainsAny(query, "đã duyệt", "vừa được xét duyệt"))
                {
                    var daDuyet = await _context.DangKyKhachs.Where(k => k.ThoiGianHen.Date == targetDate && k.TrangThaiDuyet == "Đã duyệt").ToListAsync();
                    if (!daDuyet.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ khách hàng nào đã được duyệt trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2'>";
                    foreach (var k in daDuyet) html += $"<li class='list-group-item'><b>{k.HoTen}</b> (Gặp: {k.BoPhanCanGap})</li>";
                    html += "</ul>";
                    return Json(new { answer = $"Dạ CÓ **{daDuyet.Count}** khách hàng đã được duyệt {dateLabel.ToLower()}:<br/>{html}" });
                }

                if (ContainsAny(query, "hết hạn"))
                {
                    var expired = await _context.DangKyKhachs.Where(k => k.ThoiGianHen.Date == targetDate && k.ThoiGianHen < DateTime.Now && k.TrangThaiDuyet == "Chờ duyệt").ToListAsync();
                    if (!expired.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ khách hàng nào hết hạn lịch hẹn trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2'>";
                    foreach (var k in expired) html += $"<li class='list-group-item text-danger'><b>{k.HoTen}</b> (Hẹn lúc: {k.ThoiGianHen:HH:mm}) - Đã quá giờ</li>";
                    html += "</ul>";
                    return Json(new { answer = $"Dạ CÓ **{expired.Count}** lịch hẹn đã hết hạn/quá giờ trong {dateLabel.ToLower()}:<br/>{html}" });
                }

                if (ContainsAny(query, "cần gặp giám đốc", "cần gặp phòng kế toán", "cần gặp phòng nhân sự"))
                {
                    string phong = query.Contains("giám đốc") ? "Ban Giám Đốc" : (query.Contains("kế toán") ? "Kế Toán" : "Nhân Sự");
                    var kh = await _context.DangKyKhachs.Where(x => x.BoPhanCanGap.Contains(phong) && x.ThoiGianHen.Date == targetDate).Select(x => x.HoTen).ToListAsync();
                    if (!kh.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ khách nào đăng ký gặp {phong} trong {dateLabel.ToLower()}." });
                    return Json(new { answer = $"Khách cần gặp **{phong}** {dateLabel.ToLower()}: " + string.Join(", ", kh) });
                }

                if (ContainsAny(query, "khách nào tên", "tìm khách", "ai tên"))
                {
                    string name = ExtractKeywordAfter(query, new[] { "khách nào tên là ", "khách nào tên ", "khách tên là ", "tên " });
                    if (!string.IsNullOrEmpty(name))
                    {
                        var guest = await _context.DangKyKhachs.Where(k => k.HoTen.ToLower().Contains(name)).OrderByDescending(k => k.ThoiGianHen).FirstOrDefaultAsync();
                        if (guest != null) return Json(new { answer = $"👤 **Hồ sơ Khách:** {guest.HoTen}<br/>- Muốn gặp: {guest.BoPhanCanGap}<br/>- Lý do: {guest.LyDo}<br/>- Lịch hẹn: {guest.ThoiGianHen:dd/MM/yyyy HH:mm}<br/>- Trạng thái duyệt: <span class='badge bg-primary'>{guest.TrangThaiDuyet}</span>" });
                    }
                    return Json(new { answer = $"Dạ KHÔNG CÓ khách nào mang tên này trong hệ thống." });
                }

                if (ContainsAny(query, "người này muốn gặp phòng nào", "ngày hẹn gặp của người này", "ngày hẹn của người này"))
                {
                    string name = ExtractKeywordAfter(query, new[] { "người này tên ", "người tên ", "là " });
                    if (!string.IsNullOrEmpty(name))
                    {
                        var guest = await _context.DangKyKhachs.Where(k => k.HoTen.ToLower().Contains(name)).OrderByDescending(k => k.ThoiGianHen).FirstOrDefaultAsync();
                        if (guest != null) return Json(new { answer = $"Khách hàng **{guest.HoTen}**:<br/>- Ngày hẹn: **{guest.ThoiGianHen:dd/MM/yyyy - HH:mm}**<br/>- Phòng cần gặp: **{guest.BoPhanCanGap}**." });
                    }
                    return Json(new { answer = "Bạn vui lòng gõ kèm theo tên khách hàng để tôi kiểm tra nhé (VD: ngày hẹn gặp của người tên Huy)." });
                }

                if (ContainsAny(query, "sẽ đến trong", "đã đến trong", "ai sẽ đến", "mấy giờ họ sẽ đến", "khách ngoài đăng ký có ai", "đối tượng lạ", "đăng ký"))
                {
                    var guests = await _context.DangKyKhachs.Where(k => k.ThoiGianHen.Date == targetDate).OrderBy(k => k.ThoiGianHen).ToListAsync();
                    if (!guests.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ khách ngoài/đối tác nào đến trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2'>";
                    foreach (var g in guests.Take(10)) html += $"<li class='list-group-item'><b>{g.HoTen}</b> - Lúc {g.ThoiGianHen:HH:mm} ({g.TrangThaiDuyet})</li>";
                    html += "</ul>";
                    return Json(new { answer = $"Danh sách {guests.Count} khách hàng {dateLabel.ToLower()}:<br/>{html}" });
                }

                // =====================================================================
                // 4. QUẢN LÝ BÃI ĐỖ & BẢN ĐỒ AN NINH
                // =====================================================================
                var allRecords = await _context.LichSuCheckIns.ToListAsync();
                var xeTrongBai = allRecords.GroupBy(x => x.BienSoXe).Select(g => g.OrderByDescending(x => x.ThoiGian).FirstOrDefault()).Where(x => x != null && x.Huong == "Đi Vào").ToList();
                int countA = xeTrongBai.Count(x => x.ViTriDo != null && x.ViTriDo.StartsWith("A"));
                int countB = xeTrongBai.Count(x => x.ViTriDo != null && x.ViTriDo.StartsWith("B"));
                int totalOto = xeTrongBai.Count(x => x.LoaiXe == null || x.LoaiXe.ToUpper().Contains("TÔ"));
                int motoCount = xeTrongBai.Count - totalOto;

                if (ContainsAny(query, "tầng hầm a có bao nhiêu bãi trống", "hầm a có bao nhiêu bãi trống")) return Json(new { answer = $"Tầng hầm A còn **{120 - countA}** bãi trống." });
                if (ContainsAny(query, "tầng hầm b có bao nhiêu bãi trống", "tấng hầm b có bao nhiêu bãi trống")) return Json(new { answer = $"Tầng hầm B còn **{120 - countB}** bãi trống." });
                if (ContainsAny(query, "tâng hầm xe máy còn bãi nào trống k", "xe máy còn bãi nào trống")) return Json(new { answer = $"Khu vực Xe Máy hiện đang có {motoCount} chiếc đậu, sức chứa vẫn còn trống." });
                if (ContainsAny(query, "tầng hầm có bao nhiêu bãi đang trống", "còn bao nhiêu bãi trống", "còn nhiêu bãi đang trống trong bãi đỗ xe"))
                    return Json(new { answer = $"Tổng quan: Hầm A còn trống {120 - countA} bãi. Hầm B còn trống {120 - countB} bãi." });

                if (ContainsAny(query, "có bao nhiêu xe đang đỗ")) return Json(new { answer = $"Dạ có tổng cộng **{xeTrongBai.Count} xe** đang đỗ trong bãi." });

                if (ContainsAny(query, "chi tiết những người đang đậu", "ai đang đỗ"))
                {
                    if (!xeTrongBai.Any()) return Json(new { answer = "Dạ KHÔNG CÓ xe nào đang đậu trong bãi." });
                    string html = "<ul class='list-group small mt-2 mb-2'>";
                    foreach (var x in xeTrongBai.Take(15)) html += $"<li class='list-group-item'>[Vị trí: <b class='text-danger'>{x.ViTriDo ?? "Chưa rõ"}</b>] {x.HoTen ?? "Khách"} - {x.BienSoXe}</li>";
                    html += "</ul>";
                    return Json(new { answer = $"Chi tiết danh sách xe đang đậu:<br/>{html}" });
                }

                if (ContainsAny(query, "lịch sử bãi đỗ xe", "lịch sử ra vào"))
                {
                    var ds = await _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate).OrderByDescending(x => x.ThoiGian).Take(15).ToListAsync();
                    if (!ds.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ lịch sử bãi đỗ xe trong {dateLabel.ToLower()}." });
                    string html = "<ul class='list-group small mt-2 mb-2'>";
                    foreach (var d in ds) { string c = d.Huong == "Đi Vào" ? "text-success" : "text-warning"; html += $"<li class='list-group-item'><b class='{c}'>[{d.Huong}]</b> {d.HoTen ?? "Khách"} - {d.BienSoXe} ({d.ThoiGian:HH:mm})</li>"; }
                    html += "</ul>";
                    return Json(new { answer = $"Lịch sử bãi đỗ xe {dateLabel.ToLower()}:<br/>{html}" });
                }

                // =====================================================================
                // 5. LỊCH SỬ RA VÀO (CHI TIẾT PHƯƠNG TIỆN)
                // =====================================================================
                if (ContainsAny(query, "biển số này", "có biển số", "thời gian của vào chiếc", "thời gian ra của chiếc", "thời gian ra vào của chiếc", "chủ nhân chiếc", "là loại xe gì", "hướng của chiếc", "chiếc xe có biển"))
                {
                    string bs = ExtractKeywordAfter(query, new[] { "biển số này là ", "biển số này", "biển số ", "biển " });
                    if (!string.IsNullOrEmpty(bs))
                    {
                        var xeInfoList = await _context.LichSuCheckIns.Where(x => x.BienSoXe.Contains(bs) && x.ThoiGian.Date == targetDate).OrderByDescending(x => x.ThoiGian).ToListAsync();
                        var xeLast = await _context.LichSuCheckIns.Where(x => x.BienSoXe.Contains(bs)).OrderByDescending(x => x.ThoiGian).FirstOrDefaultAsync();

                        if (xeLast == null) return Json(new { answer = $"Dạ KHÔNG CÓ dữ liệu của biển số '{bs}'." });

                        if (ContainsAny(query, "trạng thái phương tiện")) return Json(new { answer = $"Trạng thái hiện tại của xe {bs}: {(xeLast.Huong == "Đi Vào" ? $"Đang đậu tại {xeLast.ViTriDo}" : "Đã rời khỏi bãi")}." });
                        if (ContainsAny(query, "hướng của chiếc")) return Json(new { answer = $"Hướng lưu thông cuối cùng của xe {bs} là: **{xeLast.Huong}**." });
                        if (ContainsAny(query, "thời gian của vào", "thời gian ra của", "thời gian ra vào"))
                        {
                            if (!xeInfoList.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ lượt ra/vào nào của xe {bs} trong {dateLabel.ToLower()}." });
                            string log = string.Join("<br/>", xeInfoList.Select(x => $"- {x.Huong} lúc {x.ThoiGian:HH:mm:ss}"));
                            return Json(new { answer = $"Lịch sử ra/vào của xe {bs} {dateLabel.ToLower()}:<br/>{log}" });
                        }
                        if (ContainsAny(query, "chủ nhân chiếc", "chủ nhân")) return Json(new { answer = $"Chủ nhân của chiếc xe biển số {bs} là: **{xeLast.HoTen ?? "Khách vãng lai"}**." });
                        if (ContainsAny(query, "là loại xe gì")) return Json(new { answer = $"Chiếc xe {bs} thuộc phân loại: **{xeLast.LoaiXe ?? "Không rõ"}**." });
                        if (ContainsAny(query, "có đến vào ngày", "có đến")) return Json(new { answer = xeInfoList.Any() ? $"Dạ CÓ ĐẾN." : $"Dạ KHÔNG CÓ ĐẾN." });
                    }
                    return Json(new { answer = "Bạn vui lòng cung cấp biển số cụ thể nhé." });
                }

                if (ContainsAny(query, "thời gian đỗ và rời đi của người này", "thời gian này có người tên", "tìm cho tôi người tên", "họ đi xe gì", "họ đến mấy giờ", "biển số họ là bao nhiêu", "thông tin của người có tên"))
                {
                    string name = ExtractKeywordAfter(query, new[] { "người có tên là ", "người tên là ", "người tên ", "tên là ", "của người này", "tên " });
                    if (string.IsNullOrEmpty(name)) return Json(new { answer = "Dạ bạn vui lòng gõ kèm theo tên người bạn muốn tìm để tôi tra cứu nhé." });

                    var logList = await _context.LichSuCheckIns.Where(x => x.HoTen.ToLower().Contains(name) && x.ThoiGian.Date == targetDate).OrderBy(x => x.ThoiGian).ToListAsync();
                    var logLast = await _context.LichSuCheckIns.Where(x => x.HoTen.ToLower().Contains(name)).OrderByDescending(x => x.ThoiGian).FirstOrDefaultAsync();

                    if (logLast == null) return Json(new { answer = $"Dạ KHÔNG CÓ dữ liệu của người tên '{name}'." });

                    if (ContainsAny(query, "họ đi xe gì", "loại xe gì")) return Json(new { answer = $"Người tên {logLast.HoTen} thường đi xe: **{logLast.LoaiXe ?? "Không rõ"}**." });
                    if (ContainsAny(query, "biển số họ là bao nhiêu")) return Json(new { answer = $"Biển số xe của {logLast.HoTen} là: **{logLast.BienSoXe}**." });
                    if (ContainsAny(query, "họ đến mấy giờ", "thời gian đỗ và rời đi", "thời gian này có người tên"))
                    {
                        if (!logList.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ lịch sử ra/vào của '{name}' trong {dateLabel.ToLower()}." });
                        string html = string.Join("<br/>", logList.Select(x => $"- {x.Huong} lúc {x.ThoiGian:HH:mm} (Biển: {x.BienSoXe})"));
                        return Json(new { answer = $"Thời gian đỗ và rời đi của {logLast.HoTen} {dateLabel.ToLower()}:<br/>{html}" });
                    }

                    return Json(new { answer = $"👤 **Thông tin ra vào của {logLast.HoTen}**:<br/>- Loại xe: {logLast.LoaiXe ?? "Không rõ"}<br/>- Biển số: {logLast.BienSoXe}<br/>- Trạng thái gần nhất: {logLast.Huong} lúc {logLast.ThoiGian:dd/MM/yyyy HH:mm}" });
                }

                // =====================================================================
                // 6. DỮ LIỆU NHÂN SỰ & QUẢN LÝ THẺ CỨNG
                // =====================================================================
                if (ContainsAny(query, "nhân viên nào mã", "nhân viên nào tên", "cho tôi xem hết thông tin của", "cho tôi xem khuôn mặt và tên của", "tìm cho tôi thông tin của người"))
                {
                    string keyword = ExtractKeywordAfter(query, new[] { "mã là ", "mã ", "tên là ", "tên ", "tên của ", "của " });
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var nv = await _context.NhanViens.FirstOrDefaultAsync(x => x.HoTen.ToLower().Contains(keyword) || x.MaNV.ToLower() == keyword);
                        if (nv != null) return Json(new { answer = $"👤 **Dữ liệu Nhân sự:**<br/>- Họ và tên: {nv.HoTen}<br/>- Mã số: {nv.MaNV}<br/>- Chức vụ/Phòng ban: {nv.ChucVu}<br/>- Biển số đăng ký: {nv.BienSoXe ?? "Không có"}" });
                    }
                    return Json(new { answer = $"Dạ KHÔNG CÓ thông tin nhân sự khớp với từ khóa." });
                }

                if (ContainsAny(query, "kế toán gồm", "nhân sự gồm", "it gồm"))
                {
                    string phong = query.Contains("kế toán") ? "Kế Toán" : (query.Contains("it") ? "IT" : "Nhân Sự");
                    var nvs = await _context.NhanViens.Where(x => x.ChucVu.Contains(phong)).Select(x => x.HoTen).ToListAsync();
                    if (!nvs.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ ai thuộc phòng {phong}." });
                    return Json(new { answer = $"Nhân sự phòng **{phong}** gồm: " + string.Join(", ", nvs) });
                }

                if (ContainsAny(query, "thẻ mã", "nhân viên đố tên là gì", "thẻ nhân viên hay thẻ khách hàng", "có thẻ nào có tên", "có thẻ nào có mã"))
                {
                    string keyword = ExtractKeywordAfter(query, new[] { "mã số ", "mã ", "tên là ", "tên " });
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var the = await _context.TheXes.FirstOrDefaultAsync(x => x.MaThe == keyword || (x.NguoiGiu != null && x.NguoiGiu.ToLower().Contains(keyword)));
                        if (the != null)
                        {
                            if (ContainsAny(query, "tên là gì", "đố tên là gì")) return Json(new { answer = $"Thẻ mã {the.MaThe} do **{the.NguoiGiu ?? "người chưa xác định"}** giữ ạ." });
                            if (query.Contains("nhân viên hay thẻ khách")) return Json(new { answer = $"Thẻ mang mã {the.MaThe} là **{the.LoaiThe}**." });

                            string tt = the.TrangThai == 1 ? "Đang sử dụng" : (the.TrangThai == 0 ? "Sẵn sàng" : "Đã khóa/Báo mất");
                            return Json(new { answer = $"💳 **Tra cứu Thẻ Cứng {the.MaThe}**:<br/>- Loại thẻ: {the.LoaiThe}<br/>- Người đang giữ: {the.NguoiGiu ?? "Chưa cấp"}<br/>- Trạng thái: {tt}" });
                        }
                        return Json(new { answer = $"Dạ KHÔNG CÓ thẻ nào khớp với mã/tên này." });
                    }
                }

                // =====================================================================
                // 7. TRỰC BAN & BÁO CÁO TỔNG QUAN
                // =====================================================================
                if (ContainsAny(query, "tổng có bao nhiêu nhân viên đã ra vào", "tổng số lượt xe ra vào", "có bao nhiêu xe máy ra vào", "có bao nhiêu xe oto ra vào", "xe ô tô ra vào", "chi tiết ra vào"))
                {
                    var checkins = await _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate).ToListAsync();
                    if (query.Contains("nhân viên đã ra vào")) return Json(new { answer = $"Dạ {dateLabel.ToLower()} có tổng cộng **{checkins.Where(x => !string.IsNullOrEmpty(x.HoTen)).Select(x => x.HoTen).Distinct().Count()} nhân viên** đã thực hiện ra/vào." });
                    if (query.Contains("máy")) return Json(new { answer = $"Dạ, {dateLabel.ToLower()} có tổng cộng **{checkins.Count(x => x.LoaiXe != null && x.LoaiXe.Contains("MÁY"))}** lượt xe máy ra vào." });
                    if (ContainsAny(query, "oto", "ô tô")) return Json(new { answer = $"Dạ, {dateLabel.ToLower()} có tổng cộng **{checkins.Count(x => x.LoaiXe != null && x.LoaiXe.Contains("TÔ"))}** lượt ô tô ra vào." });

                    if (ContainsAny(query, "chi tiết ra vào", "danh sách ra vào"))
                    {
                        if (!checkins.Any()) return Json(new { answer = $"Dạ KHÔNG CÓ lượt ra vào nào." });
                        string html = "<ul class='list-group small mt-2 mb-2'>";
                        foreach (var d in checkins.OrderByDescending(x => x.ThoiGian).Take(15)) { string c = d.Huong == "Đi Vào" ? "text-success" : "text-warning"; html += $"<li class='list-group-item'><b class='{c}'>[{d.Huong}]</b> {d.BienSoXe} - {d.HoTen ?? "Khách"} lúc {d.ThoiGian:HH:mm}</li>"; }
                        html += "</ul>";
                        return Json(new { answer = $"Chi tiết lượt ra vào {dateLabel.ToLower()}:<br/>{html}" });
                    }
                    return Json(new { answer = $"Tổng số lượt xe ra vào {dateLabel.ToLower()} là <b>{checkins.Count}</b>." });
                }

                if (ContainsAny(query, "tỷ lệ chấm công"))
                {
                    var checkins = await _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate && x.Huong == "Đi Vào" && !string.IsNullOrEmpty(x.HoTen)).ToListAsync();
                    var firstCheckIns = checkins.GroupBy(x => x.HoTen).Select(g => g.OrderBy(x => x.ThoiGian).First().ThoiGian).ToList();
                    int onTime = firstCheckIns.Count(t => t.TimeOfDay <= new TimeSpan(8, 15, 0));
                    int late = firstCheckIns.Count(t => t.TimeOfDay > new TimeSpan(8, 15, 0));
                    double onTimeRate = (onTime + late) > 0 ? Math.Round((double)onTime / (onTime + late) * 100, 1) : 0;
                    return Json(new { answer = $"Tỷ lệ chấm công {dateLabel.ToLower()} là <b>{onTimeRate}%</b> đúng giờ.", pieChart = new { labels = new[] { "Đúng giờ", "Đi trễ" }, data = new[] { onTime, late }, colors = new[] { "#198754", "#ffc107" } } });
                }

                if (ContainsAny(query, "sơ đồ lượng", "sơ đồ lưu lượng", "biểu đồ"))
                {
                    var rawData = await _context.LichSuCheckIns.Where(x => x.ThoiGian.Date >= today.AddDays(-6)).Select(x => x.ThoiGian.Date).ToListAsync();
                    var flowData = rawData.GroupBy(x => x).Select(g => new { Date = g.Key.ToString("dd/MM"), Count = g.Count() }).OrderBy(x => x.Date).ToList();
                    return Json(new { answer = "Sơ đồ lưu lượng xe 7 ngày qua:", chart = new { labels = flowData.Select(x => x.Date).ToArray(), data = flowData.Select(x => x.Count).ToArray() } });
                }

                // =====================================================================
                // 8. THIẾT BỊ
                // =====================================================================
                if (ContainsAny(query, "có thiết bị nào đang bị hư", "thiết bị nào đang bị hư", "thiết bị hư hỏng")) return Json(new { answer = "Dạ KHÔNG CÓ thiết bị nào hư hỏng." });
                if (ContainsAny(query, "có tổng bao nhiêu thiết bị", "thiết đang chạy", "thiết đang tắt"))
                {
                    string res = "Có tổng cộng **12 thiết bị** (4 ALPR, 4 FaceID, 4 Barie).";
                    if (query.Contains("đang chạy")) res += " Đang chạy: **12 thiết bị**.";
                    if (query.Contains("đang tắt")) res += " Đang tắt: **0 thiết bị**.";
                    return Json(new { answer = res });
                }

                // =====================================================================
                // 9. FALLBACK MẶC ĐỊNH
                // =====================================================================
                return Json(new { answer = "Dạ, hệ thống chưa tìm ra dữ liệu khớp với câu hỏi của bạn. Bạn hãy cung cấp cụ thể hơn (VD: Tên người, Biển số xe, hoặc ngày tháng) nhé!" });
            }
            catch (Exception)
            {
                return Json(new { answer = $"Dạ KHÔNG CÓ." });
            }
        }

        // =====================================================================
        // CÁC HÀM HỖ TRỢ
        // =====================================================================
        private bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var kw in keywords) { if (text.Contains(kw)) return true; }
            return false;
        }

        private DateTime ExtractDateFromQuery(string query, DateTime today)
        {
            if (ContainsAny(query, "hôm qua", "ngày qua")) return today.AddDays(-1);
            if (ContainsAny(query, "ngày mai", "hôm sau")) return today.AddDays(1);
            if (ContainsAny(query, "hôm kia")) return today.AddDays(-2);
            return today;
        }

        private string GetDateLabel(DateTime target, DateTime today)
        {
            if (target == today) return "Hôm nay";
            if (target == today.AddDays(-1)) return "Hôm qua";
            if (target == today.AddDays(1)) return "Ngày mai";
            return $"Ngày {target:dd/MM}";
        }

        private string ExtractKeywordAfter(string query, string[] prefixes)
        {
            foreach (var p in prefixes)
            {
                int idx = query.IndexOf(p);
                if (idx != -1)
                {
                    string afterPrefix = query.Substring(idx + p.Length).Trim().Replace("?", "");
                    if (!string.IsNullOrEmpty(afterPrefix)) return afterPrefix.Split(' ')[0];
                }
            }
            return "";
        }
    }

    public class AIRequest { public string Query { get; set; } }
}