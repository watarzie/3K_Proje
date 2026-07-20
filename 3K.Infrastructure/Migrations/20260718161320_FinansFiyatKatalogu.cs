using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansFiyatKatalogu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansSiparisKalemleri",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "FiyatManuelDegistirildi",
                table: "FinansSiparisKalemleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FiyatlandirmaBirimi",
                table: "FinansSiparisKalemleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FiyatlandirmaMiktari",
                table: "FinansSiparisKalemleri",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvOrani",
                table: "FinansSiparisKalemleri",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvTutari",
                table: "FinansSiparisKalemleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetTutar",
                table: "FinansSiparisKalemleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "FinansSiparisKalemleri",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ToplamTutar",
                table: "FinansSiparisKalemleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UrunAdi",
                table: "FinansSiparisKalemleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UrunId",
                table: "FinansSiparisKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrunKodu",
                table: "FinansSiparisKalemleri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "FinansSiparisKalemleri" AS kalem
                SET "FiyatlandirmaBirimi" = CASE WHEN kalem."M3" > 0 THEN 2 ELSE 1 END,
                    "FiyatlandirmaMiktari" = CASE WHEN kalem."M3" > 0 THEN kalem."M3" ELSE kalem."Adet" END,
                    "ParaBirimi" = 'EUR',
                    "UrunKodu" = kaynak."SandikNo",
                    "UrunAdi" = kaynak."SandikAdi"
                FROM "FinansIsKayitlari" AS kaynak
                WHERE kaynak."Id" = kalem."IsKaydiId";
                """);

            migrationBuilder.CreateTable(
                name: "FinansUrunleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FiyatlandirmaBirimi = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansUrunleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinansUrunEslesmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UrunId = table.Column<int>(type: "integer", nullable: false),
                    IsTuru = table.Column<int>(type: "integer", nullable: false),
                    SandikAdi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansUrunEslesmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansUrunEslesmeleri_FinansUrunleri_UrunId",
                        column: x => x.UrunId,
                        principalTable: "FinansUrunleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisKalemleri_UrunId",
                table: "FinansSiparisKalemleri",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansUrunEslesmeleri_IsTuru_SandikAdi",
                table: "FinansUrunEslesmeleri",
                columns: new[] { "IsTuru", "SandikAdi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansUrunEslesmeleri_UrunId",
                table: "FinansUrunEslesmeleri",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansUrunleri_Kod",
                table: "FinansUrunleri",
                column: "Kod",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinansSiparisKalemleri_FinansUrunleri_UrunId",
                table: "FinansSiparisKalemleri",
                column: "UrunId",
                principalTable: "FinansUrunleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinansSiparisKalemleri_FinansUrunleri_UrunId",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropTable(
                name: "FinansUrunEslesmeleri");

            migrationBuilder.DropTable(
                name: "FinansUrunleri");

            migrationBuilder.DropIndex(
                name: "IX_FinansSiparisKalemleri_UrunId",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "BirimFiyat",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "FiyatManuelDegistirildi",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "FiyatlandirmaBirimi",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "FiyatlandirmaMiktari",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "KdvOrani",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "KdvTutari",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "NetTutar",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "ToplamTutar",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "UrunAdi",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "UrunId",
                table: "FinansSiparisKalemleri");

            migrationBuilder.DropColumn(
                name: "UrunKodu",
                table: "FinansSiparisKalemleri");
        }
    }
}
