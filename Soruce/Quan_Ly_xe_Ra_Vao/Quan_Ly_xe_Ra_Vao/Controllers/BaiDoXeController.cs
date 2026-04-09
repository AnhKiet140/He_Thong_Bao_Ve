using Microsoft.AspNetCore.Mvc;
using Quan_Ly_xe_Ra_Vao.Data;
using System;
using System.Linq;

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
    }
}