using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class NhanVien
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã Nhân Viên")]
        public string MaNV { get; set; } // VD: NV-001

        [Required]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; }

        [Display(Name = "Chức vụ")]
        public string ChucVu { get; set; }

        // Lưu tên file ảnh khuôn mặt (VD: nguyen-van-a.jpg)
        public string FaceDataPath { get; set; }

        // Trạng thái: true (đã lấy vân tay), false (chưa lấy)
        public bool HasFingerprint { get; set; }

        // CA LÀM VIỆC & CHẤM CÔNG
        [Display(Name = "Giờ vào ca")]
        public TimeSpan GioVaoCa { get; set; } = new TimeSpan(8, 0, 0); // Mặc định 8h00 sáng

        [Display(Name = "Giờ xin đi muộn")]
        public TimeSpan? GioXinDiMuon { get; set; } // Nullable: Có thể trống. Nếu có, AI sẽ dùng giờ này để chấm công.
    }
}