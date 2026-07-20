using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansIcSandikTipiTarifeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SandikTipi",
                table: "FinansIsKayitlari",
                type: "text",
                nullable: true);

                        migrationBuilder.Sql("""
                                UPDATE "FinansIsKayitlari" AS finans
                                SET "SandikTipi" = kaynak."SandikTipi"
                                FROM "AmbalajUretimKalemleri" AS kaynak
                                WHERE finans."KaynakModul" = 'AmbalajUretimKalemi'
                                    AND finans."KaynakKayitId" = kaynak."Id";

                                UPDATE "FinansIsKayitlari" AS finans
                                SET "SandikTipi" = kaynak."SandikTipi"
                                FROM "AmbalajBagimsizSandiklar" AS kaynak
                                WHERE finans."KaynakModul" = 'AmbalajBagimsizSandik'
                                    AND finans."KaynakKayitId" = kaynak."Id";
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SandikTipi",
                table: "FinansIsKayitlari");
        }
    }
}
