using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_IdBazli : Migration
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
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupDepoLokasyonlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupGeriGonderilmeSebepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupGeriGonderilmeSebepleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupGridDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupGridDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupIslemTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupIslemTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupProjeDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupProjeDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupSandikDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupSandikDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupSandikTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupSandikTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupStokDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupStokDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupUcKDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupUcKDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupUrunDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupUrunDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupYetkiTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupYetkiTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuTanimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kod = table.Column<string>(type: "text", nullable: false),
                    LabelKey = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<string>(type: "text", nullable: true),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuTanimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuTanimlari_MenuTanimlari_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeNo = table.Column<string>(type: "text", nullable: false),
                    Musteri = table.Column<string>(type: "text", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projeler_LookupProjeDurumlari_DurumId",
                        column: x => x.DurumId,
                        principalTable: "LookupProjeDurumlari",
                        principalColumn: "Id",
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
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokKayitlari_LookupStokDurumlari_DurumId",
                        column: x => x.DurumId,
                        principalTable: "LookupStokDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IslemOnayKurallari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LookupUcKDurumId = table.Column<int>(type: "integer", nullable: false),
                    OnayGerektirirMi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslemOnayKurallari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IslemOnayKurallari_LookupUcKDurumlari_LookupUcKDurumId",
                        column: x => x.LookupUcKDurumId,
                        principalTable: "LookupUcKDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdSoyad = table.Column<string>(type: "text", nullable: false),
                    BasHarf = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SifreHash = table.Column<string>(type: "text", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    MenuTanimiId = table.Column<int>(type: "integer", nullable: false),
                    YetkiTipiId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolYetkileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_LookupYetkiTipleri_YetkiTipiId",
                        column: x => x.YetkiTipiId,
                        principalTable: "LookupYetkiTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_MenuTanimlari_MenuTanimiId",
                        column: x => x.MenuTanimiId,
                        principalTable: "MenuTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "Sandiklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeId = table.Column<int>(type: "integer", nullable: false),
                    SandikNo = table.Column<string>(type: "text", nullable: false),
                    TipId = table.Column<int>(type: "integer", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    DepoLokasyonId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sandiklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupDepoLokasyonlari_DepoLokasyonId",
                        column: x => x.DepoLokasyonId,
                        principalTable: "LookupDepoLokasyonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupSandikDurumlari_DurumId",
                        column: x => x.DurumId,
                        principalTable: "LookupSandikDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_LookupSandikTipleri_TipId",
                        column: x => x.TipId,
                        principalTable: "LookupSandikTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sandiklar_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "OnayBekleyenIslemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IslemAciklamasi = table.Column<string>(type: "text", nullable: false),
                    CommandType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    TalepEdenKullaniciId = table.Column<int>(type: "integer", nullable: false),
                    OnaylayanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    RedAciklamasi = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnayBekleyenIslemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnayBekleyenIslemler_Kullanicilar_OnaylayanKullaniciId",
                        column: x => x.OnaylayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnayBekleyenIslemler_Kullanicilar_TalepEdenKullaniciId",
                        column: x => x.TalepEdenKullaniciId,
                        principalTable: "Kullanicilar",
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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    GridDurumuId = table.Column<int>(type: "integer", nullable: false),
                    GridGelenAdet = table.Column<int>(type: "integer", nullable: false),
                    TrafoSevkAdet = table.Column<int>(type: "integer", nullable: false),
                    GridSevkDurumuId = table.Column<int>(type: "integer", nullable: false),
                    GridSevkMiktari = table.Column<int>(type: "integer", nullable: true),
                    GridSevkTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GridNotu = table.Column<string>(type: "text", nullable: true),
                    GridPersonelId = table.Column<int>(type: "integer", nullable: true),
                    UcKDurumuId = table.Column<int>(type: "integer", nullable: false),
                    UcKKarsilamaTipiId = table.Column<int>(type: "integer", nullable: false),
                    GelenMiktar = table.Column<int>(type: "integer", nullable: false),
                    TeslimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UcKNotu = table.Column<string>(type: "text", nullable: true),
                    KaynakHedefProjeNo = table.Column<string>(type: "text", nullable: true),
                    UcKAciklama = table.Column<string>(type: "text", nullable: true),
                    KarsilananMiktar = table.Column<int>(type: "integer", nullable: false),
                    HataliMiktar = table.Column<int>(type: "integer", nullable: false),
                    GeriGonderilmeSebebiId = table.Column<int>(type: "integer", nullable: true),
                    KaynakProjeId = table.Column<int>(type: "integer", nullable: true),
                    IsManuelEklenen = table.Column<bool>(type: "boolean", nullable: false),
                    EklemeNedeni = table.Column<string>(type: "text", nullable: true),
                    PaketleyenId = table.Column<int>(type: "integer", nullable: true),
                    KontrolEdenId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                        name: "FK_CekiSatirlari_Kullanicilar_GridPersonelId",
                        column: x => x.GridPersonelId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                        name: "FK_CekiSatirlari_LookupGeriGonderilmeSebepleri_GeriGonderilmeS~",
                        column: x => x.GeriGonderilmeSebebiId,
                        principalTable: "LookupGeriGonderilmeSebepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupGridDurumlari_GridDurumuId",
                        column: x => x.GridDurumuId,
                        principalTable: "LookupGridDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupUcKDurumlari_UcKDurumuId",
                        column: x => x.UcKDurumuId,
                        principalTable: "LookupUcKDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupUrunDurumlari_DurumId",
                        column: x => x.DurumId,
                        principalTable: "LookupUrunDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_Projeler_KaynakProjeId",
                        column: x => x.KaynakProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                    IslemTipiId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                        name: "FK_StokHareketleri_LookupIslemTipleri_IslemTipiId",
                        column: x => x.IslemTipiId,
                        principalTable: "LookupIslemTipleri",
                        principalColumn: "Id",
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
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2262), "Belirsiz", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2264), "3K", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2264), "Seymen", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2265), "Grid", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGeriGonderilmeSebepleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2981), "Tadilat", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2982), "Iptal", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2983), "Projeye Geri Dönüş", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGridDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2601), "Bekliyor", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2602), "Üretimde", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2603), "Stok Hazır", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2604), "Sevk Edildi", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2604), "Kısmi Sevk Edildi", null, null },
                    { 6, 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2605), "Bekletiliyor", null, null },
                    { 7, 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2606), "İptal Edildi", null, null },
                    { 8, 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2606), "Tam Geldi", null, null },
                    { 9, 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2607), "Eksik Geldi", null, null },
                    { 10, 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2608), "Gelmedi", null, null },
                    { 11, 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2609), "Trafo Sevk", null, null },
                    { 12, 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2609), "İptal", null, null },
                    { 13, 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2610), "Siparişte", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupIslemTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3427), "CekiYuklendi", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3429), "SandikOlusturuldu", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3429), "SandikBolundu", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3430), "SandikDegisti", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3431), "UrunTasindi", null, null },
                    { 6, 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3431), "FBTransferi", null, null },
                    { 7, 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3432), "StokKullanimi", null, null },
                    { 8, 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3433), "EksikKapatildi", null, null },
                    { 9, 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3433), "PDFAlindi", null, null },
                    { 10, 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3434), "MailGonderildi", null, null },
                    { 11, 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3435), "UrunGuncellendi", null, null },
                    { 12, 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3436), "KullaniciOlusturuldu", null, null },
                    { 13, 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3436), "ProjeOlusturuldu", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupProjeDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(7955), "Hazırlanıyor", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(8399), "Devam", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(8401), "Tamamlandı", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(8402), "Beklemede", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(8402), "Sevk Edildi", null, null },
                    { 6, 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 475, DateTimeKind.Utc).AddTicks(8403), "Eksik Sevk Edildi", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(1864), "Boş", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(1866), "Hazırlanıyor", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(1867), "Hazır", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(1868), "Sevk Edildi", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2084), "Proje", null, null },
                    { 2, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2086), "Yedek", null, null },
                    { 3, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2087), "Saha", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupStokDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3282), "Aktif", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3283), "Tukendi", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3284), "Rezerve", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupUcKDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2768), "Bekliyor", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2770), "Tam Geldi", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2770), "Eksik Geldi", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2771), "Gelmedi", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2772), "Paketlendi", null, null },
                    { 6, 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2773), "Kontrol Edildi", null, null },
                    { 7, 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2811), "İade Edildi", null, null },
                    { 8, 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2812), "Projeden Karşılandı", null, null },
                    { 9, 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2813), "Stoktan Karşılandı", null, null },
                    { 10, 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2823), "Tedarikçiden Geldi", null, null },
                    { 11, 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2824), "Başka Projeye Verildi", null, null },
                    { 12, 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2825), "Geri Gönderildi", null, null },
                    { 13, 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2825), "Hatalı Ürün", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupUrunDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2421), "Bekliyor", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2422), "Kısmi Geldi", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2423), "Tamamlandı", null, null },
                    { 4, 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2424), "Eksik", null, null },
                    { 5, 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2424), "Stoktan Karşılandı", null, null },
                    { 6, 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2425), "FB'den Karşılandı", null, null },
                    { 7, 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2426), "Sonra Gidecek", null, null },
                    { 8, 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2427), "Sandık Değişti", null, null },
                    { 9, 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2427), "İptal/Pasif", null, null },
                    { 10, 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2429), "Teslim Alındı", null, null },
                    { 11, 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2429), "Geri Gönderildi", null, null },
                    { 12, 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2430), "Kısmi Tamamlandı", null, null },
                    { 13, 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2431), "Kayıp", null, null },
                    { 14, 13, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2431), "Grid'de Hazır", null, null },
                    { 15, 14, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2432), "Grid'de Eksik", null, null },
                    { 16, 15, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2444), "Siparişte", null, null },
                    { 17, 16, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2444), "Gelmedi", null, null },
                    { 18, 17, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2445), "Trafo Sevk", null, null },
                    { 19, 18, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2446), "Başka Projeye Verildi", null, null },
                    { 20, 19, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(2447), "Hatalı Ürün", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupYetkiTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3134), "N", null, null },
                    { 2, 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3135), "R", null, null },
                    { 3, 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(3136), "W", null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(979), "ri-dashboard-line", "dashboard", "MENU.DASHBOARD", null, "/dashboard", 1, null, null },
                    { 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1704), "ri-folder-line", "projeler", "MENU.PROJELER", null, null, 2, null, null },
                    { 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1706), "ri-archive-line", "sandik-yonetimi", "MENU.SANDIK_YONETIMI", null, "/sandik-yonetimi", 3, null, null },
                    { 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1707), "ri-error-warning-line", "eksik-listesi", "MENU.EKSIK_LISTESI", null, "/eksik-listesi", 4, null, null },
                    { 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1708), "ri-building-2-line", "depo-durumu", "MENU.DEPO_DURUMU", null, "/depo-durumu", 5, null, null },
                    { 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1709), "ri-arrow-left-right-line", "fb-transfer", "MENU.FB_TRANSFER", null, "/fb-transfer", 6, null, null },
                    { 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1749), "ri-stack-line", "stok", "MENU.STOK_MODULU", null, "/stok", 7, null, null },
                    { 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1751), "ri-tools-line", "saha-malzeme", "MENU.SAHA_MALZEMESI", null, "/saha-malzeme", 8, null, null },
                    { 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1752), "ri-history-line", "hareket-gecmisi", "MENU.HAREKET_GECMISI", null, "/hareket-gecmisi", 9, null, null },
                    { 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1753), "ri-user-settings-line", "kullanicilar", "MENU.KULLANICI_YETKI", null, "/kullanicilar", 10, null, null },
                    { 13, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1754), "ri-shield-user-line", "rol-yonetimi", "MENU.ROL_YONETIMI", null, "/rol-yonetimi", 11, null, null },
                    { 99, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1848), "ri-check-double-line", "islem-onay-merkezi", "MENU.ISLEM_ONAY", null, "/onay-merkezi", 12, null, null }
                });

            migrationBuilder.InsertData(
                table: "Roller",
                columns: new[] { "Id", "Ad", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Admin", null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(250), null, null },
                    { 2, "Personel3K", null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(502), null, null },
                    { 3, "PersonelGrid", null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(504), null, null },
                    { 4, "Yonetici", null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(505), null, null }
                });

            migrationBuilder.InsertData(
                table: "IslemOnayKurallari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "LookupUcKDurumId", "OnayGerektirirMi", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5021), 8, true, null, null },
                    { 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5291), 9, true, null, null },
                    { 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5292), 10, true, null, null },
                    { 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5292), 2, false, null, null },
                    { 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5293), 3, false, null, null },
                    { 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5294), 4, false, null, null },
                    { 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5295), 11, false, null, null },
                    { 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5296), 12, false, null, null },
                    { 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 476, DateTimeKind.Utc).AddTicks(5296), 13, false, null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1843), "", "aktif-projeler", "MENU.AKTIF_PROJELER", 2, "/projeler", 1, null, null },
                    { 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1844), "", "sevk-edilen", "MENU.SEVK_EDILEN", 2, "/projeler/sevk-edilen", 2, null, null },
                    { 14, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1846), "", "grid-modulu", "MENU.GRID_MODULU", 2, null, 3, null, null },
                    { 15, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(1847), "", "3k-modulu", "MENU.3K_MODULU", 2, null, 4, null, null }
                });

            migrationBuilder.InsertData(
                table: "RolYetkileri",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "MenuTanimiId", "RolId", "UpdatedBy", "UpdatedDate", "YetkiTipiId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2039), 1, 1, null, null, 3 },
                    { 2, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2364), 2, 1, null, null, 3 },
                    { 5, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2365), 5, 1, null, null, 3 },
                    { 6, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2367), 6, 1, null, null, 3 },
                    { 7, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2368), 7, 1, null, null, 3 },
                    { 8, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2368), 8, 1, null, null, 3 },
                    { 9, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2368), 9, 1, null, null, 3 },
                    { 10, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2370), 10, 1, null, null, 3 },
                    { 11, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2370), 11, 1, null, null, 3 },
                    { 12, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2370), 12, 1, null, null, 3 },
                    { 13, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2371), 13, 1, null, null, 3 },
                    { 99, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2372), 99, 1, null, null, 3 },
                    { 3, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2365), 3, 1, null, null, 3 },
                    { 4, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2365), 4, 1, null, null, 3 },
                    { 14, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2371), 14, 1, null, null, 3 },
                    { 15, null, new DateTime(2026, 4, 21, 7, 1, 4, 477, DateTimeKind.Utc).AddTicks(2371), 15, 1, null, null, 3 }
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
                name: "IX_CekiSatirlari_DurumId",
                table: "CekiSatirlari",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_GeriGonderilmeSebebiId",
                table: "CekiSatirlari",
                column: "GeriGonderilmeSebebiId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_GridDurumuId",
                table: "CekiSatirlari",
                column: "GridDurumuId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_GridPersonelId",
                table: "CekiSatirlari",
                column: "GridPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KaynakProjeId",
                table: "CekiSatirlari",
                column: "KaynakProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KontrolEdenId",
                table: "CekiSatirlari",
                column: "KontrolEdenId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_PaketleyenId",
                table: "CekiSatirlari",
                column: "PaketleyenId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_UcKDurumuId",
                table: "CekiSatirlari",
                column: "UcKDurumuId");

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_KullaniciId",
                table: "HareketGecmisleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_ProjeId",
                table: "HareketGecmisleri",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_IslemOnayKurallari_LookupUcKDurumId",
                table: "IslemOnayKurallari",
                column: "LookupUcKDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Email",
                table: "Kullanicilar",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupDepoLokasyonlari_Deger",
                table: "LookupDepoLokasyonlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupGeriGonderilmeSebepleri_Deger",
                table: "LookupGeriGonderilmeSebepleri",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupGridDurumlari_Deger",
                table: "LookupGridDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupIslemTipleri_Deger",
                table: "LookupIslemTipleri",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupProjeDurumlari_Deger",
                table: "LookupProjeDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupSandikDurumlari_Deger",
                table: "LookupSandikDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupSandikTipleri_Deger",
                table: "LookupSandikTipleri",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupStokDurumlari_Deger",
                table: "LookupStokDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupUcKDurumlari_Deger",
                table: "LookupUcKDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupUrunDurumlari_Deger",
                table: "LookupUrunDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupYetkiTipleri_Deger",
                table: "LookupYetkiTipleri",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuTanimlari_Kod",
                table: "MenuTanimlari",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuTanimlari_ParentId",
                table: "MenuTanimlari",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnayBekleyenIslemler_OnaylayanKullaniciId",
                table: "OnayBekleyenIslemler",
                column: "OnaylayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_OnayBekleyenIslemler_TalepEdenKullaniciId",
                table: "OnayBekleyenIslemler",
                column: "TalepEdenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_DurumId",
                table: "Projeler",
                column: "DurumId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_KullaniciId",
                table: "Revizyonlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Revizyonlar_ProjeId",
                table: "Revizyonlar",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_MenuTanimiId",
                table: "RolYetkileri",
                column: "MenuTanimiId");

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_RolId_MenuTanimiId",
                table: "RolYetkileri",
                columns: new[] { "RolId", "MenuTanimiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_YetkiTipiId",
                table: "RolYetkileri",
                column: "YetkiTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_SandikIcerikleri_CekiSatiriId",
                table: "SandikIcerikleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_SandikIcerikleri_SandikId",
                table: "SandikIcerikleri",
                column: "SandikId");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_DepoLokasyonId",
                table: "Sandiklar",
                column: "DepoLokasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_DurumId",
                table: "Sandiklar",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_ProjeId_SandikNo",
                table: "Sandiklar",
                columns: new[] { "ProjeId", "SandikNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sandiklar_TipId",
                table: "Sandiklar",
                column: "TipId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_CekiSatiriId",
                table: "StokHareketleri",
                column: "CekiSatiriId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_IslemTipiId",
                table: "StokHareketleri",
                column: "IslemTipiId");

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
                name: "IX_StokKayitlari_DurumId",
                table: "StokKayitlari",
                column: "DurumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HareketGecmisleri");

            migrationBuilder.DropTable(
                name: "IslemOnayKurallari");

            migrationBuilder.DropTable(
                name: "OnayBekleyenIslemler");

            migrationBuilder.DropTable(
                name: "ProjeTransferleri");

            migrationBuilder.DropTable(
                name: "Revizyonlar");

            migrationBuilder.DropTable(
                name: "RolYetkileri");

            migrationBuilder.DropTable(
                name: "SandikIcerikleri");

            migrationBuilder.DropTable(
                name: "StokHareketleri");

            migrationBuilder.DropTable(
                name: "LookupYetkiTipleri");

            migrationBuilder.DropTable(
                name: "MenuTanimlari");

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
                name: "LookupGeriGonderilmeSebepleri");

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
                name: "Roller");

            migrationBuilder.DropTable(
                name: "LookupProjeDurumlari");
        }
    }
}
