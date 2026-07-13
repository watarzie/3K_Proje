using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    public partial class AmbalajManuelSandikVeIcSablonlar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SandikTipi",
                table: "AmbalajUretimKalemleri",
                type: "text",
                nullable: false,
                defaultValue: "Ahşap Kapalı");

            migrationBuilder.CreateTable(
                name: "AmbalajIcSandikSablonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    SandikTipi = table.Column<string>(type: "text", nullable: false),
                    Boy = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    En = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Yukseklik = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AmbalajIcSandikSablonlari", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajIcSandikSablonlari_Ad",
                table: "AmbalajIcSandikSablonlari",
                column: "Ad",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AmbalajIcSandikSablonlari");
            migrationBuilder.DropColumn(name: "SandikTipi", table: "AmbalajUretimKalemleri");
        }
    }
}