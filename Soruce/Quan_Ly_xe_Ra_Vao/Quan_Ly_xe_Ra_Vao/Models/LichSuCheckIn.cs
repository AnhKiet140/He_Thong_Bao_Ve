using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class LichSuCheckIn
    {
        [Key]
        public int Id { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        public string LoaiDoiTuong { get; set; } // VD: Nhân sự, Khách ngoài, Người lạ

        public string HoTen { get; set; }

        public string BienSoXe { get; set; }

        public string PhuongThuc { get; set; } // VD: FaceID, Vân Tay, Quẹt thẻ, Cảnh báo

        public string TrangThai { get; set; } // VD: Thành Công, Không hợp lệ

        // --- 2 CỘT MỚI THÊM VÀO ---
        public string Huong { get; set; } // VD: Đi Vào, Đi Ra
        public string HinhAnh { get; set; } // Lưu ảnh Base64 (đặc biệt hữu ích để bắt quả tang người lạ)
    }
}