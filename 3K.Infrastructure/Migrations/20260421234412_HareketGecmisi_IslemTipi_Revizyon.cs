using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HareketGecmisi_IslemTipi_Revizyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IslemTipiId",
                table: "HareketGecmisleri",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(3896));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4204));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4205));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4206));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4206));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4207));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4208));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4208));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(4209));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(687));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(688));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(689));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(689));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1696));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1697));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1698));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1139));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1140));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1141));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1142));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1142));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1143));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1144));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1144));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1145));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1145));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1146));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1147));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1147));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1316));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1317));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1317));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 1, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2210), "Çeki Yüklendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 2, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2212), "Proje Oluşturuldu" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 3, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2212), "Grid Durumu Güncellendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 4, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2213), "Grid Toplu Sevk Edildi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 5, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2214), "3K Durumu Güncellendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 6, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2214), "3K Teslim Alındı" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 7, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2215), "3K Toplu Teslim Alındı" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 8, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2216), "Sandığa Manuel Ürün Eklendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 9, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2216), "Sandıklar Arası Ürün Taşındı" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 10, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2217), "Sandık İçi Ürün Güncellendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 11, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2218), "Ürün İptal Edildi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 12, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2218), "Stoktan Karşılandı" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 13, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2219), "FB'den Karşılandı" });

            migrationBuilder.InsertData(
                table: "LookupIslemTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 14, 14, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2219), "Sandık Hazırlandı / Kapatıldı", null, null },
                    { 15, 15, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2220), "Toplu Sandık Kapatıldı", null, null },
                    { 16, 16, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2221), "Fiili Sandık Değiştirildi", null, null },
                    { 17, 17, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2221), "Sandık Lokasyonu Güncellendi", null, null },
                    { 18, 18, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2222), "Sandık Otomatik Hazırlandı", null, null },
                    { 19, 19, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2223), "Excel İndirildi", null, null },
                    { 20, 20, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2223), "PDF İndirildi", null, null },
                    { 21, 21, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2224), "Sandık Oluşturuldu", null, null },
                    { 22, 22, null, new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2224), "Kullanıcı Oluşturuldu", null, null }
                });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(5964));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(6435));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(6436));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(6437));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(6437));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 861, DateTimeKind.Utc).AddTicks(6438));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(206));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(208));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(209));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(210));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(463));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(476));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(477));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2051));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2053));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(2054));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1482));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1483));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1484));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1484));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1485));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1486));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1486));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1487));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1487));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1488));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1489));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1498));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1498));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(891));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(892));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(893));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(894));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(894));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(895));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(896));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(896));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(897));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(899));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(900));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(900));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(901));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(901));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(902));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(903));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(903));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(904));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1895));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1897));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(1897));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(178));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(954));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1057));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1059));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(956));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(957));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(959));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(960));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(961));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(962));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(963));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(964));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(965));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1060));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1061));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1062));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1328));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1681));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1682));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1682));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1682));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1683));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1684));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1684));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1717));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1718));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1719));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1719));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1719));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1720));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1720));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 863, DateTimeKind.Utc).AddTicks(1720));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(9452));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(9670));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(9671));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 21, 23, 44, 11, 862, DateTimeKind.Utc).AddTicks(9673));

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_IslemTipiId",
                table: "HareketGecmisleri",
                column: "IslemTipiId");

            migrationBuilder.AddForeignKey(
                name: "FK_HareketGecmisleri_LookupIslemTipleri_IslemTipiId",
                table: "HareketGecmisleri",
                column: "IslemTipiId",
                principalTable: "LookupIslemTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HareketGecmisleri_LookupIslemTipleri_IslemTipiId",
                table: "HareketGecmisleri");

            migrationBuilder.DropIndex(
                name: "IX_HareketGecmisleri_IslemTipiId",
                table: "HareketGecmisleri");

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DropColumn(
                name: "IslemTipiId",
                table: "HareketGecmisleri");

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
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 0, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8516), "CekiYuklendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 1, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8518), "SandikOlusturuldu" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 2, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8518), "SandikBolundu" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 3, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8519), "SandikDegisti" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 4, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8520), "UrunTasindi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 5, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8521), "FBTransferi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 6, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8521), "StokKullanimi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 7, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8522), "EksikKapatildi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 8, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8523), "PDFAlindi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 9, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8524), "MailGonderildi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 10, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8524), "UrunGuncellendi" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 11, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8525), "KullaniciOlusturuldu" });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate", "Deger" },
                values: new object[] { 12, new DateTime(2026, 4, 21, 22, 13, 36, 387, DateTimeKind.Utc).AddTicks(8526), "ProjeOlusturuldu" });

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
    }
}
