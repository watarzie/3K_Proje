using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    public partial class AmbalajUretimPlanlamasi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmbalajUretimPlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    FirinPartiNo = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbalajUretimPlanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmbalajUretimPlanlari_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmbalajUretimKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AmbalajUretimPlaniId = table.Column<int>(type: "integer", nullable: false),
                    KaynakSandikId = table.Column<int>(type: "integer", nullable: true),
                    UstKalemId = table.Column<int>(type: "integer", nullable: true),
                    Tur = table.Column<int>(type: "integer", nullable: false),
                    UretimeAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    SandikNo = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: true),
                    Adet = table.Column<int>(type: "integer", nullable: false),
                    Boy = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    En = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Yukseklik = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    KullanimAmaci = table.Column<string>(type: "text", nullable: true),
                    TalimatVeren = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbalajUretimKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmbalajUretimKalemleri_AmbalajUretimKalemleri_UstKalemId",
                        column: x => x.UstKalemId,
                        principalTable: "AmbalajUretimKalemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AmbalajUretimKalemleri_AmbalajUretimPlanlari_AmbalajUretimPlaniId",
                        column: x => x.AmbalajUretimPlaniId,
                        principalTable: "AmbalajUretimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmbalajUretimKalemleri_Sandiklar_KaynakSandikId",
                        column: x => x.KaynakSandikId,
                        principalTable: "Sandiklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajUretimKalemleri_AmbalajUretimPlaniId_KaynakSandikId",
                table: "AmbalajUretimKalemleri",
                columns: new[] { "AmbalajUretimPlaniId", "KaynakSandikId" },
                unique: true,
                filter: "\"KaynakSandikId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajUretimKalemleri_KaynakSandikId",
                table: "AmbalajUretimKalemleri",
                column: "KaynakSandikId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajUretimKalemleri_UstKalemId",
                table: "AmbalajUretimKalemleri",
                column: "UstKalemId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbalajUretimPlanlari_ProjeId",
                table: "AmbalajUretimPlanlari",
                column: "ProjeId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AmbalajUretimKalemleri");
            migrationBuilder.DropTable(name: "AmbalajUretimPlanlari");
        }
    }
}
