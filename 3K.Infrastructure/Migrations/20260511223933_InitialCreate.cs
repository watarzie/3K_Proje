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
                name: "LookupBirimler",
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
                    table.PrimaryKey("PK_LookupBirimler", x => x.Id);
                });

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
                name: "LookupGridSevkDurumlari",
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
                    table.PrimaryKey("PK_LookupGridSevkDurumlari", x => x.Id);
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
                name: "LookupKaliteDurumlari",
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
                    table.PrimaryKey("PK_LookupKaliteDurumlari", x => x.Id);
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
                name: "LookupProjeTipleri",
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
                    table.PrimaryKey("PK_LookupProjeTipleri", x => x.Id);
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
                name: "LookupSurecDurumlari",
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
                    table.PrimaryKey("PK_LookupSurecDurumlari", x => x.Id);
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
                    ProjeTipiId = table.Column<int>(type: "integer", nullable: false),
                    PlanlananSevkTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GerceklesenSevkTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Projeler_LookupProjeTipleri_ProjeTipiId",
                        column: x => x.ProjeTipiId,
                        principalTable: "LookupProjeTipleri",
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
                    Miktar = table.Column<decimal>(type: "numeric", nullable: false),
                    BirimId = table.Column<int>(type: "integer", nullable: false),
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
                    Ad = table.Column<string>(type: "text", nullable: true),
                    TipId = table.Column<int>(type: "integer", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    SevkOncesiDurumId = table.Column<int>(type: "integer", nullable: true),
                    DepoLokasyonId = table.Column<int>(type: "integer", nullable: false),
                    En = table.Column<decimal>(type: "numeric", nullable: true),
                    Boy = table.Column<decimal>(type: "numeric", nullable: true),
                    Yukseklik = table.Column<decimal>(type: "numeric", nullable: true),
                    NetKg = table.Column<decimal>(type: "numeric", nullable: true),
                    GrossKg = table.Column<decimal>(type: "numeric", nullable: true),
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
                    IslemTipiId = table.Column<int>(type: "integer", nullable: true),
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
                        name: "FK_HareketGecmisleri_LookupIslemTipleri_IslemTipiId",
                        column: x => x.IslemTipiId,
                        principalTable: "LookupIslemTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    IstenenAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BirimId = table.Column<int>(type: "integer", nullable: false),
                    CekideGecenSandikNo = table.Column<string>(type: "text", nullable: false),
                    FiiliSandikNo = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    GridDurumuId = table.Column<int>(type: "integer", nullable: false),
                    GridGelenAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TrafoSevkAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GridSevkDurumuId = table.Column<int>(type: "integer", nullable: false),
                    GridSevkMiktari = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    YenidenSevkGerekliAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GridSevkTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GridAciklama = table.Column<string>(type: "text", nullable: true),
                    GridPersonelId = table.Column<int>(type: "integer", nullable: true),
                    UcKDurumuId = table.Column<int>(type: "integer", nullable: false),
                    UcKKarsilamaTipiId = table.Column<int>(type: "integer", nullable: false),
                    GelenMiktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TeslimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    KaynakHedefProjeNo = table.Column<string>(type: "text", nullable: true),
                    UcKAciklama = table.Column<string>(type: "text", nullable: true),
                    KarsilananMiktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    StokKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProjeKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProjeGonderilen = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TedarikciKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    HataliMiktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GeriGonderilenMiktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GeriGonderilmeSebebiId = table.Column<int>(type: "integer", nullable: true),
                    KaynakProjeId = table.Column<int>(type: "integer", nullable: true),
                    KaliteDurumId = table.Column<int>(type: "integer", nullable: true),
                    SurecDurumId = table.Column<int>(type: "integer", nullable: true),
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
                        name: "FK_CekiSatirlari_LookupBirimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "LookupBirimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_CekiSatirlari_LookupGridSevkDurumlari_GridSevkDurumuId",
                        column: x => x.GridSevkDurumuId,
                        principalTable: "LookupGridSevkDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupKaliteDurumlari_KaliteDurumId",
                        column: x => x.KaliteDurumId,
                        principalTable: "LookupKaliteDurumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CekiSatirlari_LookupSurecDurumlari_SurecDurumId",
                        column: x => x.SurecDurumId,
                        principalTable: "LookupSurecDurumlari",
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
                    UrunAdi = table.Column<string>(type: "text", nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TransferTipiId = table.Column<int>(type: "integer", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    ParentTransferId = table.Column<int>(type: "integer", nullable: true),
                    RootTransferId = table.Column<int>(type: "integer", nullable: true),
                    ZincirSeviyesi = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IptalTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IptalAciklama = table.Column<string>(type: "text", nullable: true),
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
                        name: "FK_ProjeTransferleri_ProjeTransferleri_ParentTransferId",
                        column: x => x.ParentTransferId,
                        principalTable: "ProjeTransferleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjeTransferleri_ProjeTransferleri_RootTransferId",
                        column: x => x.RootTransferId,
                        principalTable: "ProjeTransferleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    CekiSatiriId = table.Column<int>(type: "integer", nullable: true),
                    KonulanAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EksikAdet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    StokKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProjeKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TedarikciKarsilanan = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BarkodNo = table.Column<string>(type: "text", nullable: true),
                    Isim = table.Column<string>(type: "text", nullable: true),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    BirimId = table.Column<int>(type: "integer", nullable: true),
                    KaynakProjeNo = table.Column<string>(type: "text", nullable: true),
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
                        name: "FK_SandikIcerikleri_LookupBirimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "LookupBirimler",
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
                    Miktar = table.Column<decimal>(type: "numeric", nullable: false),
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
                table: "LookupBirimler",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9315), "Adet", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9316), "Set", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9317), "Metre", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9317), "Kg", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9318), "Litre", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9319), "Takım", null, null },
                    { 7, 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9319), "Paket", null, null },
                    { 8, 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9320), "Ton", null, null },
                    { 9, 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9321), "Metrekare", null, null },
                    { 10, 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9321), "Metreküp", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupDepoLokasyonlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7480), "Belirsiz", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7482), "3K", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7482), "Seymen", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7483), "Grid", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGeriGonderilmeSebepleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8451), "Tadilat", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8452), "Iptal", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8453), "Projeye Geri Dönüş", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8462), "Hatalı Ürün", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGridDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7861), "Bekliyor", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7862), "Üretimde", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7863), "Stok Hazır", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7864), "Sevk Edildi", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7865), "Kısmi Sevk Edildi", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7865), "Bekletiliyor", null, null },
                    { 7, 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7875), "İptal Edildi", null, null },
                    { 8, 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7885), "Tam Geldi", null, null },
                    { 9, 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7886), "Eksik Geldi", null, null },
                    { 10, 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7887), "Gelmedi", null, null },
                    { 11, 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7887), "Trafo Sevk", null, null },
                    { 12, 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7888), "İptal", null, null },
                    { 14, 14, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7889), "Grid Kapandı", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupGridSevkDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8065), "Sevk Edildi", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8066), "Bekliyor", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8067), "Sevk Edilmedi", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8067), "Yeniden Sevk Gerekli", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupIslemTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8955), "Çeki Yüklendi", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8956), "Proje Oluşturuldu", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8957), "Grid Durum Güncellendi", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8957), "Grid Toplu Sevk Edildi", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8958), "3K Durum Güncellendi", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8959), "3K Teslim Alındı", null, null },
                    { 7, 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8959), "3K Toplu Teslim Alındı", null, null },
                    { 8, 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8960), "Manuel Ürün Eklendi", null, null },
                    { 9, 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8961), "Sandık Ürün Taşıma", null, null },
                    { 10, 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8961), "Ürün Güncellendi", null, null },
                    { 11, 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8962), "Ürün İptal Edildi", null, null },
                    { 12, 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8963), "Stoktan Karşılandı", null, null },
                    { 13, 13, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8963), "F.B.'den Karşılandı", null, null },
                    { 14, 14, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8964), "Sandık Manuel Kapatma", null, null },
                    { 15, 15, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8964), "Toplu Sandık Kapatıldı", null, null },
                    { 16, 16, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8965), "Fiili Sandık Değiştirildi", null, null },
                    { 17, 17, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8966), "Lokasyon Güncelleme", null, null },
                    { 18, 18, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8966), "Sandık Otomatik Hazırlandı", null, null },
                    { 19, 19, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8967), "Excel İndirildi", null, null },
                    { 20, 20, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8977), "PDF İndirildi", null, null },
                    { 21, 21, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8977), "Sandık Oluşturuldu", null, null },
                    { 22, 22, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8978), "Kullanıcı Oluşturuldu", null, null },
                    { 23, 23, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8979), "Proje Sevk Edildi", null, null },
                    { 24, 24, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8979), "Sandık Sevk Edildi", null, null },
                    { 25, 25, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8980), "Saha/Yedek Malzeme Eklendi", null, null },
                    { 26, 26, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8981), "Toplu Durum Güncellendi", null, null },
                    { 27, 27, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8981), "Not Eklendi", null, null },
                    { 28, 28, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8982), "Manuel Ürün Sandığa Eklendi", null, null },
                    { 29, 29, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8982), "Sandık Kapandı", null, null },
                    { 30, 30, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8983), "3K Durum Sıfırlandı", null, null },
                    { 31, 31, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8984), "Grid Durum Sıfırlandı", null, null },
                    { 32, 32, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8984), "Manuel Ürün Silindi", null, null },
                    { 33, 33, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8985), "Sandık Silindi", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupKaliteDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9476), "Onaylandı", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9476), "Tadilatta", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupProjeDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(2578), "Hazırlanıyor", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(3034), "Devam", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(3035), "Tamamlandı", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(3036), "Beklemede", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(3036), "Sevk Edildi", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(3037), "Eksik Sevk Edildi", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupProjeTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9159), "Normal", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9160), "Saha", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9161), "Yedek", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7059), "Boş", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7061), "Hazırlanıyor", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7062), "Kapandı", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7062), "Sevk Edildi", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupSandikTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7293), "Ahşap Kapalı", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7294), "Katlanır Sandık", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupStokDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8795), "Aktif", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8796), "Tukendi", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8797), "Rezerve", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupSurecDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9631), "Ambar", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9632), "İmalat", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9632), "Tedarik", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9633), "Tedarik 3K Teslim", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(9634), "Siparişte", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupUcKDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8283), "Bekliyor", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8285), "Sevk Adeti Tam Geldi", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8286), "Sevk Adeti Eksik Geldi", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8286), "Gelmedi", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8287), "Tamamlandı", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8288), "Kontrol Edildi", null, null },
                    { 7, 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8288), "İade Edildi", null, null },
                    { 8, 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8289), "Projeden Karşılandı", null, null },
                    { 9, 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8290), "Stoktan Karşılandı", null, null },
                    { 10, 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8290), "Tedarikçiden Geldi", null, null },
                    { 11, 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8291), "Başka Projeye Verildi", null, null },
                    { 12, 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8291), "Geri Gönderildi", null, null },
                    { 13, 13, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8292), "Hatalı Ürün", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupUrunDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7672), "Bekliyor", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7674), "Kısmi Geldi", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7674), "Tamamlandı", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7675), "Eksik", null, null },
                    { 5, 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7676), "Stoktan Karşılandı", null, null },
                    { 6, 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7676), "FB'den Karşılandı", null, null },
                    { 7, 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7677), "Sonra Gidecek", null, null },
                    { 8, 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7678), "Sandık Değişti", null, null },
                    { 9, 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7678), "İptal/Pasif", null, null },
                    { 10, 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7679), "Teslim Alındı", null, null },
                    { 11, 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7680), "Geri Gönderildi", null, null },
                    { 12, 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7680), "Kısmi Tamamlandı", null, null },
                    { 13, 13, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7681), "Kayıp", null, null },
                    { 14, 14, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7681), "Grid'de Hazır", null, null },
                    { 15, 15, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7682), "Grid'de Eksik", null, null },
                    { 16, 16, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7683), "Siparişte", null, null },
                    { 17, 17, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7683), "Gelmedi", null, null },
                    { 18, 18, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7684), "Trafo Sevk", null, null },
                    { 19, 19, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7684), "Başka Projeye Verildi", null, null },
                    { 20, 20, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7685), "Hatalı Ürün", null, null },
                    { 21, 21, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(7686), "Hatalı/Uyumsuz Gönderim", null, null }
                });

            migrationBuilder.InsertData(
                table: "LookupYetkiTipleri",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8641), "N", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8642), "R", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 340, DateTimeKind.Utc).AddTicks(8643), "W", null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7833), "ri-dashboard-line", "dashboard", "MENU.DASHBOARD", null, "/dashboard", 1, null, null },
                    { 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8570), "ri-folder-line", "projeler", "MENU.PROJELER", null, null, 2, null, null },
                    { 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8572), "ri-archive-line", "sandik-yonetimi", "MENU.SANDIK_YONETIMI", null, "/sandik-yonetimi", 3, null, null },
                    { 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8581), "ri-building-2-line", "depo-durumu", "MENU.DEPO_DURUMU", null, "/depo-durumu", 4, null, null },
                    { 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8595), "ri-stack-line", "stok", "MENU.STOK_MODULU", null, "/stok", 5, null, null },
                    { 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8596), "ri-history-line", "hareket-gecmisi", "MENU.HAREKET_GECMISI", null, "/hareket-gecmisi", 8, null, null },
                    { 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8597), "ri-user-settings-line", "kullanicilar", "MENU.KULLANICI_YETKI", null, "/kullanicilar", 9, null, null },
                    { 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8598), "ri-shield-user-line", "rol-yonetimi", "MENU.ROL_YONETIMI", null, "/rol-yonetimi", 10, null, null },
                    { 17, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8682), "ri-tools-line", "saha-yonetimi", "MENU.SAHA_YONETIMI", null, "/saha-yonetimi", 6, null, null },
                    { 18, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8684), "ri-box-3-line", "yedek-yonetimi", "MENU.YEDEK_YONETIMI", null, "/yedek-yonetimi", 7, null, null },
                    { 99, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8685), "ri-check-double-line", "islem-onay-merkezi", "MENU.ISLEM_ONAY", null, "/onay-merkezi", 11, null, null }
                });

            migrationBuilder.InsertData(
                table: "Roller",
                columns: new[] { "Id", "Ad", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Admin", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7159), null, null },
                    { 2, "Personel3K", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7360), null, null },
                    { 3, "PersonelGrid", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7362), null, null },
                    { 4, "Yonetici", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7363), null, null },
                    { 5, "Kalite", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7364), null, null },
                    { 6, "Surec", null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(7365), null, null }
                });

            migrationBuilder.InsertData(
                table: "IslemOnayKurallari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "LookupUcKDurumId", "OnayGerektirirMi", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1235), 8, true, null, null },
                    { 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1511), 9, true, null, null },
                    { 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1512), 10, true, null, null },
                    { 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1513), 2, false, null, null },
                    { 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1513), 3, false, null, null },
                    { 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1514), 4, false, null, null },
                    { 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1515), 11, false, null, null },
                    { 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1515), 12, false, null, null },
                    { 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(1516), 13, false, null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8676), "", "aktif-projeler", "MENU.AKTIF_PROJELER", 2, "/projeler", 1, null, null },
                    { 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8677), "", "sevk-edilen", "MENU.SEVK_EDILEN", 2, "/projeler/sevk-edilen", 2, null, null },
                    { 14, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8679), "", "grid-modulu", "MENU.GRID_MODULU", 5, null, 1, null, null },
                    { 15, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8680), "", "3k-modulu", "MENU.3K_MODULU", 5, null, 2, null, null },
                    { 16, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8681), "", "proje-sevk-et", "MENU.PROJE_SEVK_ET", 2, null, 5, null, null },
                    { 20, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8686), "", "kalite-modulu", "MENU.KALITE_MODULU", 5, null, 3, null, null },
                    { 21, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(8687), "", "surec-modulu", "MENU.SUREC_MODULU", 5, null, 4, null, null }
                });

            migrationBuilder.InsertData(
                table: "RolYetkileri",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "MenuTanimiId", "RolId", "UpdatedBy", "UpdatedDate", "YetkiTipiId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9014), 1, 1, null, null, 3 },
                    { 2, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9335), 2, 1, null, null, 3 },
                    { 5, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9337), 5, 1, null, null, 3 },
                    { 6, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9339), 7, 1, null, null, 3 },
                    { 7, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9339), 8, 1, null, null, 3 },
                    { 8, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9339), 10, 1, null, null, 3 },
                    { 9, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9340), 11, 1, null, null, 3 },
                    { 10, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9341), 12, 1, null, null, 3 },
                    { 14, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9342), 17, 1, null, null, 3 },
                    { 15, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9342), 18, 1, null, null, 3 },
                    { 99, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9344), 99, 1, null, null, 3 },
                    { 3, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9336), 3, 1, null, null, 3 },
                    { 4, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9337), 4, 1, null, null, 3 },
                    { 11, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9341), 14, 1, null, null, 3 },
                    { 12, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9341), 15, 1, null, null, 3 },
                    { 13, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9342), 16, 1, null, null, 3 },
                    { 16, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9343), 20, 1, null, null, 3 },
                    { 17, null, new DateTime(2026, 5, 11, 22, 39, 33, 341, DateTimeKind.Utc).AddTicks(9343), 21, 1, null, null, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cekiler_ProjeId",
                table: "Cekiler",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_BirimId",
                table: "CekiSatirlari",
                column: "BirimId");

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
                name: "IX_CekiSatirlari_GridSevkDurumuId",
                table: "CekiSatirlari",
                column: "GridSevkDurumuId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KaliteDurumId",
                table: "CekiSatirlari",
                column: "KaliteDurumId");

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
                name: "IX_CekiSatirlari_SurecDurumId",
                table: "CekiSatirlari",
                column: "SurecDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_UcKDurumuId",
                table: "CekiSatirlari",
                column: "UcKDurumuId");

            migrationBuilder.CreateIndex(
                name: "IX_HareketGecmisleri_IslemTipiId",
                table: "HareketGecmisleri",
                column: "IslemTipiId");

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
                name: "IX_LookupBirimler_Deger",
                table: "LookupBirimler",
                column: "Deger",
                unique: true);

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
                name: "IX_LookupGridSevkDurumlari_Deger",
                table: "LookupGridSevkDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupIslemTipleri_Deger",
                table: "LookupIslemTipleri",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupKaliteDurumlari_Deger",
                table: "LookupKaliteDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupProjeDurumlari_Deger",
                table: "LookupProjeDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupProjeTipleri_Deger",
                table: "LookupProjeTipleri",
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
                name: "IX_LookupSurecDurumlari_Deger",
                table: "LookupSurecDurumlari",
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
                name: "IX_Projeler_ProjeTipiId",
                table: "Projeler",
                column: "ProjeTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_DurumId",
                table: "ProjeTransferleri",
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
                name: "IX_ProjeTransferleri_ParentTransferId",
                table: "ProjeTransferleri",
                column: "ParentTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_RootTransferId",
                table: "ProjeTransferleri",
                column: "RootTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeTransferleri_TransferTipiId",
                table: "ProjeTransferleri",
                column: "TransferTipiId");

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
                name: "IX_SandikIcerikleri_BirimId",
                table: "SandikIcerikleri",
                column: "BirimId");

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
                name: "LookupBirimler");

            migrationBuilder.DropTable(
                name: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropTable(
                name: "LookupGridDurumlari");

            migrationBuilder.DropTable(
                name: "LookupGridSevkDurumlari");

            migrationBuilder.DropTable(
                name: "LookupKaliteDurumlari");

            migrationBuilder.DropTable(
                name: "LookupSurecDurumlari");

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

            migrationBuilder.DropTable(
                name: "LookupProjeTipleri");
        }
    }
}
