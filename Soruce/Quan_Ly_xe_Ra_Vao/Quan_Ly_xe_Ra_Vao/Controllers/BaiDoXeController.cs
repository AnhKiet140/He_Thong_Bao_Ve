using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;

namespace Quan_Ly_xe_Ra_Vao.Controllers
{
    public class BaiDoXeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BaiDoXeController(ApplicationDbContext context) { _context = context; }

        public IActionResult Index(DateTime? selectedDate, int page = 1, string searchName = "", string filterStatus = "Tất cả", string tang = "B1")
        {
            // Mặc định là ngày hôm nay nếu không có ngày được chọn
            DateTime ngayLoc = selectedDate ?? DateTime.Today;

            var query = _context.LichSuCheckIns
                .Where(x => x.ThoiGian.Date == ngayLoc.Date);

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(x => x.HoTen.Contains(searchName) || x.BienSoXe.Contains(searchName));

            if (filterStatus == "Đã ra")
                query = query.Where(x => x.Huong == "Đi Ra");
            else if (filterStatus == "Đang đậu")
                query = query.Where(x => x.Huong == "Đi Vào");

            var records = query.OrderByDescending(x => x.ThoiGian).ToList();

            ViewBag.SelectedDate = ngayLoc.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.SearchName = searchName;
            ViewBag.FilterStatus = filterStatus;
            ViewBag.CurrentTang = tang;

            return View(records);
        }

        // LỆNH DỌN RÁC ĐỂ TEST
        [AllowAnonymous]
        public async Task<IActionResult> ClearData()
        {
            // Xóa sạch toàn bộ lịch sử xe ra vào
            _context.LichSuCheckIns.RemoveRange(_context.LichSuCheckIns);

            // Xóa sạch toàn bộ khách đăng ký nháp (nếu muốn)
            // _context.DangKyKhachs.RemoveRange(_context.DangKyKhachs); 

            await _context.SaveChangesAsync();
            return Content("🚀 ĐÃ XÓA SẠCH DỮ LIỆU CŨ! Bạn hãy quay lại trang web và nhấn F5 để test lại từ đầu.");
        }
    }

}