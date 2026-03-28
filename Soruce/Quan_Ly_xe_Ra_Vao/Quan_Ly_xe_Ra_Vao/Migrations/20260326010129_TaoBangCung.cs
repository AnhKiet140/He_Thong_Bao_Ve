using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_Ly_xe_Ra_Vao.Migrations
{
    /// <inheritdoc />
    public partial class TaoBangCung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TheXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiThe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiGiu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "TheXes");
        }
    }
}
