using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansDonemVeIsTuruButunlugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinansOzelIsleri_DuzenliIsId",
                table: "FinansOzelIsleri");

            migrationBuilder.AddColumn<string>(
                name: "DonemAnahtari",
                table: "FinansOzelIsleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptalAciklamasi",
                table: "FinansOzelIsleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IptalEdildi",
                table: "FinansOzelIsleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IptalTarihi",
                table: "FinansOzelIsleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptalAciklamasi",
                table: "FinansGiderleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IptalEdildi",
                table: "FinansGiderleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IptalTarihi",
                table: "FinansGiderleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvTutari",
                table: "FinansGiderleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Matrah",
                table: "FinansGiderleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "FinansDuzenliIsleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Miktar",
                table: "FinansDuzenliIsleri",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProjeId",
                table: "FinansDuzenliIsleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DosyaUzantisi",
                table: "FinansBelgeleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SaklananDosyaAdi",
                table: "FinansBelgeleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "YukleyenKullanici",
                table: "FinansBelgeleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FinansIsTuruTanimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansIsTuruTanimlari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinansOzelIsleri_DuzenliIsId_DonemAnahtari",
                table: "FinansOzelIsleri",
                columns: new[] { "DuzenliIsId", "DonemAnahtari" },
                unique: true,
                filter: "\"DuzenliIsId\" IS NOT NULL AND \"DonemAnahtari\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinansDuzenliIsleri_ProjeId",
                table: "FinansDuzenliIsleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansIsTuruTanimlari_Ad",
                table: "FinansIsTuruTanimlari",
                column: "Ad",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinansDuzenliIsleri_Projeler_ProjeId",
                table: "FinansDuzenliIsleri",
                column: "ProjeId",
                principalTable: "Projeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Finans verisini korumak için bu migration otomatik geri alınamaz.
        }
    }
}
