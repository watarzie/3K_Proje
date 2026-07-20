using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansDuzenliIsTarifeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansOzelIsleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvOrani",
                table: "FinansOzelIsleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "FinansOzelIsleri",
                type: "text",
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansDuzenliIsleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvOrani",
                table: "FinansDuzenliIsleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "FinansDuzenliIsleri",
                type: "text",
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.Sql("""
                INSERT INTO "FinansDuzenliIsleri"
                    ("IsAdi", "IsTuru", "Musteri", "Aciklama", "TekrarSikligi", "BaslangicTarihi", "OlusturmaGunu",
                     "Miktar", "Birim", "BirimFiyat", "ParaBirimi", "KdvOrani", "Aktif", "CreatedDate")
                SELECT v."IsAdi", v."IsTuru", 'GE Vernova', v."Aciklama", 'Aylık', DATE '2026-07-01', 1,
                       1, v."Birim", v."BirimFiyat", v."ParaBirimi", 20, TRUE, CURRENT_TIMESTAMP
                FROM (VALUES
                    ('3K Derince Kira', 'Kira', '3K Derince kira bedeli', 'Ay', 513000::numeric, 'TRY'),
                    ('3K Seymen Kira', 'Kira', '3K Seymen kira bedeli', 'Ay', 360000::numeric, 'TRY'),
                    ('Sevkiyat', 'Sevkiyat', 'Sevkiyat hizmet bedeli', 'Sefer', 108000::numeric, 'EUR'),
                    ('SKIT', 'SKIT', 'SKIT hizmet bedeli', 'Hizmet', 215::numeric, 'EUR'),
                    ('Haliade-X', 'Haliade-X', 'Haliade-X hizmet bedeli', 'Hizmet', 812::numeric, 'EUR')
                ) AS v("IsAdi", "IsTuru", "Aciklama", "Birim", "BirimFiyat", "ParaBirimi")
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FinansDuzenliIsleri" d WHERE LOWER(d."IsAdi") = LOWER(v."IsAdi")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirimFiyat",
                table: "FinansOzelIsleri");

            migrationBuilder.DropColumn(
                name: "KdvOrani",
                table: "FinansOzelIsleri");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "FinansOzelIsleri");

            migrationBuilder.DropColumn(
                name: "BirimFiyat",
                table: "FinansDuzenliIsleri");

            migrationBuilder.DropColumn(
                name: "KdvOrani",
                table: "FinansDuzenliIsleri");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "FinansDuzenliIsleri");
        }
    }
}
