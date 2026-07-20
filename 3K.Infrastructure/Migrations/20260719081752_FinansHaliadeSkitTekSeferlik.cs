using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansHaliadeSkitTekSeferlik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "FinansDuzenliIsleri"
                SET "Aktif" = FALSE
                WHERE LOWER("IsAdi") IN ('haliade-x', 'skit');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "FinansDuzenliIsleri"
                SET "Aktif" = TRUE
                WHERE LOWER("IsAdi") IN ('haliade-x', 'skit');
                """);
        }
    }
}
