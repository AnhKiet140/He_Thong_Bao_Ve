using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class DonDepDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhachNgoais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CCCD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiCanGap = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachNgoais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LichSuCheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    KhachNgoaiId = table.Column<int>(type: "int", nullable: true),
                    TheXeId = table.Column<int>(type: "int", nullable: true),
                    ThoiGianCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HinhThucCheckIn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuCheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChucVu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhongBan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaVanTay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnhKhuonMat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaiKhoan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TheXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaRFID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    KhachNgoaiId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheXes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhachNgoais");

            migrationBuilder.DropTable(
                name: "LichSuCheckIns");

            migrationBuilder.DropTable(
                name: "NhanViens");

            migrationBuilder.DropTable(
                name: "TheXes");
        }
    }
}
