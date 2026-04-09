using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class TheXe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã Thẻ (UID)")]
        public string MaThe { get; set; }

        [Display(Name = "Loại Thẻ")]
        public string LoaiThe { get; set; }

        [Display(Name = "Người đang giữ")]
        public string NguoiGiu { get; set; }

        // 0 = Sẵn sàng, 1 = Đang sử dụng, 2 = Khóa / Báo mất
        public int TrangThai { get; set; } = 0;

        public string GhiChu { get; set; }

        // THÊM DÒNG NÀY ĐỂ LƯU CHUỖI ẢNH TỪ CAMERA
        [Display(Name = "Hình Ảnh Nhận Diện")]
        public string? HinhAnh { get; set; }
    }
}