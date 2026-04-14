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

        // ==========================================
        // ĐÃ THÊM 2 TRƯỜNG MỚI KHỚP VỚI GIAO DIỆN
        // ==========================================
        [Required(ErrorMessage = "Vui lòng chọn nhân viên cần gặp")]
        [Display(Name = "Người cần gặp")]
        public string NhanVienCanGap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng người")]
        [Range(1, 10, ErrorMessage = "Số lượng người chỉ được phép từ 1 đến 10 người")]
        [Display(Name = "Số lượng người đi cùng")]
        public int SoLuongNguoi { get; set; } = 1;
        // ==========================================

        [Display(Name = "Ngày giờ hẹn")]
        public DateTime ThoiGianHen { get; set; }

        [Display(Name = "Trạng thái duyệt")]
        public string TrangThaiDuyet { get; set; } = "Chờ duyệt"; // Các trạng thái: Chờ duyệt, Đã duyệt, Từ chối
    }
}