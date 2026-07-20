using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    public partial class AddSpecialCrateSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajBagimsizSandiklar_KaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                column: "KaynakSandikId");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Sandiklar_KaynakSandikId",
                table: "AmbalajBagimsizSandiklar",
                column: "KaynakSandikId",
                principalTable: "Sandiklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AmbalajBagimsizSandiklar_Sandiklar_KaynakSandikId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropIndex(
                name: "IX_AmbalajBagimsizSandiklar_KaynakSandikId",
                table: "AmbalajBagimsizSandiklar");

            migrationBuilder.DropColumn(
                name: "KaynakSandikId",
                table: "AmbalajBagimsizSandiklar");
        }
    }
}
