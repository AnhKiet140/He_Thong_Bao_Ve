using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class KhachNgoai
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string HoTen { get; set; }

        [Required]
        public string CCCD { get; set; }

        public string SoDienThoai { get; set; }

        public string BienSoXe { get; set; }

        public string PhongBanCanGap { get; set; }

        public DateTime ThoiGianDangKy { get; set; } = DateTime.Now;

        // Trạng thái duyệt: 0 = Chờ duyệt, 1 = Đã duyệt (Cấp quyền), 2 = Từ chối
        public int TrangThai { get; set; } = 0;

        // Ảnh khuôn mặt khách chụp lúc điền form
        public string FaceDataPath { get; set; }
    }
}