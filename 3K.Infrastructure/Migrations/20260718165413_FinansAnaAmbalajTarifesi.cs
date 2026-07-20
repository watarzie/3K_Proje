using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansAnaAmbalajTarifesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FinansUrunleri"
                    ("Kod", "Ad", "FiyatlandirmaBirimi", "BirimFiyat", "ParaBirimi", "KdvOrani", "Aktif", "Sira", "CreatedDate")
                SELECT 'AMBALAJ-M3', 'Ana Ambalaj', 2, 561.00, 'EUR', 20.00, TRUE, 1, NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FinansUrunleri" WHERE "Kod" = 'AMBALAJ-M3'
                );

                INSERT INTO "FinansUrunEslesmeleri"
                    ("UrunId", "IsTuru", "SandikAdi", "Aktif", "CreatedDate")
                SELECT u."Id", 1, NULL, TRUE, NOW()
                FROM "FinansUrunleri" u
                WHERE u."Kod" = 'AMBALAJ-M3'
                  AND NOT EXISTS (
                      SELECT 1 FROM "FinansUrunEslesmeleri" e
                      WHERE e."IsTuru" = 1 AND e."SandikAdi" IS NULL
                  );

                UPDATE "FinansSiparisKalemleri" sk
                SET "UrunId" = u."Id",
                    "UrunKodu" = u."Kod",
                    "UrunAdi" = u."Ad",
                    "FiyatlandirmaBirimi" = 2,
                    "FiyatlandirmaMiktari" = sk."M3",
                    "BirimFiyat" = u."BirimFiyat",
                    "ParaBirimi" = u."ParaBirimi",
                    "KdvOrani" = u."KdvOrani",
                    "NetTutar" = ROUND(sk."M3" * u."BirimFiyat", 2),
                    "KdvTutari" = ROUND(ROUND(sk."M3" * u."BirimFiyat", 2) * u."KdvOrani" / 100, 2),
                    "ToplamTutar" = ROUND(sk."M3" * u."BirimFiyat", 2)
                        + ROUND(ROUND(sk."M3" * u."BirimFiyat", 2) * u."KdvOrani" / 100, 2),
                    "FiyatManuelDegistirildi" = FALSE
                FROM "FinansUrunleri" u, "FinansIsKayitlari" ik
                WHERE sk."IsKaydiId" = ik."Id"
                  AND ik."IsTuru" = 1
                  AND sk."BirimFiyat" = 0
                  AND u."Kod" = 'AMBALAJ-M3';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("""
                                DELETE FROM "FinansUrunEslesmeleri"
                                WHERE "UrunId" IN (SELECT "Id" FROM "FinansUrunleri" WHERE "Kod" = 'AMBALAJ-M3');

                                DELETE FROM "FinansUrunleri"
                                WHERE "Kod" = 'AMBALAJ-M3'
                                    AND NOT EXISTS (SELECT 1 FROM "FinansSiparisKalemleri" WHERE "UrunId" = "FinansUrunleri"."Id");
                                """);
        }
    }
}
