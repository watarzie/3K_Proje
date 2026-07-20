using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _3K.Infrastructure.Data;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260714101000_UnifySpecialPackagingCrates")]
    public partial class UnifySpecialPackagingCrates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjeId",
                table: "AmbalajBagimsizSandiklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajBagimsizSandiklar_ProjeId",
                table: "AmbalajBagimsizSandiklar",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajBagimsizSandiklar_UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                column: "UstKaynakSandikId");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Projeler_ProjeId",
                table: "AmbalajBagimsizSandiklar",
                column: "ProjeId",
                principalTable: "Projeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Sandiklar_UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                column: "UstKaynakSandikId",
                principalTable: "Sandiklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Projeler_ProjeId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Sandiklar_UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropIndex(
                name: "IX_AmbalajBagimsizSandiklar_ProjeId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropIndex(
                name: "IX_AmbalajBagimsizSandiklar_UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropColumn(
                name: "ProjeId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropColumn(
                name: "UstKaynakSandikId",
                table: "AmbalajBagimsizSandiklar");
        }
    }
}