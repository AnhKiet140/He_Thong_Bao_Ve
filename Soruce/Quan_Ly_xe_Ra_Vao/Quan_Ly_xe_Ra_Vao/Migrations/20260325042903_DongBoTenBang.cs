using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class DongBoTenBang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuRaVaos",
                table: "LichSuRaVaos");

            migrationBuilder.RenameTable(
                name: "LichSuRaVaos",
                newName: "LichSuCheckIns");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuCheckIns",
                table: "LichSuCheckIns",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuCheckIns",
                table: "LichSuCheckIns");

            migrationBuilder.RenameTable(
                name: "LichSuCheckIns",
                newName: "LichSuRaVaos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuRaVaos",
                table: "LichSuRaVaos",
                column: "Id");
        }
    }
}
