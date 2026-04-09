using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Quan_Ly_xe_Ra_Vao.Data;
using System.Linq;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    [Authorize]
    public class BanDoAnNinhController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BanDoAnNinhController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Tìm tất cả xe có Trạng thái chứa chữ "đậu" và có Vị trí đỗ
            var occupiedSlots = _context.LichSuCheckIns
                                        .Where(x => x.TrangThai != null && x.TrangThai.ToLower().Contains("đậu"))
                                        .Where(x => x.ViTriDo != null)
                                        .Select(x => x.ViTriDo.Trim()) // Trim() để cắt hết dấu cách thừa
                                        .ToList();

            ViewBag.OccupiedSlots = occupiedSlots;

            return View();
        }
    }
}