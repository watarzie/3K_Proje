using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GridVe3KMenuTanimlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3080));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3843));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3935));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3844));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3846));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3847));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3848));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3849));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3850));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3851));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3852));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3853));

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 14, null, new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3938), "", "grid-modulu", "MENU.GRID_MODULU", 2, null, 3, null, null },
                    { 15, null, new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3939), "", "3k-modulu", "MENU.3K_MODULU", 2, null, 4, null, null }
                });

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4148));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4499));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4500));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4501));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4501));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4503));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4512));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2258));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2590));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2591));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2592));

            migrationBuilder.InsertData(
                table: "RolYetkileri",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "MenuTanimiId", "RolId", "UpdatedBy", "UpdatedDate", "YetkiTipi" },
                values: new object[,]
                {
                    { 14, null, new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514), 14, 1, null, null, "W" },
                    { 15, null, new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4515), 15, 1, null, null, "W" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2704));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3458));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3544));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3545));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3459));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3460));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3462));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3463));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3464));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3465));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3466));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3467));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3468));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3754));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4103));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4103));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4106));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4108));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(1859));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2238));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2239));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2240));
        }
    }
}
