using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDynamicApprovalFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IslemKodu",
                table: "IslemOnayKurallari");

            migrationBuilder.DropColumn(
                name: "KuralAdi",
                table: "IslemOnayKurallari");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupYetkiTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupYetkiTipleri",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupYetkiTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupYetkiTipleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupUrunDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupUrunDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupUrunDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupUrunDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupUcKDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupUcKDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupUcKDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupUcKDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupStokDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupStokDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupStokDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupStokDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupSandikTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupSandikTipleri",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupSandikTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupSandikTipleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupSandikDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupSandikDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupSandikDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupSandikDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupProjeDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupProjeDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupProjeDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupProjeDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupIslemTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupIslemTipleri",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupIslemTipleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupIslemTipleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupGridDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupGridDurumlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupGridDurumlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupGridDurumlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupGeriGonderilmeSebepleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupGeriGonderilmeSebepleri",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupGeriGonderilmeSebepleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupGeriGonderilmeSebepleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LookupDepoLokasyonlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LookupDepoLokasyonlari",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "LookupDepoLokasyonlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LookupDepoLokasyonlari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LookupUcKDurumId",
                table: "IslemOnayKurallari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(4881), 8 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5140), 9 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5140), 10 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5141), 2 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5142), 3 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5142), 4 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5143), 11 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5144), 12 });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedDate", "LookupUcKDurumId" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(5144), 13 });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2299), null, null });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2300), null, null });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2301), null, null });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2301), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2986), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2987), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2988), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2640), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2641), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2642), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2643), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2643), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2644), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2645), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2645), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2646), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2646), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2647), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2648), null, null });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2658), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3417), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3418), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3419), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3420), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3420), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3421), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3421), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3422), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3423), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3423), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3424), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3425), null, null });

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3425), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8015), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8452), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8454), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8455), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8455), null, null });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 2, DateTimeKind.Utc).AddTicks(8456), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(1905), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(1907), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(1907), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(1908), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2121), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2123), null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2123), null, null });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3272), null, null });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3273), null, null });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3274), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2815), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2816), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2817), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2818), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2819), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2819), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2820), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2821), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2822), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2822), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2823), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2823), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2824), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2455), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2456), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2456), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2457), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2471), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2472), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2472), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2473), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2474), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2474), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2475), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2475), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2476), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2477), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2477), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2478), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2479), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2479), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2480), null, null });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(2480), null, null });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3128), null, null });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3129), null, null });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, new DateTime(2026, 4, 19, 21, 43, 18, 3, DateTimeKind.Utc).AddTicks(3130), null, null });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(1982));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2770));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2882));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2884));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2772));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2773));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2774));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2775));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2776));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2777));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2778));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2788));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2791));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2885));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2886));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(2887));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3095));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3434));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3435));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3435));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3435));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3437));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3437));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3437));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3438));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3438));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3439));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3439));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3440));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3440));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3440));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(3441));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(1170));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(1410));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(1411));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 43, 18, 4, DateTimeKind.Utc).AddTicks(1412));

            migrationBuilder.CreateIndex(
                name: "IX_IslemOnayKurallari_LookupUcKDurumId",
                table: "IslemOnayKurallari",
                column: "LookupUcKDurumId");

            migrationBuilder.AddForeignKey(
                name: "FK_IslemOnayKurallari_LookupUcKDurumlari_LookupUcKDurumId",
                table: "IslemOnayKurallari",
                column: "LookupUcKDurumId",
                principalTable: "LookupUcKDurumlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IslemOnayKurallari_LookupUcKDurumlari_LookupUcKDurumId",
                table: "IslemOnayKurallari");

            migrationBuilder.DropIndex(
                name: "IX_IslemOnayKurallari_LookupUcKDurumId",
                table: "IslemOnayKurallari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupYetkiTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupYetkiTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupYetkiTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupYetkiTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupUrunDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupUrunDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupUrunDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupUrunDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupUcKDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupUcKDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupUcKDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupUcKDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupStokDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupStokDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupStokDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupStokDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupSandikTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupSandikTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupSandikTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupSandikTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupSandikDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupSandikDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupSandikDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupSandikDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupProjeDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupProjeDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupProjeDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupProjeDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupIslemTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupIslemTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupIslemTipleri");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupIslemTipleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupGridDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupGridDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupGridDurumlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupGridDurumlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupDepoLokasyonlari");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LookupDepoLokasyonlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LookupDepoLokasyonlari");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LookupDepoLokasyonlari");

            migrationBuilder.DropColumn(
                name: "LookupUcKDurumId",
                table: "IslemOnayKurallari");

            migrationBuilder.AddColumn<string>(
                name: "IslemKodu",
                table: "IslemOnayKurallari",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KuralAdi",
                table: "IslemOnayKurallari",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8111), "UcKDurum_ProjedenKarsilandi", "Projeden Karşılandı" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8663), "UcKDurum_StoktanKarsilandi", "Stoktan Karşılandı" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8664), "UcKDurum_TedarikcidenGeldi", "Tedarikçiden Geldi" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8665), "UcKDurum_TamGeldi", "Tam Geldi" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8666), "UcKDurum_EksikGeldi", "Eksik Geldi" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8666), "UcKDurum_Gelmedi", "Gelmedi" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8667), "UcKDurum_TrafoSevk", "Trafo Sevk" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8668), "UcKDurum_Iptal", "İptal" });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedDate", "IslemKodu", "KuralAdi" },
                values: new object[] { new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8669), "UcKDurum_Sipariste", "Siparişte" });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4810));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5556));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5658));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5659));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5558));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5559));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5560));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5561));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5563));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5564));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5565));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5566));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5567));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5660));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5662));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5663));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5858));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6200));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6212));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6213));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6214));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6214));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6218));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4088));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4298));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4299));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4301));
        }
    }
}
