using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatDKKhac2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoaiXe",
                table: "KhachNgoais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LyDo",
                table: "KhachNgoais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NhanVienCanGap",
                table: "KhachNgoais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SoLuongNguoi",
                table: "KhachNgoais",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianHen",
                table: "KhachNgoais",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoaiXe",
                table: "KhachNgoais");

            migrationBuilder.DropColumn(
                name: "LyDo",
                table: "KhachNgoais");

            migrationBuilder.DropColumn(
                name: "NhanVienCanGap",
                table: "KhachNgoais");

            migrationBuilder.DropColumn(
                name: "SoLuongNguoi",
                table: "KhachNgoais");

            migrationBuilder.DropColumn(
                name: "ThoiGianHen",
                table: "KhachNgoais");
        }
    }
}
