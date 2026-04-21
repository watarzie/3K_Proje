using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IsKurallariBatch1_RemoveRevizyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Revizyonlar");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(112));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(394));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(395));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(396));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(397));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(398));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(398));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(399));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(400));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7042));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7045));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7045));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7046));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8040));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8042));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8042));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7451));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7454));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7454));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7455));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7456));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7457));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7458));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7458));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7459));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7460));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7460));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7461));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7462));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7648));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7650));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7650));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8516));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8518));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8518));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8519));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8520));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8521));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8521));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8522));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8523));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8524));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8524));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8525));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8526));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2423));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2860));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2862));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2863));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2863));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(2864));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6603));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6606));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6607));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6608));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6842));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6844));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(6845));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8360));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8362));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8363));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7827));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7829));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7830));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7831));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7832));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7832));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7833));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7834));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7835));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7835));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7836));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7837));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7838));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7248));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7250));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7251));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7252));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7253));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7262));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7263));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7264));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7265));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7266));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7267));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7267));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7268));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7269));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7270));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7270));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7271));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7272));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7273));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(7273));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8204));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8206));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8207));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6034));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6794));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6929));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6931));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6796));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6797));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6799));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6800));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6801));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6802));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6803));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6839));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6840));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6932));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6934));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(6935));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7133));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7473));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7474));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7474));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7475));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7477));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7478));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7478));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7478));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7479));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7479));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7480));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7480));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7480));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7481));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(7481));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(5346));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(5555));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(5557));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 22, 13, 36, 388, DateTimeKind.Utc).AddTicks(5558));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Revizyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EskiDeger = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    YeniDeger = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revizyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Revizyonlar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revizyonlar_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3022));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3286));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3287));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3288));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3288));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3289));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3289));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3290));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(3291));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(156));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(157));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(158));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(159));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1023));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1024));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1025));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(545));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(546));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(547));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(548));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(548));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(549));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(549));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(550));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(551));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(551));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(552));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(553));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(553));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(711));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(712));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(713));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1454));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1455));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1455));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1456));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1457));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1457));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1458));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1459));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1459));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1460));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1492));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1493));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1494));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(5975));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(6388));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(6389));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(6389));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(6390));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(6391));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9730));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9732));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9733));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9746));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9976));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9978));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 26, DateTimeKind.Utc).AddTicks(9978));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1310));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1312));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1313));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(857));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(859));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(859));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(860));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(861));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(861));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(862));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(862));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(871));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(872));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(873));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(873));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(874));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(311));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(312));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(313));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(313));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(314));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(315));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(315));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(316));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(316));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(317));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(370));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(371));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(372));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(372));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(373));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(374));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(374));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(375));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(384));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(385));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1168));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1170));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(1170));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(8946));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9650));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9741));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9743));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9651));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9653));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9654));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9655));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9656));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9657));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9658));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9659));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9660));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9744));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9745));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9746));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(9942));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(260));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(261));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(261));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(261));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(263));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(263));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(263));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(264));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(301));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(302));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(302));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(302));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(302));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(303));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 28, DateTimeKind.Utc).AddTicks(303));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(8278));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(8482));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(8483));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 8, 55, 30, 27, DateTimeKind.Utc).AddTicks(8484));

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_KullaniciId",
                table: "Revizyonlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_ProjeId",
                table: "Revizyonlar",
                column: "ProjeId");
        }
    }
}
