using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUckLokasyonSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(4454));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5167));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5265));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5266));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5168));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5170));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5171));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5172));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5173));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5174));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5175));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5176));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5177));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5267));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5268));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5470));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5808));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5809));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5809));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5809));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5811));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5812));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5812));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5813));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5814));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5814));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5815));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5815));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5815));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(5816));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(3955));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(3955));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 23, 4, 14, 845, DateTimeKind.Utc).AddTicks(3957));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LookupDepoLokasyonlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[] { 3, 2, "Üçk" });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(1351));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2064));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2162));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2164));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2066));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2067));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2068));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2069));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2070));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2071));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2072));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2073));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2075));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2173));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2174));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2376));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2716));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2716));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2717));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2717));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2718));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2718));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2719));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2719));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2720));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2721));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2721));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2722));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2722));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(2722));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(533));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(867));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(868));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 21, 59, 50, 775, DateTimeKind.Utc).AddTicks(869));
        }
    }
}
