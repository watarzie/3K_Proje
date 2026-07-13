using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    public partial class AmbalajBagimsizUretimKuyruklari : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IcSandikFirinPartiNo",
                table: "AmbalajUretimPlanlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IcSandiklarDurumId",
                table: "AmbalajUretimPlanlari",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "IlaveFirinPartiNo",
                table: "AmbalajUretimPlanlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IlaveSandiklarDurumId",
                table: "AmbalajUretimPlanlari",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ProjeSandiklariDurumId",
                table: "AmbalajUretimPlanlari",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IcSandikFirinPartiNo", table: "AmbalajUretimPlanlari");
            migrationBuilder.DropColumn(name: "IcSandiklarDurumId", table: "AmbalajUretimPlanlari");
            migrationBuilder.DropColumn(name: "IlaveFirinPartiNo", table: "AmbalajUretimPlanlari");
            migrationBuilder.DropColumn(name: "IlaveSandiklarDurumId", table: "AmbalajUretimPlanlari");
            migrationBuilder.DropColumn(name: "ProjeSandiklariDurumId", table: "AmbalajUretimPlanlari");
        }
    }
}