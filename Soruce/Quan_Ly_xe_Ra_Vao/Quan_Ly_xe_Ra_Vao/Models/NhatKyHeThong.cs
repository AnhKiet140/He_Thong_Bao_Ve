using System;
using System.ComponentModel.DataAnnotations;

namespace Quan_Ly_xe_Ra_Vao.Models
{
    public class NhatKyHeThong
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Thời gian")]
        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [Display(Name = "Mức độ")]
        [MaxLength(50)]
        public string MucDo { get; set; } // Ví dụ: "Thông tin", "Cảnh báo", "Nguy hiểm"

        [Display(Name = "Người thực hiện")]
        [MaxLength(100)]
        public string NguoiThucHien { get; set; }

        [Display(Name = "Phân hệ")]
        [MaxLength(100)]
        public string PhanHe { get; set; } // Nơi xảy ra sự kiện (VD: "Quản Lý Thẻ Cứng")

        [Display(Name = "Chi tiết thao tác")]
        public string ChiTietThaoTac { get; set; }
    }
}