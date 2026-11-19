using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Data;

/// <summary>
/// Yükleme Simülasyonu ve Araç Planlama: ekipman kitaplığı ve yükleme planı eşlemeleri
/// (20261105_02_Yukleme_Planlama.sql aynı şemayı kurar; 20261119_01_Yukleme_Arac_Aileleri.sql tip sadelik sırasını ve
/// Lowbed / ağır yük ailesini ekler).
/// </summary>
internal static class YuklemeModelBuilderExtensions
{
    public static void ConfigureYuklemePlanlamaModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EkipmanAilesi>(e =>
        {
            e.ToTable("EkipmanAileleri");
            e.Property(p => p.Kod).HasMaxLength(EkipmanKitapligiKurali.KodEnFazla).IsRequired();
            e.Property(p => p.Ad).HasMaxLength(EkipmanKitapligiKurali.AdEnFazla).IsRequired();
            e.HasIndex(p => p.Kod).IsUnique();
        });

        modelBuilder.Entity<EkipmanTipi>(e =>
        {
            e.ToTable("EkipmanTipleri", table => table.HasCheckConstraint(
                "CK_EkipmanTipleri_OtomatikSecimSirasi", "\"OtomatikSecimSirasi\" IS NULL OR \"OtomatikSecimSirasi\" > 0"));
            e.Property(p => p.Kod).HasMaxLength(EkipmanKitapligiKurali.KodEnFazla).IsRequired();
            e.Property(p => p.Ad).HasMaxLength(EkipmanKitapligiKurali.AdEnFazla).IsRequired();
            e.Property(p => p.Kategori).HasConversion<short>();
            e.Property(p => p.Aktif).HasDefaultValue(true);
            e.HasIndex(p => p.Kod).IsUnique();
            e.HasOne(p => p.Aile)
                .WithMany(a => a.Tipler)
                .HasForeignKey(p => p.AileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EkipmanModeli>(e =>
        {
            e.ToTable("EkipmanModelleri");
            e.Property(p => p.Uretici).HasMaxLength(EkipmanKitapligiKurali.UreticiEnFazla).IsRequired();
            e.Property(p => p.ModelAdi).HasMaxLength(EkipmanKitapligiKurali.ModelEnFazla).IsRequired();
            e.HasIndex(p => new { p.TipId, p.Uretici, p.ModelAdi }).IsUnique();
            e.HasOne(p => p.Tip)
                .WithMany()
                .HasForeignKey(p => p.TipId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EkipmanKonfigurasyonu>(e =>
        {
            e.ToTable("EkipmanKonfigurasyonlari", table => table.HasCheckConstraint(
                "CK_EkipmanKonfigurasyonlari_SonRevizyonNo_NonNegative", "\"SonRevizyonNo\" >= 0"));
            e.Property(p => p.Kod).HasMaxLength(EkipmanKitapligiKurali.KodEnFazla).IsRequired();
            e.Property(p => p.Ad).HasMaxLength(EkipmanKitapligiKurali.AdEnFazla).IsRequired();
            e.Property(p => p.Aktif).HasDefaultValue(true);
            // Npgsql, uint + IsRowVersion eşleşmesini PostgreSQL xmin sistem kolonuna map eder.
            e.Property(p => p.Version).IsRowVersion();
            e.HasIndex(p => p.Kod).IsUnique();
            e.HasOne(p => p.Model)
                .WithMany(m => m.Konfigurasyonlar)
                .HasForeignKey(p => p.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EkipmanKonfigurasyonRevizyonu>(e =>
        {
            e.ToTable("EkipmanKonfigurasyonRevizyonlari", table =>
            {
                table.HasCheckConstraint(
                    "CK_EkipmanKonfigurasyonRevizyonlari_TeknikVeriJson_Nesne",
                    "jsonb_typeof(\"TeknikVeriJson\") = 'object'");
                table.HasCheckConstraint(
                    "CK_EkipmanKonfigurasyonRevizyonlari_RevizyonNo_Pozitif", "\"RevizyonNo\" > 0");
            });
            e.Property(p => p.TeknikVeriJson).HasColumnType("jsonb").IsRequired();
            e.Property(p => p.KaynakTuru).HasConversion<short>();
            e.Property(p => p.KaynakAciklama).HasMaxLength(EkipmanKitapligiKurali.KaynakAciklamaEnFazla);
            e.Property(p => p.Aciklama).HasMaxLength(EkipmanKitapligiKurali.AciklamaEnFazla);
            e.Property(p => p.DogrulamaDurumu).HasConversion<short>();
            e.HasIndex(p => new { p.KonfigurasyonId, p.RevizyonNo }).IsUnique();
            e.HasOne(p => p.Konfigurasyon)
                .WithMany(k => k.Revizyonlar)
                .HasForeignKey(p => p.KonfigurasyonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<YuklemePlani>(e =>
        {
            e.ToTable("YuklemePlanlari", table => table.HasCheckConstraint(
                "CK_YuklemePlanlari_SonRevizyonNo_NonNegative", "\"SonRevizyonNo\" >= 0"));
            e.Property(p => p.PlanNo).HasMaxLength(20).IsRequired();
            e.Property(p => p.Ad).HasMaxLength(YuklemePlaniKurali.AdEnFazla).IsRequired();
            e.Property(p => p.CalismaBicimi).HasConversion<short>();
            e.Property(p => p.ProjeKarisimi).HasConversion<short>();
            e.Property(p => p.LokasyonTercihi).HasConversion<short>();
            e.Property(p => p.Aciklama).HasMaxLength(YuklemePlaniKurali.AciklamaEnFazla);
            e.Property(p => p.Version).IsRowVersion();
            e.HasIndex(p => p.PlanNo).IsUnique();
            e.HasOne(p => p.TasimaAilesi)
                .WithMany()
                .HasForeignKey(p => p.TasimaAilesiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<YuklemePlanRevizyonu>(e =>
        {
            e.ToTable("YuklemePlanRevizyonlari", table =>
            {
                table.HasCheckConstraint(
                    "CK_YuklemePlanRevizyonlari_AyarlarJson_Nesne", "jsonb_typeof(\"AyarlarJson\") = 'object'");
                table.HasCheckConstraint("CK_YuklemePlanRevizyonlari_RevizyonNo_Pozitif", "\"RevizyonNo\" > 0");
                table.HasCheckConstraint(
                    "CK_YuklemePlanRevizyonlari_YerlesimJson_Nesne", "\"YerlesimJson\" IS NULL OR jsonb_typeof(\"YerlesimJson\") = 'object'");
            });
            e.Property(p => p.Aciklama).HasMaxLength(YuklemePlaniKurali.AciklamaEnFazla);
            e.Property(p => p.AyarlarJson).HasColumnType("jsonb").IsRequired();
            e.Property(p => p.YerlesimJson).HasColumnType("jsonb");
            e.HasIndex(p => new { p.PlanId, p.RevizyonNo }).IsUnique();
            e.HasOne(p => p.Plan)
                .WithMany(p => p.Revizyonlar)
                .HasForeignKey(p => p.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<YuklemePlanProjesi>(e =>
        {
            e.ToTable("YuklemePlanProjeleri");
            e.Property(p => p.ProjeNo).IsRequired();
            e.HasIndex(p => new { p.RevizyonId, p.ProjeId }).IsUnique();
            e.HasOne(p => p.Revizyon)
                .WithMany(r => r.Projeler)
                .HasForeignKey(p => p.RevizyonId)
                .OnDelete(DeleteBehavior.Cascade);
            // Proje silinirse bağ boşalır; plan numara kopyasıyla okunur.
            e.HasOne(p => p.Proje)
                .WithMany()
                .HasForeignKey(p => p.ProjeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<YuklemePlanYuku>(e =>
        {
            e.ToTable("YuklemePlanYukleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_YuklemePlanYukleri_Birim", "\"BirimNo\" >= 1 AND \"BirimSayisi\" >= \"BirimNo\"");
                // Sandık yükünün grup numarası yoktur; elle eklenen yükün grubu vardır ve sandığa bağlanmaz.
                table.HasCheckConstraint(
                    "CK_YuklemePlanYukleri_Kaynak",
                    "(\"Kaynak\" = 1 AND \"ManuelGrupNo\" IS NULL) OR " +
                    "(\"Kaynak\" = 2 AND \"ManuelGrupNo\" IS NOT NULL AND \"SandikId\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_YuklemePlanYukleri_UstYukSiniri_NonNegative",
                    "\"UstYukSiniriKg\" IS NULL OR \"UstYukSiniriKg\" >= 0");
            });
            e.Property(p => p.Kaynak).HasConversion<short>();
            e.Property(p => p.Kod).IsRequired();
            e.Property(p => p.ElleDegisenIzinler).HasConversion<short>();
            e.Property(p => p.OzelTasimaNotu).HasMaxLength(YuklemePlaniKurali.OzelTasimaNotuEnFazla);
            e.HasIndex(p => new { p.RevizyonId, p.SiraNo }).IsUnique();
            e.HasIndex(p => new { p.RevizyonId, p.SandikId })
                .IsUnique()
                .HasFilter("\"SandikId\" IS NOT NULL");
            e.HasIndex(p => new { p.RevizyonId, p.ManuelGrupNo, p.BirimNo })
                .IsUnique()
                .HasFilter("\"ManuelGrupNo\" IS NOT NULL");
            e.HasOne(p => p.Revizyon)
                .WithMany(r => r.Yukler)
                .HasForeignKey(p => p.RevizyonId)
                .OnDelete(DeleteBehavior.Cascade);
            // Kaynak silinirse kopya kalır: sandık, proje ve lokasyon bağı boşalır.
            e.HasOne(p => p.Sandik)
                .WithMany()
                .HasForeignKey(p => p.SandikId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Proje)
                .WithMany()
                .HasForeignKey(p => p.ProjeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.YukTuruLookup)
                .WithMany()
                .HasForeignKey(p => p.YukTuruId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.LokasyonLookup)
                .WithMany()
                .HasForeignKey(p => p.LokasyonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<YuklemeOturumu>(e =>
        {
            e.ToTable("YuklemeOturumlari", table =>
            {
                table.HasCheckConstraint("CK_YuklemeOturumlari_Durum", "\"Durum\" IN (1, 2, 3)");
                // Açık oturumun kapanış zamanı yoktur; kapanan oturumunki vardır.
                table.HasCheckConstraint("CK_YuklemeOturumlari_Kapanis", "(\"Durum\" = 1) = (\"KapanisTarihi\" IS NULL)");
                table.HasCheckConstraint("CK_YuklemeOturumlari_Sayilar", "\"AracSayisi\" >= 0 AND \"ToplamYuk\" >= 0");
            });
            e.Property(p => p.Durum).HasConversion<short>();
            e.Property(p => p.KapanisGerekcesi).HasMaxLength(YuklemeOturumuKurali.GerekceEnFazla);
            e.Property(p => p.Version).IsRowVersion();
            // Planda aynı anda tek açık oturum (iki kişi aynı anda başlatırsa biri reddedilir).
            e.HasIndex(p => p.PlanId).IsUnique().HasFilter("\"Durum\" = 1");
            // Yükleme kaydı olan plan ve revizyon silinemez.
            e.HasOne(p => p.Plan)
                .WithMany()
                .HasForeignKey(p => p.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Revizyon)
                .WithMany()
                .HasForeignKey(p => p.RevizyonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<YuklemeOturumYuku>(e =>
        {
            e.ToTable("YuklemeOturumYukleri", table =>
            {
                table.HasCheckConstraint("CK_YuklemeOturumYukleri_AracNo_Pozitif", "\"AracNo\" > 0");
                table.HasCheckConstraint("CK_YuklemeOturumYukleri_Kaynak", "\"Kaynak\" IN (1, 2)");
                // Geri alınan işaretin zamanı ve gerekçesi birlikte dolar.
                table.HasCheckConstraint(
                    "CK_YuklemeOturumYukleri_GeriAlma", "(\"GeriAlinmaTarihi\" IS NULL) = (\"GeriAlmaGerekcesi\" IS NULL)");
            });
            e.Property(p => p.YukAnahtari).HasMaxLength(YuklemeOturumuKurali.YukAnahtariEnFazla).IsRequired();
            e.Property(p => p.Kod).IsRequired();
            e.Property(p => p.Kaynak).HasConversion<short>();
            e.Property(p => p.GeriAlmaGerekcesi).HasMaxLength(YuklemeOturumuKurali.GerekceEnFazla);
            // Oturumda bir yükün en çok bir etkin işareti olur (iki tablet aynı yükü okutursa tek kayıt kalır).
            e.HasIndex(p => new { p.OturumId, p.YukAnahtari }).IsUnique().HasFilter("\"GeriAlinmaTarihi\" IS NULL");
            e.HasIndex(p => new { p.OturumId, p.AracNo });
            e.HasOne(p => p.Oturum)
                .WithMany(o => o.Yukler)
                .HasForeignKey(p => p.OturumId)
                .OnDelete(DeleteBehavior.Cascade);
            // Sandık silinirse bağ boşalır; kod kopyası kalır.
            e.HasOne(p => p.Sandik)
                .WithMany()
                .HasForeignKey(p => p.SandikId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Taşıma aileleri ve tipleri (isterler 9, 11). Betik aynı kimliklerle tohumlar; yeni tip veritabanından eklenir.
    /// Teknik veri tohumlanmaz: gerçek değerler kaynağıyla kitaplığa girilir (isterler 31).
    /// </summary>
    public static void SeedYuklemePlanlamaData(this ModelBuilder modelBuilder)
    {
        var tarih = new DateTime(2026, 11, 5);
        modelBuilder.Entity<EkipmanAilesi>().HasData(EkipmanTohumu.Aileler.Select(a =>
            new EkipmanAilesi { Id = a.Id, Kod = a.Kod, Ad = a.Ad, Sira = a.Sira, CreatedDate = tarih }));
        modelBuilder.Entity<EkipmanTipi>().HasData(EkipmanTohumu.Tipler.Select((t, i) =>
            new EkipmanTipi
            {
                Id = t.Id, AileId = t.AileId, Kod = t.Kod, Ad = t.Ad, Kategori = t.Kategori, Sira = i + 1, Aktif = true,
                OtomatikSecimSirasi = t.OtomatikSecimSirasi, CreatedDate = tarih
            }));
    }
}

/// <summary>
/// Tohum aileler ve tipler; betikler (20261105_02 kurar, 20261119_01 lowbed ailesini ayırır ve sadelik sırasını yazar) aynı
/// kimlik, kod ve değerleri yazar. Lowbed ve ağır yük tipleri kendi ailesindedir (saha geri bildirimi 19.11.2026 madde 3):
/// standart açık kasa seçilince lowbed gelmez.
/// </summary>
internal static class EkipmanTohumu
{
    public static readonly (int Id, string Kod, string Ad, int Sira)[] Aileler =
    [
        (1, "konteyner", "Konteyner", 1),
        (2, "acik-platform", "Standart açık kasa / platform", 2),
        (8, "lowbed-agir-yuk", "Lowbed / ağır yük", 3),
        (3, "tenteli", "Tenteli", 4),
        (4, "kapali-kasa", "Kapalı kasa / frigo", 5),
        (5, "konteyner-sasisi", "Konteyner şasisi", 6),
        (6, "dokme-tanker", "Damper / hareketli taban / silobas / tanker", 7),
        (7, "diger", "Diğer özel amaçlı", 8)
    ];

    private static readonly (int Id, int AileId, string Kod, string Ad, EkipmanKategorisi Kategori)[] TipTanimlari =
    [
        (1, 1, "k20-dv", "20' standart (DV)", EkipmanKategorisi.KapaliKonteyner),
        (2, 1, "k40-dv", "40' standart (DV)", EkipmanKategorisi.KapaliKonteyner),
        (3, 1, "k40-hc", "40' High Cube", EkipmanKategorisi.KapaliKonteyner),
        (4, 1, "k45-hc", "45' High Cube", EkipmanKategorisi.KapaliKonteyner),
        (5, 1, "k45-pw", "45' Pallet Wide High Cube", EkipmanKategorisi.KapaliKonteyner),
        (6, 1, "k20-ot", "20' Open Top", EkipmanKategorisi.OpenTopKonteyner),
        (7, 1, "k40-ot", "40' Open Top", EkipmanKategorisi.OpenTopKonteyner),
        (8, 1, "k20-fr", "20' Flat Rack", EkipmanKategorisi.FlatRack),
        (9, 1, "k40-fr", "40' Flat Rack", EkipmanKategorisi.FlatRack),
        (10, 1, "k20-pl", "20' Platform", EkipmanKategorisi.PlatformKonteyner),
        (11, 1, "k40-pl", "40' Platform", EkipmanKategorisi.PlatformKonteyner),
        (12, 1, "k20-rf", "20' Reefer", EkipmanKategorisi.KapaliKonteyner),
        (13, 1, "k40-rf", "40' Reefer High Cube", EkipmanKategorisi.KapaliKonteyner),
        (14, 1, "k20-ht", "20' Hardtop", EkipmanKategorisi.HardtopKonteyner),
        (15, 1, "k40-ht", "40' Hardtop", EkipmanKategorisi.HardtopKonteyner),
        (16, 1, "k-diger", "Diğer özel konteyner", EkipmanKategorisi.Diger),
        (17, 2, "acik-kasa", "Açık kasa", EkipmanKategorisi.AcikKasa),
        (18, 2, "kapakli-acik-kasa", "Kapaklı açık kasa", EkipmanKategorisi.AcikKasa),
        (19, 2, "sal-platform", "Sal platform", EkipmanKategorisi.Platform),
        (20, 2, "uzamali-platform", "Uzamalı platform", EkipmanKategorisi.UzamaliPlatform),
        (21, 2, "lowbed", "Lowbed", EkipmanKategorisi.Lowbed),
        (22, 2, "low-loader", "Low-loader", EkipmanKategorisi.Lowbed),
        (23, 2, "uzamali-agir-yuk", "Uzamalı ağır yük dorsesi", EkipmanKategorisi.AgirYukDorsesi),
        (24, 2, "havuzlu", "Havuzlu ağır yük dorsesi", EkipmanKategorisi.AgirYukDorsesi),
        (25, 2, "hidrolik-dumenli", "Hidrolik dümenlenir ağır yük dorsesi", EkipmanKategorisi.AgirYukDorsesi),
        (26, 2, "moduler", "Modüler ağır yük taşıyıcısı", EkipmanKategorisi.ModulerAgirYuk),
        (27, 3, "tenteli-standart", "Standart tenteli", EkipmanKategorisi.Tenteli),
        (28, 3, "tenteli-mega", "Mega tenteli", EkipmanKategorisi.Tenteli),
        (29, 3, "tenteli-jumbo", "Jumbo tenteli", EkipmanKategorisi.Tenteli),
        (30, 4, "kapali-kasa", "Kapalı kasa", EkipmanKategorisi.KapaliKasa),
        (31, 4, "frigo", "Frigo", EkipmanKategorisi.Frigo),
        (32, 5, "konteyner-sasisi", "Konteyner şasisi", EkipmanKategorisi.KonteynerSasisi),
        (33, 6, "damper", "Damper", EkipmanKategorisi.Damper),
        (34, 6, "hareketli-taban", "Hareketli taban", EkipmanKategorisi.HareketliTaban),
        (35, 6, "silobas", "Silobas", EkipmanKategorisi.Silobas),
        (36, 6, "tanker", "Tanker", EkipmanKategorisi.Tanker),
        (37, 7, "diger-ozel", "Diğer özel amaçlı taşıyıcı", EkipmanKategorisi.Diger)
    ];

    /// <summary>
    /// Otomatik yerleşimdeki sadelik sırası (EkipmanTipi.OtomatikSecimSirasi). Listede olmayan tip yalnız açıkça seçilince
    /// kullanılır (Reefer, Pallet Wide, frigo, modüler, diğer).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> OtomatikSecimSiralari = new Dictionary<string, int>
    {
        ["k20-dv"] = 1, ["k40-dv"] = 1, ["k40-hc"] = 2, ["k45-hc"] = 2, ["k20-ht"] = 3, ["k40-ht"] = 3,
        ["k20-ot"] = 4, ["k40-ot"] = 4, ["k20-fr"] = 5, ["k40-fr"] = 5, ["k20-pl"] = 6, ["k40-pl"] = 6,
        ["acik-kasa"] = 1, ["kapakli-acik-kasa"] = 1, ["sal-platform"] = 2, ["uzamali-platform"] = 3,
        ["lowbed"] = 1, ["low-loader"] = 1, ["uzamali-agir-yuk"] = 2, ["havuzlu"] = 2, ["hidrolik-dumenli"] = 3,
        ["tenteli-standart"] = 1, ["tenteli-mega"] = 2, ["tenteli-jumbo"] = 2,
        ["kapali-kasa"] = 1
    };

    /// <summary>Lowbed ve ağır yük tiplerinin ailesi (20261119_01 bu tipleri 2'den 8'e taşır).</summary>
    public const int LowbedAilesiId = 8;

    private static readonly HashSet<string> LowbedAilesiTipleri =
        ["lowbed", "low-loader", "uzamali-agir-yuk", "havuzlu", "hidrolik-dumenli", "moduler"];

    public static readonly (int Id, int AileId, string Kod, string Ad, EkipmanKategorisi Kategori, int? OtomatikSecimSirasi)[] Tipler =
        TipTanimlari.Select(t => (t.Id, LowbedAilesiTipleri.Contains(t.Kod) ? LowbedAilesiId : t.AileId, t.Kod, t.Ad, t.Kategori,
            OtomatikSecimSiralari.TryGetValue(t.Kod, out var sira) ? (int?)sira : null)).ToArray();
}
