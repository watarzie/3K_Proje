using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBagimsizAmbalajSandiklari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmbalajBagimsizSandiklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tur = table.Column<int>(type: "integer", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    UretimeAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    FirinPartiNo = table.Column<string>(type: "text", nullable: true),
                    SandikNo = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    SandikTipi = table.Column<string>(type: "text", nullable: false),
                    Adet = table.Column<int>(type: "integer", nullable: false),
                    Boy = table.Column<decimal>(type: "numeric", nullable: false),
                    En = table.Column<decimal>(type: "numeric", nullable: false),
                    Yukseklik = table.Column<decimal>(type: "numeric", nullable: false),
                    KullanimAmaci = table.Column<string>(type: "text", nullable: true),
                    TalimatVeren = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbalajBagimsizSandiklar", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO "AmbalajBagimsizSandiklar"
                    ("Tur", "DurumId", "UretimeAlindi", "FirinPartiNo", "SandikNo", "Ad", "SandikTipi",
                     "Adet", "Boy", "En", "Yukseklik", "KullanimAmaci", "TalimatVeren", "Aciklama",
                     "CreatedDate", "UpdatedDate", "CreatedBy", "UpdatedBy")
                SELECT k."Tur",
                       CASE WHEN k."Tur" = 2 THEN p."IlaveSandiklarDurumId" ELSE p."IcSandiklarDurumId" END,
                       k."UretimeAlindi",
                       CASE WHEN k."Tur" = 2 THEN p."IlaveFirinPartiNo" ELSE p."IcSandikFirinPartiNo" END,
                       k."SandikNo", COALESCE(k."Ad", k."SandikNo"), k."SandikTipi", k."Adet",
                       k."Boy", k."En", k."Yukseklik", k."KullanimAmaci", k."TalimatVeren", k."Aciklama",
                       k."CreatedDate", k."UpdatedDate", k."CreatedBy", k."UpdatedBy"
                FROM "AmbalajUretimKalemleri" k
                INNER JOIN "AmbalajUretimPlanlari" p ON p."Id" = k."AmbalajUretimPlaniId"
                WHERE k."KaynakSandikId" IS NULL AND k."Tur" IN (2, 3);
                """);

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3379));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3710));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3713));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3714));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3715));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3716));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3717));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3718));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3719));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(3720));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(841));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(843));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(844));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(845));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(847));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(850));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(851));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(852));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8708));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8710));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8711));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8712));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9856));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9860));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9861));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9862));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9092));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9103));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9104));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9105));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9107));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9108));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9109));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9117));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9118));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9119));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9120));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9121));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9122));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9330));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9332));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9333));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9334));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(431));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(433));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(434));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(435));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(436));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(437));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(438));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(440));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(441));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(442));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(443));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(444));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(455));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(457));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(458));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(459));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(460));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(461));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(462));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(463));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(464));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(465));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(466));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(467));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(468));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(469));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(470));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(471));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(472));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(473));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(474));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(475));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(476));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(477));

            migrationBuilder.UpdateData(
                table: "LookupKaliteDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1024));

            migrationBuilder.UpdateData(
                table: "LookupKaliteDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1026));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(2666));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(4158));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(4166));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(4167));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(4168));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(671));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(673));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(674));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8260));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8271));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8272));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8273));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8504));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8519));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(257));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(259));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(260));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1192));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1194));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1195));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1196));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1197));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(1198));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9528));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9530));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9531));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9532));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9534));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9535));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9536));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9537));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9538));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9539));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9540));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9549));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9550));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(9552));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8886));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8888));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8889));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8891));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8892));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8893));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8894));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8895));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8896));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8897));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8899));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8900));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8901));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8902));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8903));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8904));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8905));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8905));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8907));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8908));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 934, DateTimeKind.Unspecified).AddTicks(8909));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(82));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(84));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 935, DateTimeKind.Unspecified).AddTicks(85));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5880));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6740));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6864));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6867));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6752));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6754));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6756));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6759));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6761));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6762));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6869));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6870));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6883));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6892));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6903));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6907));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6916));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6918));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6919));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6885));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6921));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6923));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6924));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6890));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6893));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6895));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6896));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6898));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6900));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6901));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 35,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6926));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 36,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6927));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 37,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6929));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 38,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6931));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 39,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6942));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 40,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 41,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6941));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 42,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 43,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6906));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 44,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6886));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 45,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6888));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 46,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6758));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(6904));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7297));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7664));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7665));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7666));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7667));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7669));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7670));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7671));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7672));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7673));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7674));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7674));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7675));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7676));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7676));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7677));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7677));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7679));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7679));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7680));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7683));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7684));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7684));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7685));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7686));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7686));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7687));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7687));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7688));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7689));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 35,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7690));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 36,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7691));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 37,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7691));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 38,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7692));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 39,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7692));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 40,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7693));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 41,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7693));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 42,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(7695));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5029));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5291));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5294));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5295));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5296));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 22, 13, 11, 936, DateTimeKind.Unspecified).AddTicks(5297));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmbalajBagimsizSandiklar");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1437));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1769));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1771));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1772));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1773));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1774));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1775));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 261, DateTimeKind.Unspecified).AddTicks(1778));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8995));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8997));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8999));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9000));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9001));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9002));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9003));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9004));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9005));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9006));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6971));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6973));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6975));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6976));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8003));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8005));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8006));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8007));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7371));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7382));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7383));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7385));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7386));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7387));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7388));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7389));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7390));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7391));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7392));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7393));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7394));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7587));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7589));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7591));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7592));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8565));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8567));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8568));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8569));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8570));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8571));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8572));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8574));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8575));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8576));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8577));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8578));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8588));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8589));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8590));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8592));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8593));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8594));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8595));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8596));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8597));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8598));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8599));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8600));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8601));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8602));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8603));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8604));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8605));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8606));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8608));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8609));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8610));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8611));

            migrationBuilder.UpdateData(
                table: "LookupKaliteDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9181));

            migrationBuilder.UpdateData(
                table: "LookupKaliteDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9183));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(552));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(2062));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(2070));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(2072));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(2073));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(2074));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8817));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8819));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8820));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6494));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6504));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6506));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6507));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6758));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(6772));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8371));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8374));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8375));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9353));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9355));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9356));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9357));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9358));

            migrationBuilder.UpdateData(
                table: "LookupSurecDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(9359));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7771));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7773));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7774));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7775));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7776));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7777));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7778));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7779));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7780));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7781));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7782));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7793));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7795));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7155));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7157));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7158));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7159));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7160));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7161));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7162));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7163));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7165));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7166));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7167));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7168));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7169));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7170));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7171));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7172));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7173));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7174));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7175));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7176));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(7177));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8186));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8188));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 260, DateTimeKind.Unspecified).AddTicks(8189));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(4213));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5057));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5198));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5201));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5061));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5063));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5064));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5077));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5079));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5080));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5203));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5204));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5221));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5230));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5241));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5245));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5254));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5256));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5258));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5223));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5259));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5261));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5263));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5228));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5231));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5233));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5234));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5236));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5238));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5239));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 35,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5264));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 36,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5266));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 37,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5267));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 38,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5269));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 39,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5280));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 40,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5272));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 41,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5274));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 42,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5270));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 43,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5244));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 44,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5225));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 45,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5227));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 46,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5075));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5242));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(5645));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6015));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6017));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6018));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6019));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6021));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6022));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6022));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6023));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6024));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6025));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6026));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6026));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6028));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6028));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6029));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6030));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6030));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6031));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6032));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6032));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6033));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6034));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6034));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6035));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6035));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6036));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6036));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6037));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6038));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6038));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6039));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 34,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6040));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 35,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6041));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 36,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6041));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 37,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6042));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 38,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6043));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 39,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6043));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 40,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6044));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 41,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6044));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 42,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6045));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(6046));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3268));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3575));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3578));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3579));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3581));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 7, 13, 21, 41, 22, 262, DateTimeKind.Unspecified).AddTicks(3582));
        }
    }
}
