using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansTarifeToplamYuvarlama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH toplamlar AS (
                    SELECT sk."SiparisId",
                           sk."UrunId",
                           MAX(sk."Id") AS "SonKalemId",
                           ROUND(ROUND(SUM(sk."M3"), 2) * MAX(sk."BirimFiyat"), 2) AS "HedefNet",
                           ROUND(ROUND(ROUND(SUM(sk."M3"), 2) * MAX(sk."BirimFiyat"), 2)
                               * MAX(sk."KdvOrani") / 100, 2) AS "HedefKdv",
                           SUM(sk."NetTutar") AS "MevcutNet",
                           SUM(sk."KdvTutari") AS "MevcutKdv"
                    FROM "FinansSiparisKalemleri" sk
                    INNER JOIN "FinansIsKayitlari" ik ON ik."Id" = sk."IsKaydiId"
                    WHERE ik."IsTuru" = 1 AND sk."UrunKodu" = 'AMBALAJ-M3'
                    GROUP BY sk."SiparisId", sk."UrunId"
                )
                UPDATE "FinansSiparisKalemleri" sk
                SET "NetTutar" = sk."NetTutar" + t."HedefNet" - t."MevcutNet",
                    "KdvTutari" = sk."KdvTutari" + t."HedefKdv" - t."MevcutKdv",
                    "ToplamTutar" = sk."NetTutar" + t."HedefNet" - t."MevcutNet"
                        + sk."KdvTutari" + t."HedefKdv" - t."MevcutKdv"
                FROM toplamlar t
                WHERE sk."Id" = t."SonKalemId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Financial snapshots are not reverted to avoid destroying corrected history.
        }
    }
}
