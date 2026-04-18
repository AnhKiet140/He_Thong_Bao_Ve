using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // THÊM THƯ VIỆN NÀY ĐỂ ĐỌC APPSETTINGS
using Quan_Ly_xe_Ra_Vao.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class SecurityAIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _mistralApiKey; // Đã bỏ gán cứng ở đây

        // ĐÃ SỬA: Nhúng IConfiguration vào Constructor để đọc Key
        public SecurityAIController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // Kéo Key an toàn từ appsettings.json
            _mistralApiKey = _configuration["ApiKeys:MistralAI"];
        }

        [HttpPost]
        public async Task<IActionResult> AskAI([FromBody] AIRequest request)
        {
            try
            {
                // Bảo vệ thêm 1 lớp: Báo lỗi nếu quên chưa nhập Key vào appsettings
                if (string.IsNullOrEmpty(_mistralApiKey))
                {
                    return Json(new { answer = "Lỗi hệ thống: Chưa cấu hình API Key của Mistral trong file appsettings.json!" });
                }

                if (string.IsNullOrEmpty(request?.Query))
                    return Json(new { answer = "Dạ, bạn chưa nhập câu hỏi. Bạn cần tôi giúp gì ạ?" });

                string query = request.Query.ToLower().Trim();

                if (query.Contains("clear") || query.Contains("xóa") || query.Contains("reset"))
                    return Json(new { answer = "CLEAR_COMMAND" });

                var today = DateTime.Today;
                DateTime targetDate = ExtractDateFromQuery(query, today);
                string dateLabel = GetDateLabel(targetDate, today);

                // =========================================================
                // 1. GOM DỮ LIỆU TỪ DATABASE
                // =========================================================
                var rawCheckins = await _context.LichSuCheckIns.Where(x => x.ThoiGian.Date == targetDate).ToListAsync();
                var checkins = rawCheckins.Select(x => new { x.HoTen, x.BienSoXe, x.LoaiXe, x.Huong, ThoiGian = x.ThoiGian.ToString("HH:mm") }).ToList();

                var rawKhachs = await _context.DangKyKhachs.Where(x => x.ThoiGianHen.Date == targetDate).ToListAsync();
                var khachs = rawKhachs.Select(x => new { x.HoTen, x.BoPhanCanGap, x.TrangThaiDuyet, ThoiGian = x.ThoiGianHen.ToString("HH:mm") }).ToList();

                var rawCanhBaos = await _context.NhatKyHeThongs.Where(x => x.ThoiGian.Date == targetDate && (x.MucDo == "NGUY HIỂM" || x.MucDo == "CẢNH BÁO")).ToListAsync();
                var canhBaos = rawCanhBaos.Select(x => new { x.ChiTietThaoTac, x.NguoiThucHien, ThoiGian = x.ThoiGian.ToString("HH:mm") }).ToList();

                var allRecords = await _context.LichSuCheckIns.ToListAsync();
                var xeTrongBai = allRecords.GroupBy(x => x.BienSoXe).Select(g => g.OrderByDescending(x => x.ThoiGian).FirstOrDefault()).Where(x => x != null && x.Huong == "Đi Vào").ToList();

                int countA = xeTrongBai.Count(x => x.ViTriDo != null && x.ViTriDo.StartsWith("A"));
                int countB = xeTrongBai.Count(x => x.ViTriDo != null && x.ViTriDo.StartsWith("B"));

                var nhanViens = await _context.NhanViens.Select(x => new { x.MaNV, x.HoTen, x.ChucVu }).ToListAsync();

                // =========================================================
                // 2. CHUẨN BỊ SYSTEM PROMPT "KỶ LUẬT SẮT" CHO MISTRAL
                // =========================================================
                string systemPrompt = $@"
Bạn là 'Security AI' - Trợ lý an ninh thông minh của hệ thống Quản Lý Xe Ra Vào (CHK-IN PRO).
Thời gian hệ thống hiện tại: {DateTime.Now:dd/MM/yyyy HH:mm}.
Dữ liệu đang được truy vấn thuộc về: {dateLabel} ({targetDate:dd/MM/yyyy}).

[SỰ THẬT TỪ CƠ SỞ DỮ LIỆU (GROUND TRUTH)]:
- BÃI ĐỖ XE: Hầm A còn {120 - countA} chỗ. Hầm B còn {120 - countB} chỗ. Tổng xe đang đỗ: {xeTrongBai.Count}.
- LỊCH SỬ XE RA VÀO: {JsonSerializer.Serialize(checkins)}
- KHÁCH HẸN HÔM NAY: {JsonSerializer.Serialize(khachs)}
- CẢNH BÁO AN NINH: {JsonSerializer.Serialize(canhBaos)}
- DANH SÁCH NHÂN SỰ: {JsonSerializer.Serialize(nhanViens)}

[QUY TẮC ĐỊNH DẠNG ĐẦU RA - BẮT BUỘC TUÂN THỦ 100%]:
1. CẤM TUYỆT ĐỐI SỬ DỤNG MARKDOWN: Không bao giờ được sinh ra các ký tự như dấu sao (* hoặc **), dấu thăng (# hoặc ###), gạch ngang (---).
2. KHÔNG DÙNG BLOCK CODE: Tuyệt đối không bọc câu trả lời trong các dấu backticks như ```html hay ```json. Chỉ trả về văn bản thuần.
3. KHÔNG DÙNG KÝ TỰ XUỐNG DÒNG \n: Bắt buộc phải thay thế mọi thao tác xuống dòng bằng thẻ HTML <br/>.
4. CHỈ ĐƯỢC PHÉP SỬ DỤNG CÁC THẺ HTML SAU ĐỂ TRANG TRÍ:
   - Nhấn mạnh/In đậm: <b>nội dung</b>
   - Ngắt dòng: <br/>
   - Danh sách: <ul class='list-group small mt-2 mb-2'> <li class='list-group-item'>nội dung</li> </ul>
   - Tiêu đề từng phần: <h6 class='text-primary mt-2 mb-1 fw-bold'>Tiêu đề</h6>
5. NGUYÊN TẮC TRẢ LỜI: Dựa hoàn toàn vào [SỰ THẬT TỪ CƠ SỞ DỮ LIỆU]. Không suy diễn, không bịa đặt số liệu. Nếu không tìm thấy thông tin, hãy nói rõ: 'Hệ thống chưa ghi nhận dữ liệu này'. Hành văn chuyên nghiệp, ngắn gọn, xưng 'Tôi' và gọi 'Bạn'.
";

                // =========================================================
                // 3. GỌI MISTRAL API
                // =========================================================
                string mistralResponse = await CallMistralAPI(systemPrompt, request.Query);

                // =========================================================
                // 4. MÀNG LỌC CUỐI: DỌN DẸP MARKDOWN RÁC BẰNG REGEX
                // =========================================================
                mistralResponse = mistralResponse.Replace("```html", "").Replace("```", "");
                mistralResponse = mistralResponse.Replace("\n", "<br/>");
                mistralResponse = Regex.Replace(mistralResponse, @"(<br/>\s*){2,}", "<br/><br/>");
                mistralResponse = Regex.Replace(mistralResponse, @"\*\*(.*?)\*\*", "<b>$1</b>");
                mistralResponse = Regex.Replace(mistralResponse, @"### (.*?)<br/>", "<h6 class='text-primary mt-3 fw-bold'>$1</h6><br/>");
                mistralResponse = mistralResponse.Replace("---", "<hr class='my-2'/>");

                bool isDanger = canhBaos.Any() && query.Contains("nguy hiểm");

                return Json(new { answer = mistralResponse, isDanger = isDanger });
            }
            catch (Exception ex)
            {
                return Json(new { answer = "Dạ, kết nối với bộ não AI đang bị gián đoạn. Lỗi: " + ex.Message });
            }
        }

        // HÀM CALL API MISTRAL AI
        private async Task<string> CallMistralAPI(string systemPrompt, string userPrompt)
        {
            using (var client = new HttpClient())
            {
                // URL chuẩn của Mistral
                string url = "https://api.mistral.ai/v1/chat/completions";

                // Header xác thực
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mistralApiKey}");

                var payload = new
                {
                    model = "mistral-large-latest", // Model mạnh nhất của Mistral
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"Xin lỗi, API Key của Mistral bị lỗi hoặc hết hạn mức. Chi tiết: {errorDetails}";
                }

                var responseString = await response.Content.ReadAsStringAsync();

                // Parse JSON trả về theo cấu trúc của Mistral
                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    var root = doc.RootElement;
                    var text = root.GetProperty("choices")[0]
                                   .GetProperty("message")
                                   .GetProperty("content").GetString();
                    return text;
                }
            }
        }

        private DateTime ExtractDateFromQuery(string query, DateTime today)
        {
            if (query.Contains("hôm qua") || query.Contains("ngày qua")) return today.AddDays(-1);
            if (query.Contains("ngày mai") || query.Contains("hôm sau")) return today.AddDays(1);
            if (query.Contains("hôm kia")) return today.AddDays(-2);
            return today;
        }

        private string GetDateLabel(DateTime target, DateTime today)
        {
            if (target == today) return "Hôm nay";
            if (target == today.AddDays(-1)) return "Hôm qua";
            if (target == today.AddDays(1)) return "Ngày mai";
            return $"Ngày {target:dd/MM}";
        }
    }

    public class AIRequest { public string Query { get; set; } }
}