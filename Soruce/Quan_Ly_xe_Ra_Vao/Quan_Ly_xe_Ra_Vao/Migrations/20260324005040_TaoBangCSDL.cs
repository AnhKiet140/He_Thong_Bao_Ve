using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class TaoBangCSDL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TheXes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuCheckIns",
                table: "LichSuCheckIns");

            migrationBuilder.DropColumn(
                name: "AnhKhuonMat",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "MaVanTay",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "MatKhau",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "PhongBan",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "KhachNgoaiId",
                table: "LichSuCheckIns");

            migrationBuilder.DropColumn(
                name: "NhanVienId",
                table: "LichSuCheckIns");

            migrationBuilder.DropColumn(
                name: "TheXeId",
                table: "LichSuCheckIns");

            migrationBuilder.DropColumn(
                name: "ThoiGianCheckOut",
                table: "LichSuCheckIns");

            migrationBuilder.RenameTable(
                name: "LichSuCheckIns",
                newName: "LichSuRaVaos");

            migrationBuilder.RenameColumn(
                name: "VaiTro",
                table: "NhanViens",
                newName: "MaNV");

            migrationBuilder.RenameColumn(
                name: "TaiKhoan",
                table: "NhanViens",
                newName: "FaceDataPath");

            migrationBuilder.RenameColumn(
                name: "NguoiCanGap",
                table: "KhachNgoais",
                newName: "PhongBanCanGap");

            migrationBuilder.RenameColumn(
                name: "LyDo",
                table: "KhachNgoais",
                newName: "FaceDataPath");

            migrationBuilder.RenameColumn(
                name: "ThoiGianCheckIn",
                table: "LichSuRaVaos",
                newName: "ThoiGian");

            migrationBuilder.RenameColumn(
                name: "HinhThucCheckIn",
                table: "LichSuRaVaos",
                newName: "PhuongThuc");

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "HasFingerprint",
                table: "NhanViens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "KhachNgoais",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "BienSoXe",
                table: "KhachNgoais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "BienSoXe",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoaiDoiTuong",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuRaVaos",
                table: "LichSuRaVaos",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuRaVaos",
                table: "LichSuRaVaos");

            migrationBuilder.DropColumn(
                name: "HasFingerprint",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "BienSoXe",
                table: "KhachNgoais");

            migrationBuilder.DropColumn(
                name: "BienSoXe",
                table: "LichSuRaVaos");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "LichSuRaVaos");

            migrationBuilder.DropColumn(
                name: "LoaiDoiTuong",
                table: "LichSuRaVaos");

            migrationBuilder.RenameTable(
                name: "LichSuRaVaos",
                newName: "LichSuCheckIns");

            migrationBuilder.RenameColumn(
                name: "MaNV",
                table: "NhanViens",
                newName: "VaiTro");

            migrationBuilder.RenameColumn(
                name: "FaceDataPath",
                table: "NhanViens",
                newName: "TaiKhoan");

            migrationBuilder.RenameColumn(
                name: "PhongBanCanGap",
                table: "KhachNgoais",
                newName: "NguoiCanGap");

            migrationBuilder.RenameColumn(
                name: "FaceDataPath",
                table: "KhachNgoais",
                newName: "LyDo");

            migrationBuilder.RenameColumn(
                name: "ThoiGian",
                table: "LichSuCheckIns",
                newName: "ThoiGianCheckIn");

            migrationBuilder.RenameColumn(
                name: "PhuongThuc",
                table: "LichSuCheckIns",
                newName: "HinhThucCheckIn");

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "NhanViens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AnhKhuonMat",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaVanTay",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatKhau",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhongBan",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "KhachNgoais",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "TrangThai",
                table: "LichSuCheckIns",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "KhachNgoaiId",
                table: "LichSuCheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NhanVienId",
                table: "LichSuCheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TheXeId",
                table: "LichSuCheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianCheckOut",
                table: "LichSuCheckIns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuCheckIns",
                table: "LichSuCheckIns",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TheXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhachNgoaiId = table.Column<int>(type: "int", nullable: true),
                    MaRFID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheXes", x => x.Id);
                });
        }
    }
}
