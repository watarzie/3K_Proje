using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AmbalajYalnizNormalProjeKaynagi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("""
                                UPDATE "AmbalajUretimKalemleri" AS kalem
                                SET "UretimeAlindi" = FALSE,
                                        "UpdatedDate" = NOW(),
                                        "UpdatedBy" = 'AmbalajYalnizNormalProjeKaynagi'
                                FROM "AmbalajUretimPlanlari" AS plan
                                INNER JOIN "Projeler" AS proje ON proje."Id" = plan."ProjeId"
                                WHERE kalem."AmbalajUretimPlaniId" = plan."Id"
                                    AND proje."ProjeTipiId" IN (2, 3)
                                    AND kalem."UretimeAlindi" = TRUE;

                                UPDATE "AmbalajUretimPlanlari" AS plan
                                SET "ProjeSandiklariDurumId" = 1,
                                        "IlaveSandiklarDurumId" = 1,
                                        "IcSandiklarDurumId" = 1,
                                        "UpdatedDate" = NOW(),
                                        "UpdatedBy" = 'AmbalajYalnizNormalProjeKaynagi'
                                FROM "Projeler" AS proje
                                WHERE plan."ProjeId" = proje."Id"
                                    AND proje."ProjeTipiId" IN (2, 3);

                                UPDATE "FinansIsKayitlari" AS finans
                                SET "KaynakAktif" = FALSE,
                                        "UpdatedDate" = NOW(),
                                        "UpdatedBy" = 'AmbalajYalnizNormalProjeKaynagi'
                                FROM "Projeler" AS proje
                                WHERE finans."ProjeId" = proje."Id"
                                    AND proje."ProjeTipiId" IN (2, 3)
                                    AND finans."KaynakModul" IN ('AmbalajUretimKalemi', 'AmbalajSarfKereste')
                                    AND finans."KaynakAktif" = TRUE;

                                UPDATE "AmbalajBagimsizSandiklar"
                                SET "KaynakSandikId" = NULL,
                                        "UpdatedDate" = NOW(),
                                        "UpdatedBy" = 'AmbalajYalnizNormalProjeKaynagi'
                                WHERE "Tur" IN (4, 5)
                                    AND "KaynakSandikId" IS NOT NULL;
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
