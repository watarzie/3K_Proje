using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LookupGuncelleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LookupUrunDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 14, 13, "GriddeHazir" },
                    { 15, 14, "GriddeEksik" },
                    { 16, 15, "Sipariste" },
                    { 17, 16, "Gelmedi" },
                    { 18, 17, "TrafoSevk" },
                    { 19, 18, "BaskaProyeVerildi" },
                    { 20, 19, "HataliUrun" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20);
        }
    }
}
