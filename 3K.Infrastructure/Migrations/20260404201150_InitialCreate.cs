using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LookupDepoLokasyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupDepoLokasyonlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupDepoLokasyonlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupGridDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupGridDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupGridDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupIslemTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupIslemTipleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupIslemTipleri_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupKullaniciRolleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupKullaniciRolleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupKullaniciRolleri_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupProjeDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupProjeDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupProjeDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupSandikDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupSandikDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupSandikDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupSandikTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupSandikTipleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupSandikTipleri_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupStokDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupStokDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupStokDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupUcKDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupUcKDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupUcKDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "LookupUrunDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupUrunDurumlari", x => x.Id);
                    table.UniqueConstraint("AK_LookupUrunDurumlari_Deger", x => x.Deger);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdSoyad = table.Column<string>(type: "text", nullable: false),
                    BasHarf = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SifreHash = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_LookupKullaniciRolleri_Rol",
                        column: x => x.Rol,
                        principalTable: "LookupKullaniciRolleri",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeNo = table.Column<string>(type: "text", nullable: false),
                    Musteri = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    PlanlananSevkTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SorumluKisi = table.Column<string>(type: "text", nullable: false),
                    FBNo = table.Column<string>(type: "text", nullable: true),
                    Guc = table.Column<string>(type: "text", nullable: true),
                    Gerilim = table.Column<string>(type: "text", nullable: true),
                    Lokasyon = table.Column<string>(type: "text", nullable: true),
                    OlcuResmiNo = table.Column<string>(type: "text", nullable: true),
                    NakilOlcuResmiNo = table.Column<string>(type: "text", nullable: true),
                    SonMontajResmiNo = table.Column<string>(type: "text", nullable: true),
                    ProjeMuduru = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projeler_LookupProjeDurumlari_Durum",
                        column: x => x.Durum,
                        principalTable: "LookupProjeDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MalzemeKodu = table.Column<string>(type: "text", nullable: false),
                    MalzemeAdi = table.Column<string>(type: "text", nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    Birim = table.Column<string>(type: "text", nullable: false),
                    Lokasyon = table.Column<string>(type: "text", nullable: true),
                    KaynakProje = table.Column<string>(type: "text", nullable: true),
                    StokGirisNedeni = table.Column<string>(type: "text", nullable: true),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokKayitlari_LookupStokDurumlari_Durum",
                        column: x => x.Durum,
                        principalTable: "LookupStokDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cekiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    OrijinalDosyaYolu = table.Column<string>(type: "text", nullable: false),
                    YuklemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cekiler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cekiler_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HareketGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    ReferansTipi = table.Column<string>(type: "text", nullable: false),
                    ReferansId = table.Column<string>(type: "text", nullable: true),
                    Islem = table.Column<string>(type: "text", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EskiDeger = table.Column<string>(type: "text", nullable: true),
                    YeniDeger = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HareketGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HareketGecmisleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HareketGecmisleri_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Revizyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    EskiDeger = table.Column<string>(type: "text", nullable: true),
                    YeniDeger = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revizyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Revizyonlar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revizyonlar_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sandiklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    SandikNo = table.Column<string>(type: "text", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    DepoLokasyonu = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sandiklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupDepoLokasyonlari_DepoLokasyonu",
                        column: x => x.DepoLokasyonu,
                        principalTable: "LookupDepoLokasyonlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupSandikDurumlari_Durum",
                        column: x => x.Durum,
                        principalTable: "LookupSandikDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupSandikTipleri_Tip",
                        column: x => x.Tip,
                        principalTable: "LookupSandikTipleri",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CekiSatirlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CekiId = table.Column<int>(type: "integer", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    OlcuResmiPozNo = table.Column<string>(type: "text", nullable: true),
                    BarkodNo = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: false),
                    IstenenAdet = table.Column<int>(type: "integer", nullable: false),
                    Birim = table.Column<string>(type: "text", nullable: false),
                    CekideGecenSandikNo = table.Column<string>(type: "text", nullable: false),
                    FiiliSandikNo = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    GridDurumu = table.Column<string>(type: "text", nullable: false),
                    UcKDurumu = table.Column<string>(type: "text", nullable: false),
                    IsManuelEklenen = table.Column<bool>(type: "boolean", nullable: false),
                    EklemeNedeni = table.Column<string>(type: "text", nullable: true),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    PaketleyenId = table.Column<int>(type: "integer", nullable: true),
                    KontrolEdenId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CekiSatirlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_Cekiler_CekiId",
                        column: x => x.CekiId,
                        principalTable: "Cekiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_Kullanicilar_KontrolEdenId",
                        column: x => x.KontrolEdenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_Kullanicilar_PaketleyenId",
                        column: x => x.PaketleyenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupGridDurumlari_GridDurumu",
                        column: x => x.GridDurumu,
                        principalTable: "LookupGridDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupUcKDurumlari_UcKDurumu",
                        column: x => x.UcKDurumu,
                        principalTable: "LookupUcKDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupUrunDurumlari_Durum",
                        column: x => x.Durum,
                        principalTable: "LookupUrunDurumlari",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FBTransferleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CekiSatiriId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    AsilFB = table.Column<string>(type: "text", nullable: false),
                    AlinanFB = table.Column<string>(type: "text", nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    Neden = table.Column<string>(type: "text", nullable: true),
                    IadeDurumu = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "SandikIcerikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SandikId = table.Column<int>(type: "integer", nullable: false),
                    CekiSatiriId = table.Column<int>(type: "integer", nullable: false),
                    KonulanAdet = table.Column<int>(type: "integer", nullable: false),
                    EksikAdet = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SandikIcerikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SandikIcerikleri_CekiSatirlari_CekiSatiriId",
                        column: x => x.CekiSatiriId,
                        principalTable: "CekiSatirlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SandikIcerikleri_Sandiklar_SandikId",
                        column: x => x.SandikId,
                        principalTable: "Sandiklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokKaydiId = table.Column<int>(type: "integer", nullable: false),
                    CekiSatiriId = table.Column<int>(type: "integer", nullable: false),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokHareketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_CekiSatirlari_CekiSatiriId",
                        column: x => x.CekiSatiriId,
                        principalTable: "CekiSatirlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_LookupIslemTipleri_IslemTipi",
                        column: x => x.IslemTipi,
                        principalTable: "LookupIslemTipleri",
                        principalColumn: "Deger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_StokKayitlari_StokKaydiId",
                        column: x => x.StokKaydiId,
                        principalTable: "StokKayitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LookupDepoLokasyonlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Belirsiz" },
                    { 2, 1, "Grid" },
                    { 3, 2, "UcK" },
                    { 4, 3, "Protest" }
                });

            migrationBuilder.InsertData(
                table: "LookupGridDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Bekliyor" },
                    { 2, 1, "Geldi" },
                    { 3, 2, "Gelmedi" },
                    { 4, 3, "SevkEdildi" }
                });

            migrationBuilder.InsertData(
                table: "LookupIslemTipleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "CekiYuklendi" },
                    { 2, 1, "SandikOlusturuldu" },
                    { 3, 2, "SandikBolundu" },
                    { 4, 3, "SandikDegisti" },
                    { 5, 4, "UrunTasindi" },
                    { 6, 5, "FBTransferi" },
                    { 7, 6, "StokKullanimi" },
                    { 8, 7, "EksikKapatildi" },
                    { 9, 8, "PDFAlindi" },
                    { 10, 9, "MailGonderildi" },
                    { 11, 10, "UrunGuncellendi" },
                    { 12, 11, "KullaniciOlusturuldu" },
                    { 13, 12, "ProjeOlusturuldu" }
                });

            migrationBuilder.InsertData(
                table: "LookupKullaniciRolleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Admin" },
                    { 2, 1, "Personel3K" },
                    { 3, 2, "PersonelGrid" },
                    { 4, 3, "Yonetici" }
                });

            migrationBuilder.InsertData(
                table: "LookupProjeDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Hazirlaniyor" },
                    { 2, 1, "Devam" },
                    { 3, 2, "Tamamlandi" },
                    { 4, 3, "Beklemede" },
                    { 5, 4, "SevkEdildi" },
                    { 6, 5, "EksikSevkEdildi" }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Bos" },
                    { 2, 1, "Hazirlaniyor" },
                    { 3, 2, "Hazir" },
                    { 4, 3, "Sevkedildi" }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikTipleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 1, "Proje" },
                    { 2, 2, "Yedek" },
                    { 3, 3, "Saha" }
                });

            migrationBuilder.InsertData(
                table: "LookupStokDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Aktif" },
                    { 2, 1, "Tukendi" },
                    { 3, 2, "Rezerve" }
                });

            migrationBuilder.InsertData(
                table: "LookupUcKDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Bekliyor" },
                    { 2, 1, "TamGeldi" },
                    { 3, 2, "EksikGeldi" },
                    { 4, 3, "Gelmedi" }
                });

            migrationBuilder.InsertData(
                table: "LookupUrunDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Bekliyor" },
                    { 2, 1, "KismiGeldi" },
                    { 3, 2, "Tamamlandi" },
                    { 4, 3, "Eksik" },
                    { 5, 4, "StoktanKarsilandi" },
                    { 6, 5, "FBdenKarsilandi" },
                    { 7, 6, "SonraGidecek" },
                    { 8, 7, "SandikDegisti" },
                    { 9, 8, "IptalVeyaPasif" },
                    { 10, 9, "TeslimAlindi" },
                    { 11, 10, "GeriGonderildi" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cekiler_ProjeId",
                table: "Cekiler",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_CekiId",
                table: "CekiSatirlari",
                column: "CekiId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_Durum",
                table: "CekiSatirlari",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_GridDurumu",
                table: "CekiSatirlari",
                column: "GridDurumu");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KontrolEdenId",
                table: "CekiSatirlari",
                column: "KontrolEdenId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_PaketleyenId",
                table: "CekiSatirlari",
                column: "PaketleyenId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_UcKDurumu",
                table: "CekiSatirlari",
                column: "UcKDurumu");

            migrationBuilder.CreateIndex(
                name: "IX_FBTransferleri_CekiSatiriId",
                table: "FBTransferleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_FBTransferleri_KullaniciId",
                table: "FBTransferleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_KullaniciId",
                table: "HareketGecmisleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_ProjeId",
                table: "HareketGecmisleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Email",
                table: "Kullanicilar",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Rol",
                table: "Kullanicilar",
                column: "Rol");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_Durum",
                table: "Projeler",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_KullaniciId",
                table: "Revizyonlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_ProjeId",
                table: "Revizyonlar",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_SandikIcerikleri_CekiSatiriId",
                table: "SandikIcerikleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_SandikIcerikleri_SandikId",
                table: "SandikIcerikleri",
                column: "SandikId");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_DepoLokasyonu",
                table: "Sandiklar",
                column: "DepoLokasyonu");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_Durum",
                table: "Sandiklar",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_ProjeId_SandikNo",
                table: "Sandiklar",
                columns: new[] { "ProjeId", "SandikNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_Tip",
                table: "Sandiklar",
                column: "Tip");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_CekiSatiriId",
                table: "StokHareketleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_IslemTipi",
                table: "StokHareketleri",
                column: "IslemTipi");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_KullaniciId",
                table: "StokHareketleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_ProjeId",
                table: "StokHareketleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_StokKaydiId",
                table: "StokHareketleri",
                column: "StokKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_StokKayitlari_Durum",
                table: "StokKayitlari",
                column: "Durum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FBTransferleri");

            migrationBuilder.DropTable(
                name: "HareketGecmisleri");

            migrationBuilder.DropTable(
                name: "Revizyonlar");

            migrationBuilder.DropTable(
                name: "SandikIcerikleri");

            migrationBuilder.DropTable(
                name: "StokHareketleri");

            migrationBuilder.DropTable(
                name: "Sandiklar");

            migrationBuilder.DropTable(
                name: "CekiSatirlari");

            migrationBuilder.DropTable(
                name: "LookupIslemTipleri");

            migrationBuilder.DropTable(
                name: "StokKayitlari");

            migrationBuilder.DropTable(
                name: "LookupDepoLokasyonlari");

            migrationBuilder.DropTable(
                name: "LookupSandikDurumlari");

            migrationBuilder.DropTable(
                name: "LookupSandikTipleri");

            migrationBuilder.DropTable(
                name: "Cekiler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "LookupGridDurumlari");

            migrationBuilder.DropTable(
                name: "LookupUcKDurumlari");

            migrationBuilder.DropTable(
                name: "LookupUrunDurumlari");

            migrationBuilder.DropTable(
                name: "LookupStokDurumlari");

            migrationBuilder.DropTable(
                name: "Projeler");

            migrationBuilder.DropTable(
                name: "LookupKullaniciRolleri");

            migrationBuilder.DropTable(
                name: "LookupProjeDurumlari");
        }
    }
}
