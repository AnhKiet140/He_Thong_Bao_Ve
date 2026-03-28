using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using Quan_Ly_xe_Ra_Vao.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class LichSuCheckInController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichSuCheckInController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. MỞ TRANG DANH SÁCH LỊCH SỬ (Load cái mới nhất lên đầu)
        public IActionResult Index()
        {
            var history = _context.LichSuCheckIns.OrderByDescending(x => x.ThoiGian).ToList();
            return View(history);
        }

        // 2. API CHO TRÍ TUỆ NHÂN TẠO GỌI ĐỂ GHI NHẬT KÝ (Chạy ngầm)
        [HttpPost]
        public async Task<IActionResult> GhiNhatKy([FromBody] LichSuCheckIn log)
        {
            if (log == null) return BadRequest();

            log.ThoiGian = DateTime.Now; // Đóng dấu thời gian thực tế

            _context.LichSuCheckIns.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã ghi log thành công" });
        }
    }
}