using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansSiparisTakipModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinansDuzenliIsleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsAdi = table.Column<string>(type: "text", nullable: false),
                    IsTuru = table.Column<string>(type: "text", nullable: false),
                    Musteri = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    TekrarSikligi = table.Column<string>(type: "text", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OlusturmaGunu = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    SonOlusturulanDonem = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansDuzenliIsleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinansGiderKategorileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansGiderKategorileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinansIslemGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferansTipi = table.Column<string>(type: "text", nullable: false),
                    ReferansId = table.Column<int>(type: "integer", nullable: false),
                    Islem = table.Column<string>(type: "text", nullable: false),
                    EskiDeger = table.Column<string>(type: "text", nullable: true),
                    YeniDeger = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansIslemGecmisleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinansSiparisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KayitNo = table.Column<string>(type: "text", nullable: false),
                    ProjeId = table.Column<int>(type: "integer", nullable: true),
                    AnaProjeNo = table.Column<string>(type: "text", nullable: false),
                    PoNumarasi = table.Column<string>(type: "text", nullable: false),
                    SiparisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    IptalTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IptalAciklamasi = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansSiparisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansSiparisleri_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FinansOzelIsleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KayitNo = table.Column<string>(type: "text", nullable: false),
                    IsTuru = table.Column<string>(type: "text", nullable: false),
                    Musteri = table.Column<string>(type: "text", nullable: false),
                    ProjeId = table.Column<int>(type: "integer", nullable: true),
                    IsAdi = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Birim = table.Column<string>(type: "text", nullable: false),
                    IsTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DuzenliIsId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansOzelIsleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansOzelIsleri_FinansDuzenliIsleri_DuzenliIsId",
                        column: x => x.DuzenliIsId,
                        principalTable: "FinansDuzenliIsleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinansOzelIsleri_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FinansGiderleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KategoriId = table.Column<int>(type: "integer", nullable: false),
                    AltKategori = table.Column<string>(type: "text", nullable: true),
                    FirmaVeyaKisi = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvDahil = table.Column<bool>(type: "boolean", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProjeId = table.Column<int>(type: "integer", nullable: true),
                    IsTuru = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansGiderleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansGiderleri_FinansGiderKategorileri_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "FinansGiderKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansGiderleri_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FinansFaturalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KayitNo = table.Column<string>(type: "text", nullable: false),
                    SiparisId = table.Column<int>(type: "integer", nullable: false),
                    FaturaNumarasi = table.Column<string>(type: "text", nullable: false),
                    FaturaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    IptalTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IptalAciklamasi = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansFaturalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansFaturalari_FinansSiparisleri_SiparisId",
                        column: x => x.SiparisId,
                        principalTable: "FinansSiparisleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinansIsKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: true),
                    OzelIsId = table.Column<int>(type: "integer", nullable: true),
                    KaynakKayitId = table.Column<int>(type: "integer", nullable: true),
                    KaynakModul = table.Column<string>(type: "text", nullable: false),
                    ProjeNo = table.Column<string>(type: "text", nullable: false),
                    Musteri = table.Column<string>(type: "text", nullable: false),
                    SandikNo = table.Column<string>(type: "text", nullable: false),
                    SandikAdi = table.Column<string>(type: "text", nullable: false),
                    IsTuru = table.Column<int>(type: "integer", nullable: false),
                    Adet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BirimM3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UretimeAlinmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UretimTamamlanmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UretimDurumu = table.Column<string>(type: "text", nullable: false),
                    AktarimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KaynakAktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansIsKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansIsKayitlari_FinansOzelIsleri_OzelIsId",
                        column: x => x.OzelIsId,
                        principalTable: "FinansOzelIsleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansIsKayitlari_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FinansBelgeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BelgeTuru = table.Column<int>(type: "integer", nullable: false),
                    SiparisId = table.Column<int>(type: "integer", nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    OzelIsId = table.Column<int>(type: "integer", nullable: true),
                    GiderId = table.Column<int>(type: "integer", nullable: true),
                    DosyaAdi = table.Column<string>(type: "text", nullable: false),
                    DosyaYolu = table.Column<string>(type: "text", nullable: false),
                    IcerikTuru = table.Column<string>(type: "text", nullable: false),
                    Boyut = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansBelgeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansBelgeleri_FinansFaturalari_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "FinansFaturalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansBelgeleri_FinansGiderleri_GiderId",
                        column: x => x.GiderId,
                        principalTable: "FinansGiderleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansBelgeleri_FinansOzelIsleri_OzelIsId",
                        column: x => x.OzelIsId,
                        principalTable: "FinansOzelIsleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansBelgeleri_FinansSiparisleri_SiparisId",
                        column: x => x.SiparisId,
                        principalTable: "FinansSiparisleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinansSiparisKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiparisId = table.Column<int>(type: "integer", nullable: false),
                    IsKaydiId = table.Column<int>(type: "integer", nullable: false),
                    Adet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    M3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansSiparisKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansSiparisKalemleri_FinansIsKayitlari_IsKaydiId",
                        column: x => x.IsKaydiId,
                        principalTable: "FinansIsKayitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansSiparisKalemleri_FinansSiparisleri_SiparisId",
                        column: x => x.SiparisId,
                        principalTable: "FinansSiparisleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinansFaturaKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    SiparisKalemiId = table.Column<int>(type: "integer", nullable: false),
                    Adet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    M3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansFaturaKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansFaturaKalemleri_FinansFaturalari_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "FinansFaturalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansFaturaKalemleri_FinansSiparisKalemleri_SiparisKalemiId",
                        column: x => x.SiparisKalemiId,
                        principalTable: "FinansSiparisKalemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinansBelgeleri_FaturaId",
                table: "FinansBelgeleri",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansBelgeleri_GiderId",
                table: "FinansBelgeleri",
                column: "GiderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansBelgeleri_OzelIsId",
                table: "FinansBelgeleri",
                column: "OzelIsId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansBelgeleri_SiparisId",
                table: "FinansBelgeleri",
                column: "SiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansFaturaKalemleri_FaturaId_SiparisKalemiId",
                table: "FinansFaturaKalemleri",
                columns: new[] { "FaturaId", "SiparisKalemiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansFaturaKalemleri_SiparisKalemiId",
                table: "FinansFaturaKalemleri",
                column: "SiparisKalemiId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansFaturalari_FaturaNumarasi",
                table: "FinansFaturalari",
                column: "FaturaNumarasi");

            migrationBuilder.CreateIndex(
                name: "IX_FinansFaturalari_KayitNo",
                table: "FinansFaturalari",
                column: "KayitNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansFaturalari_SiparisId",
                table: "FinansFaturalari",
                column: "SiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansGiderKategorileri_Ad",
                table: "FinansGiderKategorileri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansGiderleri_KategoriId",
                table: "FinansGiderleri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansGiderleri_ProjeId",
                table: "FinansGiderleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansIsKayitlari_KaynakModul_KaynakKayitId",
                table: "FinansIsKayitlari",
                columns: new[] { "KaynakModul", "KaynakKayitId" },
                unique: true,
                filter: "\"KaynakKayitId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinansIsKayitlari_OzelIsId",
                table: "FinansIsKayitlari",
                column: "OzelIsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansIsKayitlari_ProjeId_IsTuru",
                table: "FinansIsKayitlari",
                columns: new[] { "ProjeId", "IsTuru" });

            migrationBuilder.CreateIndex(
                name: "IX_FinansIslemGecmisleri_ReferansTipi_ReferansId_IslemTarihi",
                table: "FinansIslemGecmisleri",
                columns: new[] { "ReferansTipi", "ReferansId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_FinansOzelIsleri_DuzenliIsId",
                table: "FinansOzelIsleri",
                column: "DuzenliIsId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansOzelIsleri_KayitNo",
                table: "FinansOzelIsleri",
                column: "KayitNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansOzelIsleri_ProjeId",
                table: "FinansOzelIsleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisKalemleri_IsKaydiId",
                table: "FinansSiparisKalemleri",
                column: "IsKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisKalemleri_SiparisId_IsKaydiId",
                table: "FinansSiparisKalemleri",
                columns: new[] { "SiparisId", "IsKaydiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisleri_KayitNo",
                table: "FinansSiparisleri",
                column: "KayitNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisleri_PoNumarasi",
                table: "FinansSiparisleri",
                column: "PoNumarasi");

            migrationBuilder.CreateIndex(
                name: "IX_FinansSiparisleri_ProjeId",
                table: "FinansSiparisleri",
                column: "ProjeId");

            migrationBuilder.Sql("""
                INSERT INTO "MenuTanimlari" ("Id", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "CreatedDate")
                SELECT 47, 'ri-bank-card-line', 'finans-yonetimi', 'MENU.FINANS_YONETIMI', NULL, '/finans-yonetimi', 9, CURRENT_TIMESTAMP
                WHERE NOT EXISTS (
                    SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'finans-yonetimi'
                );

                INSERT INTO "RolYetkileri" ("Id", "MenuTanimiId", "RolId", "YetkiTipiId", "CreatedDate")
                SELECT 43, menu."Id", 1, 3, CURRENT_TIMESTAMP
                FROM "MenuTanimlari" AS menu
                WHERE menu."Kod" = 'finans-yonetimi'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "RolYetkileri" AS yetki
                      WHERE yetki."RolId" = 1 AND yetki."MenuTanimiId" = menu."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Finans verisini korumak için bu migration otomatik geri alınamaz.
        }
    }
}
