using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UcKKarsilamaVeProjeTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KaynakHedefProjeNo",
                table: "CekiSatirlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UcKAciklama",
                table: "CekiSatirlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UcKKarsilamaTipi",
                table: "CekiSatirlari",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProjeTransferleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KaynakProjeId = table.Column<int>(type: "integer", nullable: false),
                    HedefProjeId = table.Column<int>(type: "integer", nullable: false),
                    KaynakCekiSatiriId = table.Column<int>(type: "integer", nullable: false),
                    HedefCekiSatiriId = table.Column<int>(type: "integer", nullable: true),
                    BarkodNo = table.Column<string>(type: "text", nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjeTransferleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_CekiSatirlari_HedefCekiSatiriId",
                        column: x => x.HedefCekiSatiriId,
                        principalTable: "CekiSatirlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_CekiSatirlari_KaynakCekiSatiriId",
                        column: x => x.KaynakCekiSatiriId,
                        principalTable: "CekiSatirlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_Projeler_HedefProjeId",
                        column: x => x.HedefProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_Projeler_KaynakProjeId",
                        column: x => x.KaynakProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LookupGridDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 8, 7, "TamGeldi" },
                    { 9, 8, "EksikGeldi" },
                    { 10, 9, "Gelmedi" },
                    { 11, 10, "TrafoSevk" },
                    { 12, 11, "Iptal" },
                    { 13, 12, "Sipariste" }
                });

            migrationBuilder.InsertData(
                table: "LookupUcKDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 8, 7, "ProjedenKarsilandi" },
                    { 9, 8, "StoktanKarsilandi" },
                    { 10, 9, "TedarikcidenGeldi" },
                    { 11, 10, "BaskaProyeVerildi" },
                    { 12, 11, "HataliUrun" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_HedefCekiSatiriId",
                table: "ProjeTransferleri",
                column: "HedefCekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_HedefProjeId",
                table: "ProjeTransferleri",
                column: "HedefProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_KaynakCekiSatiriId",
                table: "ProjeTransferleri",
                column: "KaynakCekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_KaynakProjeId",
                table: "ProjeTransferleri",
                column: "KaynakProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_KullaniciId",
                table: "ProjeTransferleri",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjeTransferleri");

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "KaynakHedefProjeNo",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "UcKAciklama",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "UcKKarsilamaTipi",
                table: "CekiSatirlari");
        }
    }
}
