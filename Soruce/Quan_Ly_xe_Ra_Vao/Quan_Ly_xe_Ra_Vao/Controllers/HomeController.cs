using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Models;
using Quan_Ly_xe_Ra_Vao.Data;
using System.Linq;
using System.Threading.Tasks; // Thêm thư viện chạy đa luồng
using Microsoft.EntityFrameworkCore; // Thêm thư viện để Database chạy mượt hơn

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ========================================================
        // ĐÃ NÂNG CẤP LÊN ASYNC: Chống nghẽn cổ chai (Timeout)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> GetFaceData()
        {
            // Dùng await và ToListAsync() để Server tải ngầm, không bị đơ
            var data = await _context.NhanViens
                .Where(n => !string.IsNullOrEmpty(n.FaceDataPath) && n.FaceDataPath != "chua_co_anh.jpg")
                .Select(n => new {
                    name = n.HoTen,
                    role = n.ChucVu,
                    image = n.FaceDataPath
                }).ToListAsync();

            return Json(data);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}