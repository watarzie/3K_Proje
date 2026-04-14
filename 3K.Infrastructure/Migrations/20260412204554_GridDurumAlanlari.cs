using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GridDurumAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GridGelenAdet",
                table: "CekiSatirlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GridSevkDurumu",
                table: "CekiSatirlari",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TrafoSevkAdet",
                table: "CekiSatirlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GridGelenAdet",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "GridSevkDurumu",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "TrafoSevkAdet",
                table: "CekiSatirlari");
        }
    }
}
