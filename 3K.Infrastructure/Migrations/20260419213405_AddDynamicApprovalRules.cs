using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicApprovalRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FBTransferleri");

            migrationBuilder.CreateTable(
                name: "IslemOnayKurallari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IslemKodu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KuralAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OnayGerektirirMi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslemOnayKurallari", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "IslemOnayKurallari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IslemKodu", "KuralAdi", "OnayGerektirirMi", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8111), "UcKDurum_ProjedenKarsilandi", "Projeden Karşılandı", true, null, null },
                    { 2, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8663), "UcKDurum_StoktanKarsilandi", "Stoktan Karşılandı", true, null, null },
                    { 3, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8664), "UcKDurum_TedarikcidenGeldi", "Tedarikçiden Geldi", true, null, null },
                    { 4, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8665), "UcKDurum_TamGeldi", "Tam Geldi", false, null, null },
                    { 5, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8666), "UcKDurum_EksikGeldi", "Eksik Geldi", false, null, null },
                    { 6, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8666), "UcKDurum_Gelmedi", "Gelmedi", false, null, null },
                    { 7, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8667), "UcKDurum_TrafoSevk", "Trafo Sevk", false, null, null },
                    { 8, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8668), "UcKDurum_Iptal", "İptal", false, null, null },
                    { 9, null, new DateTime(2026, 4, 19, 21, 34, 5, 358, DateTimeKind.Utc).AddTicks(8669), "UcKDurum_Sipariste", "Siparişte", false, null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGeriGonderilmeSebepleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[] { 3, 2, "Projeye Geri Dönüş" });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4810));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5556));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5658));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5659));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5558));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5559));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5560));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5561));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5563));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5564));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5565));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5566));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5567));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5660));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5662));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5663));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(5858));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6200));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6201));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6212));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6213));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6214));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6214));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(6218));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4088));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4298));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4299));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 21, 34, 5, 359, DateTimeKind.Utc).AddTicks(4301));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IslemOnayKurallari");

            migrationBuilder.DeleteData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.CreateTable(
                name: "FBTransferleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CekiSatiriId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    AlinanFB = table.Column<string>(type: "text", nullable: false),
                    AsilFB = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IadeDurumu = table.Column<string>(type: "text", nullable: true),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    Neden = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FBTransferleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FBTransferleri_CekiSatirlari_CekiSatiriId",
                        column: x => x.CekiSatiriId,
                        principalTable: "CekiSatirlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FBTransferleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8186));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8928));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9019));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9020));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8930));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8931));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8932));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8933));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8934));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8936));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8938));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(8939));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9021));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9022));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9023));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9221));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9568));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9571));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9571));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9572));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9572));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9573));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9573));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9574));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9574));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9574));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9575));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(9575));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(7347));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 20, 17, 7, 431, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.CreateIndex(
                name: "IX_FBTransferleri_CekiSatiriId",
                table: "FBTransferleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_FBTransferleri_KullaniciId",
                table: "FBTransferleri",
                column: "KullaniciId");
        }
    }
}
