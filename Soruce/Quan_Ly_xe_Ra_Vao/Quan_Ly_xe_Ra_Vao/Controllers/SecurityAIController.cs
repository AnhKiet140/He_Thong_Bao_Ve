using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly string _mistralApiKey;

        public SecurityAIController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _mistralApiKey = _configuration["ApiKeys:MistralAI"]?.Trim();
        }

        [HttpPost]
        public async Task<IActionResult> AskAI([FromBody] AIRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(_mistralApiKey))
                {
                    return Json(new { answer = "Lỗi hệ thống: Chưa cấu hình API Key của Mistral trong file appsettings.json!" });
                }

                if (string.IsNullOrEmpty(request?.Query) && string.IsNullOrEmpty(request?.ImageBase64))
                    return Json(new { answer = "Dạ, bạn chưa nhập câu hỏi hoặc gửi ảnh. Bạn cần tôi giúp gì ạ?" });

                string query = request.Query?.ToLower().Trim() ?? "";

                if (query.Contains("clear") || query.Contains("xóa") || query.Contains("reset"))
                    return Json(new { answer = "CLEAR_COMMAND" });

                var today = DateTime.Today;
                DateTime targetDate = ExtractDateFromQuery(query, today);
                string dateLabel = GetDateLabel(targetDate, today);

                // =========================================================
                // 1. GOM DỮ LIỆU TỪ DATABASE (KÉO VỀ RAM TRƯỚC KHI FORMAT)
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
                // 2. CHUẨN BỊ SYSTEM PROMPT (ÉP BUỘC TRẢ VỀ JSON CHUẨN)
                // =========================================================
                string systemPrompt = $@"
Bạn là 'Security AI' - Trợ lý an ninh đa phương thức của CHK-IN PRO. 
Thời gian hệ thống hiện tại: {DateTime.Now:dd/MM/yyyy HH:mm}.
Dữ liệu ngày đang truy vấn: {dateLabel}.

[SỰ THẬT TỪ CƠ SỞ DỮ LIỆU]:
- BÃI ĐỖ XE: Hầm A còn {120 - countA} chỗ. Hầm B còn {120 - countB} chỗ. Tổng xe đang đỗ: {xeTrongBai.Count}.
- LỊCH SỬ XE RA VÀO: {JsonSerializer.Serialize(checkins)}
- KHÁCH HẸN HÔM NAY: {JsonSerializer.Serialize(khachs)}
- CẢNH BÁO AN NINH: {JsonSerializer.Serialize(canhBaos)}
- DANH SÁCH NHÂN SỰ: {JsonSerializer.Serialize(nhanViens)}

[QUY TẮC ĐẦU RA - BẮT BUỘC]:
BẠN BẮT BUỘC PHẢI TRẢ LỜI BẰNG 1 CHUỖI JSON ĐÚNG ĐỊNH DẠNG SAU:
{{
  ""answer"": ""Câu trả lời chính. Tuyệt đối KHÔNG dùng Markdown như **, ###. Chỉ dùng HTML cơ bản như <b>, <br/>, <ul><li>. Nếu có ảnh, hãy mô tả chi tiết ảnh."",
  ""analysis"": ""Phân tích chuyên sâu (hoặc null)"",
  ""showExcel"": true/false, 
  ""isDanger"": true/false, 
  ""imageUrl"": ""Link ảnh minh họa (hoặc null)"",
  ""chart"": {{ ""labels"": [""Tên 1"", ""Tên 2""], ""data"": [10, 5], ""colors"": [""#198754"", ""#ffc107""] }} // Hoặc null.
}}
";

                // =========================================================
                // 3. GỌI MISTRAL API (CÓ XỬ LÝ HÌNH ẢNH)
                // =========================================================
                string mistralJsonResponse = await CallMistralAPI(systemPrompt, request.Query, request.ImageBase64);

                // =========================================================
                // 4. BÓC TÁCH JSON VÀ DỌN DẸP RÁC MARKDOWN (BẢO VỆ GIAO DIỆN)
                // =========================================================
                using (JsonDocument doc = JsonDocument.Parse(mistralJsonResponse))
                {
                    var root = doc.RootElement;

                    // Lấy câu trả lời và dọn dẹp các ký tự Markdown cứng đầu lỡ lọt vào
                    string answer = root.TryGetProperty("answer", out var ansElement) ? ansElement.GetString() : "";
                    answer = answer.Replace("```html", "").Replace("```", "");
                    answer = answer.Replace("\n", "<br/>");
                    answer = Regex.Replace(answer, @"(<br/>\s*){2,}", "<br/><br/>");
                    answer = Regex.Replace(answer, @"\*\*(.*?)\*\*", "<b>$1</b>");
                    answer = Regex.Replace(answer, @"### (.*?)<br/>", "<h6 class='text-primary mt-3 fw-bold'>$1</h6><br/>");
                    answer = answer.Replace("---", "<hr class='my-2'/>");

                    // Bóc tách các trường khác
                    string analysis = root.TryGetProperty("analysis", out var anElement) ? anElement.GetString() : null;
                    bool showExcel = root.TryGetProperty("showExcel", out var excelElement) && excelElement.ValueKind == JsonValueKind.True;
                    bool isDanger = root.TryGetProperty("isDanger", out var dangerElement) && dangerElement.ValueKind == JsonValueKind.True;
                    string imageUrl = root.TryGetProperty("imageUrl", out var imgElement) ? imgElement.GetString() : null;

                    // Xử lý object Chart
                    object chart = null;
                    if (root.TryGetProperty("chart", out var chartElement) && chartElement.ValueKind != JsonValueKind.Null)
                    {
                        chart = JsonSerializer.Deserialize<object>(chartElement.GetRawText());
                    }

                    bool finalIsDanger = isDanger || (canhBaos.Any() && query.Contains("nguy hiểm"));

                    return Json(new
                    {
                        answer = answer,
                        analysis = analysis,
                        showExcel = showExcel,
                        isDanger = finalIsDanger,
                        imageUrl = imageUrl,
                        chart = chart
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { answer = "Dạ, kết nối với bộ não AI đang bị gián đoạn. Lỗi: " + ex.Message });
            }
        }

        // HÀM CALL API MISTRAL AI (HỖ TRỢ MULTIMODAL)
        private async Task<string> CallMistralAPI(string systemPrompt, string userText, string imageBase64)
        {
            using (var client = new HttpClient())
            {
                string url = "https://api.mistral.ai/v1/chat/completions";
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mistralApiKey}");

                // Đóng gói nội dung User: Có ảnh hoặc Không có ảnh
                object userContent;
                if (!string.IsNullOrEmpty(imageBase64))
                {
                    string textPrompt = string.IsNullOrEmpty(userText) ? "Hãy phân tích bức ảnh này." : userText;
                    userContent = new object[] {
                        new { type = "text", text = textPrompt },
                        new { type = "image_url", image_url = new { url = imageBase64 } }
                    };
                }
                else
                {
                    userContent = userText;
                }

                var payload = new
                {
                    model = "pixtral-large-latest",
                    response_format = new { type = "json_object" },

                    // ĐÃ SỬA LỖI BIÊN DỊCH Ở ĐÂY: Thêm 'object[]' và ép kiểu '(object)systemPrompt'
                    messages = new object[]
                    {
                        new { role = "system", content = (object)systemPrompt },
                        new { role = "user", content = userContent }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"{{\"answer\": \"<span class='text-danger'>Lỗi API Mistral: {errorDetails}</span>\"}}";
                }

                var responseString = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    var root = doc.RootElement;
                    var jsonText = root.GetProperty("choices")[0]
                                       .GetProperty("message")
                                       .GetProperty("content").GetString();
                    return jsonText;
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

    public class AIRequest
    {
        public string Query { get; set; }
        public string ImageBase64 { get; set; }
    }
}