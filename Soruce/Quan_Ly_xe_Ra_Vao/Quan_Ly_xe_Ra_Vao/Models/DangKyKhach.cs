using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class DangKyKhach
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và Tên Khách")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải quét khuôn mặt")]
        public string FaceDataPath { get; set; } // AI sẽ dùng ảnh này để nhận diện lúc khách đến

        [Display(Name = "Biển số xe (Nếu có)")]
        public string BienSoXe { get; set; }

        [Display(Name = "Loại xe")]
        public string LoaiXe { get; set; } // "Ô tô" hoặc "Xe máy"

        [Required(ErrorMessage = "Vui lòng nhập lý do")]
        [Display(Name = "Lý do vào công ty")]
        public string LyDo { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bộ phận")]
        [Display(Name = "Bộ phận cần gặp")]
        public string BoPhanCanGap { get; set; }

        [Display(Name = "Ngày giờ hẹn")]
        public DateTime ThoiGianHen { get; set; }

        [Display(Name = "Trạng thái duyệt")]
        public string TrangThaiDuyet { get; set; } = "Chờ duyệt"; // Các trạng thái: Chờ duyệt, Đã duyệt, Từ chối
    }
}