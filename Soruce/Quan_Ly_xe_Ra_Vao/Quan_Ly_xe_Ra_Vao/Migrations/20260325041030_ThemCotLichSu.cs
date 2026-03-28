using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class ThemCotLichSu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HinhAnh",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Huong",
                table: "LichSuRaVaos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HinhAnh",
                table: "LichSuRaVaos");

            migrationBuilder.DropColumn(
                name: "Huong",
                table: "LichSuRaVaos");
        }
    }
}
