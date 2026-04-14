using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class KhachNgoai
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; }

        // Các trường mặc định của bạn (Có thể để trống trên form nếu chưa cần dùng ngay)
        public string CCCD { get; set; }
        public string SoDienThoai { get; set; }

        [Display(Name = "Biển số xe")]
        public string BienSoXe { get; set; }

        [Display(Name = "Phòng ban cần gặp")]
        public string PhongBanCanGap { get; set; }

        public DateTime ThoiGianDangKy { get; set; } = DateTime.Now;

        // Trạng thái duyệt: 0 = Chờ duyệt, 1 = Đã duyệt (Cấp quyền), 2 = Từ chối
        public int TrangThai { get; set; } = 0;

        // Ảnh khuôn mặt khách chụp lúc điền form
        [Required(ErrorMessage = "Bắt buộc phải chụp ảnh khuôn mặt")]
        public string FaceDataPath { get; set; }

        // =========================================================
        // CÁC TRƯỜNG BỔ SUNG ĐỂ KHỚP VỚI GIAO DIỆN FORM ĐĂNG KÝ
        // =========================================================
        [Display(Name = "Loại xe")]
        public string LoaiXe { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhân viên cần gặp")]
        [Display(Name = "Nhân viên cần gặp")]
        public string NhanVienCanGap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng người")]
        [Range(1, 10, ErrorMessage = "Số lượng người đi cùng chỉ được phép từ 1 đến 10")]
        [Display(Name = "Số lượng người")]
        public int SoLuongNguoi { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập lý do")]
        [Display(Name = "Lý do vào công ty")]
        public string LyDo { get; set; }

        [Display(Name = "Thời gian hẹn")]
        public DateTime? ThoiGianHen { get; set; }
        // =========================================================
    }
}