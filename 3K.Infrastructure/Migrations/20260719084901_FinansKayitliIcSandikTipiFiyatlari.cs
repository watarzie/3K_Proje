using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansKayitliIcSandikTipiFiyatlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IcSandikSablonId",
                table: "FinansUrunEslesmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IcSandikSablonId",
                table: "FinansIsKayitlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IcSandikSablonId",
                table: "AmbalajUretimKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IcSandikSablonId",
                table: "AmbalajBagimsizSandiklar",
                type: "integer",
                nullable: true);

                        migrationBuilder.Sql("""
                                UPDATE "AmbalajUretimKalemleri" AS kalem
                                SET "IcSandikSablonId" = (
                                        SELECT sablon."Id"
                                        FROM "AmbalajIcSandikSablonlari" AS sablon
                                        WHERE sablon."Ad" = kalem."Ad"
                                            AND sablon."SandikTipi" = kalem."SandikTipi"
                                            AND sablon."Boy" = kalem."Boy"
                                            AND sablon."En" = kalem."En"
                                            AND sablon."Yukseklik" = kalem."Yukseklik"
                                        ORDER BY sablon."Id"
                                        LIMIT 1)
                                WHERE kalem."Tur" = 3;

                                UPDATE "AmbalajBagimsizSandiklar" AS sandik
                                SET "IcSandikSablonId" = (
                                        SELECT sablon."Id"
                                        FROM "AmbalajIcSandikSablonlari" AS sablon
                                        WHERE sablon."Ad" = sandik."Ad"
                                            AND sablon."SandikTipi" = sandik."SandikTipi"
                                            AND sablon."Boy" = sandik."Boy"
                                            AND sablon."En" = sandik."En"
                                            AND sablon."Yukseklik" = sandik."Yukseklik"
                                        ORDER BY sablon."Id"
                                        LIMIT 1)
                                WHERE sandik."Tur" = 3;

                                UPDATE "FinansIsKayitlari" AS finans
                                SET "IcSandikSablonId" = kaynak."IcSandikSablonId"
                                FROM "AmbalajUretimKalemleri" AS kaynak
                                WHERE finans."KaynakModul" = 'AmbalajUretimKalemi'
                                    AND finans."KaynakKayitId" = kaynak."Id";

                                UPDATE "FinansIsKayitlari" AS finans
                                SET "IcSandikSablonId" = kaynak."IcSandikSablonId"
                                FROM "AmbalajBagimsizSandiklar" AS kaynak
                                WHERE finans."KaynakModul" = 'AmbalajBagimsizSandik'
                                    AND finans."KaynakKayitId" = kaynak."Id";
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IcSandikSablonId",
                table: "FinansUrunEslesmeleri");

            migrationBuilder.DropColumn(
                name: "IcSandikSablonId",
                table: "FinansIsKayitlari");

            migrationBuilder.DropColumn(
                name: "IcSandikSablonId",
                table: "AmbalajUretimKalemleri");

            migrationBuilder.DropColumn(
                name: "IcSandikSablonId",
                table: "AmbalajBagimsizSandiklar");
        }
    }
}
