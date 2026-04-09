using Microsoft.EntityFrameworkCore;
using Quan_Ly_xe_Ra_Vao.Models; // Khai báo đường dẫn tới thư mục Models

namespace Quan_Ly_xe_Ra_Vao.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- THÊM 3 DÒNG NÀY VÀO ---
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<KhachNgoai> KhachNgoais { get; set; }
        public DbSet<LichSuCheckIn> LichSuCheckIns { get; set; }

        public DbSet<TheXe> TheXes { get; set; }
        public DbSet<DangKyKhach> DangKyKhachs { get; set; }
        public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }
    }
}