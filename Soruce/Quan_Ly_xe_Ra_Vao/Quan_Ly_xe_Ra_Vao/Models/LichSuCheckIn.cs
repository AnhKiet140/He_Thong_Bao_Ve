using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class LichSuCheckIn
    {
        [Key]
        public int Id { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        // Thêm dấu ? sau string để cho phép Null, tránh lỗi "chặn cửa" của Database
        public string? LoaiDoiTuong { get; set; }

        public string? HoTen { get; set; }

        public string? BienSoXe { get; set; }

        public string? PhuongThuc { get; set; }

        public string? TrangThai { get; set; }

        public string? Huong { get; set; }

        public string? HinhAnh { get; set; }

        [Display(Name = "Loại phương tiện")]
        public string? LoaiXe { get; set; }

        [Display(Name = "Hình ảnh toàn cảnh xe")]
        public string? HinhAnhXe { get; set; }

        public string? ViTriDo { get; set; } // Lưu mã vị trí như A-01, A-02...

    }
}