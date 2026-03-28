using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class TheXe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã Thẻ (UID)")]
        public string MaThe { get; set; } // VD: THE-001, THE-NV-05

        [Display(Name = "Loại Thẻ")]
        public string LoaiThe { get; set; } // VD: "Thẻ Nhân Viên", "Thẻ Khách"

        [Display(Name = "Người đang giữ")]
        public string NguoiGiu { get; set; } // Trống nếu thẻ đang nằm ở quầy

        // 0 = Sẵn sàng (Nằm ở quầy), 1 = Đang sử dụng, 2 = Khóa / Báo mất
        public int TrangThai { get; set; } = 0;

        public string GhiChu { get; set; }
    }
}