using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansGiderKategorileriSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FinansGiderKategorileri" ("Ad", "Aktif", "CreatedDate", "CreatedBy")
                SELECT kategori."Ad", TRUE, CURRENT_TIMESTAMP, 'Sistem'
                FROM (VALUES
                    ('Hammadde ve Malzeme'),
                    ('Nakliye ve Lojistik'),
                    ('Personel'),
                    ('Kira'),
                    ('Elektrik, Su ve Doğalgaz'),
                    ('Bakım ve Onarım'),
                    ('Araç ve Yakıt'),
                    ('Makine ve Ekipman'),
                    ('Dışarıdan Alınan Hizmet'),
                    ('Ofis ve İdari Gider'),
                    ('Vergi, Harç ve Sigorta'),
                    ('Yemek ve Konaklama'),
                    ('Diğer')
                ) AS kategori("Ad")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "FinansGiderKategorileri" AS mevcut
                    WHERE LOWER(TRIM(mevcut."Ad")) = LOWER(TRIM(kategori."Ad"))
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FinansGiderKategorileri" AS kategori
                WHERE kategori."Ad" IN (
                    'Hammadde ve Malzeme',
                    'Nakliye ve Lojistik',
                    'Personel',
                    'Kira',
                    'Elektrik, Su ve Doğalgaz',
                    'Bakım ve Onarım',
                    'Araç ve Yakıt',
                    'Makine ve Ekipman',
                    'Dışarıdan Alınan Hizmet',
                    'Ofis ve İdari Gider',
                    'Vergi, Harç ve Sigorta',
                    'Yemek ve Konaklama',
                    'Diğer'
                )
                AND kategori."CreatedBy" = 'Sistem'
                AND NOT EXISTS (
                    SELECT 1
                    FROM "FinansGiderleri" AS gider
                    WHERE gider."KategoriId" = kategori."Id"
                );
                """);
        }
    }
}
