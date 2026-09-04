using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Infrastructure.Data;

internal static class ModuleModelBuilderExtensions
{
    public static void ConfigureAmbalajUretimModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AmbalajUretimKaydi>(entity =>
        {
            entity.ToTable("AmbalajUretimKayitlari", table =>
            {
                table.HasCheckConstraint("CK_AmbalajUretimKayitlari_Adet", "\"Adet\" > 0");
                table.HasCheckConstraint(
                    "CK_AmbalajUretimKayitlari_Olculer",
                    "\"Boy\" >= 0 AND \"En\" >= 0 AND \"Yukseklik\" >= 0");
                table.HasCheckConstraint(
                    "CK_AmbalajUretimKayitlari_SarfOrani",
                    "\"SarfOrani\" >= 0 AND \"SarfOrani\" <= 1");
                table.HasCheckConstraint(
                    "CK_AmbalajUretimKayitlari_M3",
                    "\"HesaplananBirimM3\" >= 0 AND \"HesaplananToplamM3\" >= 0 AND " +
                    "(\"M3Override\" IS NULL OR \"M3Override\" >= 0) AND \"SarfM3\" >= 0 AND \"ToplamM3\" >= 0");
                table.HasCheckConstraint(
                    "CK_AmbalajUretimKayitlari_Proje",
                    "\"ProjeId\" IS NOT NULL OR NULLIF(BTRIM(\"ManuelProjeNo\"), '') IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_AmbalajUretimKayitlari_UretimSecimi",
                    "NOT \"UretimeAlindi\" OR (\"AmbalajaDahil\" AND " +
                    "(\"M3Override\" IS NOT NULL OR (\"Adet\" > 0 AND CASE " +
                    "WHEN NOT \"BagimsizKayitMi\" AND \"KaynakKayitId\" IS NOT NULL AND \"KaynakModul\" IN (1, 2, 3) " +
                    "THEN \"Boy\" > 92 AND \"En\" > 92 AND \"Yukseklik\" > 255 " +
                    "ELSE \"Boy\" > 0 AND \"En\" > 0 AND \"Yukseklik\" > 0 END)))");
            });

            entity.HasIndex(x => x.IsAkisKimligi).IsUnique();
            entity.HasIndex(x => x.ProjeId);
            // Kaynak modül, proje türü değiştiğinde güncellenebilir. Aynı Sandik.Id
            // için modülden bağımsız tek üretim kaydı bulunmalıdır.
            entity.HasIndex(x => x.KaynakKayitId)
                .IsUnique()
                .HasFilter("\"KaynakKayitId\" IS NOT NULL");
            entity.HasIndex(x => new { x.IptalMi, x.UretimeAlindi, x.UretimDurumu });
            entity.HasIndex(x => new { x.BagimsizKayitMi, x.Tur, x.ProjeId });
            entity.HasIndex(x => x.UretimTarihi);

            entity.Property(x => x.ManuelProjeNo).HasMaxLength(100);
            entity.Property(x => x.ManuelProjeAdi).HasMaxLength(250);
            entity.Property(x => x.SandikNo).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Ad).HasMaxLength(250);
            entity.Property(x => x.DigerSandikCinsi).HasMaxLength(100);
            entity.Property(x => x.M3OverrideNedeni).HasMaxLength(500);
            entity.Property(x => x.M3HesaplamaVersiyonu).HasMaxLength(50).IsRequired();
            entity.Property(x => x.KullanimAmaci).HasMaxLength(250);
            entity.Property(x => x.TalepEdenKisi).HasMaxLength(200);
            entity.Property(x => x.TalepEdenBolum).HasMaxLength(200);
            entity.Property(x => x.TalimatVeren).HasMaxLength(200);
            entity.Property(x => x.FirinPartiNo).HasMaxLength(100);
            entity.Property(x => x.IptalNedeni).HasMaxLength(500);

            entity.Property(x => x.Boy).HasPrecision(18, 4);
            entity.Property(x => x.En).HasPrecision(18, 4);
            entity.Property(x => x.Yukseklik).HasPrecision(18, 4);
            entity.Property(x => x.HesaplananBirimM3).HasPrecision(18, 6);
            entity.Property(x => x.HesaplananToplamM3).HasPrecision(18, 6);
            entity.Property(x => x.M3Override).HasPrecision(18, 6);
            entity.Property(x => x.SarfOrani).HasPrecision(7, 6).HasDefaultValue(0.11m);
            entity.Property(x => x.SarfM3).HasPrecision(18, 6);
            entity.Property(x => x.ToplamM3).HasPrecision(18, 6);
            entity.Property(x => x.AmbalajaDahil).HasDefaultValue(true);
            entity.Property(x => x.BagimsizKayitMi).HasDefaultValue(false);
            entity.Property(x => x.UretimeAlindi).HasDefaultValue(false);
            entity.Property(x => x.KaynakSenkronizasyonuKilitliMi).HasDefaultValue(false);
            entity.Property(x => x.IptalMi).HasDefaultValue(false);
            entity.Property(x => x.Tur).HasConversion<int>();
            entity.Property(x => x.KaynakModul).HasConversion<int>();
            entity.Property(x => x.SandikCinsi).HasConversion<int>();
            entity.Property(x => x.UretimDurumu).HasConversion<int>();
            entity.Property(x => x.IptalOncesiUretimDurumu).HasConversion<int?>();

            entity.HasOne(x => x.Proje)
                .WithMany()
                .HasForeignKey(x => x.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UstKayit)
                .WithMany(x => x.IcKayitlar)
                .HasForeignKey(x => x.UstKayitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.IcSandikSablonu)
                .WithMany()
                .HasForeignKey(x => x.IcSandikSablonId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AmbalajUretimKayitlari_AmbalajIcSandikSablonlari_IcSandikSab");
        });

        modelBuilder.Entity<AmbalajUretimHareketi>(entity =>
        {
            entity.ToTable("AmbalajUretimHareketleri");
            entity.HasIndex(x => new { x.AmbalajUretimKaydiId, x.Tarih });
            entity.HasIndex(x => x.IslemGrubu);
            entity.Property(x => x.Islem).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AlanAdi).HasMaxLength(150).IsRequired();
            entity.Property(x => x.EskiDeger).HasColumnType("text");
            entity.Property(x => x.YeniDeger).HasColumnType("text");
            entity.Property(x => x.Aciklama).HasMaxLength(1000);

            entity.HasOne(x => x.AmbalajUretimKaydi)
                .WithMany(x => x.Hareketler)
                .HasForeignKey(x => x.AmbalajUretimKaydiId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Kullanici>()
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AmbalajIcSandikSablonu>(entity =>
        {
            entity.ToTable("AmbalajIcSandikSablonlari", table =>
            {
                table.HasCheckConstraint(
                    "CK_AmbalajIcSandikSablonlari_Olculer",
                    "\"Boy\" > 0 AND \"En\" > 0 AND \"Yukseklik\" > 0");
            });
            entity.HasIndex(x => x.Ad).IsUnique();
            entity.Property(x => x.Ad).HasMaxLength(150).IsRequired();
            entity.Property(x => x.SandikCinsi).HasConversion<int>();
            entity.Property(x => x.DigerSandikCinsi).HasMaxLength(100);
            entity.Property(x => x.Boy).HasPrecision(18, 4);
            entity.Property(x => x.En).HasPrecision(18, 4);
            entity.Property(x => x.Yukseklik).HasPrecision(18, 4);
        });

        modelBuilder.Entity<AmbalajTalepEden>(entity =>
        {
            entity.ToTable("AmbalajTalepEdenler");
            entity.HasIndex(x => x.Ad).IsUnique();
            entity.Property(x => x.Ad).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<AmbalajKaynakSenkronizasyonKuyrukKaydi>(entity =>
        {
            entity.ToTable("AmbalajKaynakSenkronizasyonKuyrugu", table =>
            {
                table.HasCheckConstraint(
                    "CK_AmbalajKaynakSenkronizasyonKuyrugu_Surum",
                    "\"Surum\" > 0");
                table.HasCheckConstraint(
                    "CK_AmbalajKaynakSenkronizasyonKuyrugu_DenemeSayisi",
                    "\"DenemeSayisi\" >= 0");
                table.HasCheckConstraint(
                    "CK_AmbalajKaynakSenkronizasyonKuyrugu_Durum",
                    "\"Durum\" >= 0 AND \"Durum\" <= 3");
                table.HasCheckConstraint(
                    "CK_AmbalajKaynakSenkronizasyonKuyrugu_Kilit",
                    "(\"Durum\" = 1 AND \"KilitKimligi\" IS NOT NULL AND \"KilitBitisTarihiUtc\" IS NOT NULL) OR " +
                    "(\"Durum\" <> 1 AND \"KilitKimligi\" IS NULL AND \"KilitBitisTarihiUtc\" IS NULL)");
            });

            // ProjeId bilincli olarak FK degildir. Proje silindikten sonra kalan
            // kuyruk isi 404 sonucuyla idempotent bicimde tamamlanabilir.
            entity.HasKey(x => x.ProjeId);
            entity.Property(x => x.ProjeId).ValueGeneratedNever();
            entity.Property(x => x.Surum).HasDefaultValue(1L);
            entity.Property(x => x.Durum)
                .HasConversion<short>()
                .HasDefaultValue(AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            entity.Property(x => x.TalepTarihiUtc)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UygunTarihUtc)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.DenemeSayisi).HasDefaultValue(0);
            entity.Property(x => x.KilitBitisTarihiUtc).HasColumnType("timestamp with time zone");
            entity.Property(x => x.SonDenemeTarihiUtc).HasColumnType("timestamp with time zone");
            entity.Property(x => x.SonBasariliTarihUtc).HasColumnType("timestamp with time zone");
            entity.Property(x => x.SonHata).HasMaxLength(2000);
            entity.Property(x => x.HataKuyrugunaAlindiTarihiUtc).HasColumnType("timestamp with time zone");

            entity.HasIndex(x => new { x.Durum, x.UygunTarihUtc, x.TalepTarihiUtc })
                .HasDatabaseName("IX_AmbalajKaynakSenkronizasyonKuyrugu_Islenebilir")
                .HasFilter("\"Durum\" IN (0, 1)");
        });
    }

    public static void ConfigureFinansModule(this ModelBuilder modelBuilder)
    {
        ConfigureFinansUrun(modelBuilder);
        ConfigureFinansIsKaydi(modelBuilder);
        ConfigureFinansSiparisVeFatura(modelBuilder);
        ConfigureFinansDuzenliIs(modelBuilder);
        ConfigureFinansGider(modelBuilder);
        ConfigureFinansDenetim(modelBuilder);
    }

    private static void ConfigureFinansUrun(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansUrun>(entity =>
        {
            entity.ToTable("FinansUrunleri");
            entity.HasIndex(x => x.Kod).IsUnique();
            entity.Property(x => x.Kod).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Ad).HasMaxLength(250).IsRequired();
            entity.Property(x => x.FiyatlandirmaBirimi).HasConversion<int>();
            entity.Property(x => x.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<FinansUrunEslesmesi>(entity =>
        {
            entity.ToTable("FinansUrunEslesmeleri");
            entity.HasIndex(x => new { x.FinansUrunId, x.IsTuru });
            entity.Property(x => x.IsTuru).HasConversion<int>();
            entity.Property(x => x.SandikAdi).HasMaxLength(250);
            entity.Property(x => x.SandikTipi).HasMaxLength(100);
            entity.Property(x => x.Boy).HasPrecision(18, 4);
            entity.Property(x => x.En).HasPrecision(18, 4);
            entity.Property(x => x.Yukseklik).HasPrecision(18, 4);
            entity.Property(x => x.Aktif).HasDefaultValue(true);
            entity.HasOne(x => x.FinansUrun)
                .WithMany(x => x.Eslesmeler)
                .HasForeignKey(x => x.FinansUrunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinansFiyatTarifesi>(entity =>
        {
            entity.ToTable("FinansFiyatTarifeleri", table =>
            {
                table.HasCheckConstraint("CK_FinansFiyatTarifeleri_Yil", "\"Yil\" BETWEEN 2000 AND 2200");
                table.HasCheckConstraint("CK_FinansFiyatTarifeleri_Tarih", "\"GecerlilikBitisi\" >= \"GecerlilikBaslangici\"");
                table.HasCheckConstraint("CK_FinansFiyatTarifeleri_Fiyat", "\"BirimFiyat\" >= 0 AND \"KdvOrani\" >= 0 AND \"KdvOrani\" <= 100");
            });
            entity.HasIndex(x => new { x.FinansUrunId, x.Yil, x.GecerlilikBaslangici, x.GecerlilikBitisi }).IsUnique();
            entity.Property(x => x.BirimFiyat).HasPrecision(18, 6);
            entity.Property(x => x.ParaBirimi).HasMaxLength(3).IsRequired();
            entity.Property(x => x.KdvOrani).HasPrecision(7, 4);
            entity.Property(x => x.Aktif).HasDefaultValue(true);
            entity.HasOne(x => x.FinansUrun)
                .WithMany(x => x.FiyatTarifeleri)
                .HasForeignKey(x => x.FinansUrunId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFinansIsKaydi(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansIsKaydi>(entity =>
        {
            entity.ToTable("FinansIsKayitlari", table =>
            {
                table.HasCheckConstraint("CK_FinansIsKayitlari_Miktarlar", "\"Adet\" > 0 AND \"BirimM3\" >= 0 AND \"ToplamM3\" >= 0");
                table.HasCheckConstraint("CK_FinansIsKayitlari_Fiyat", "\"BirimFiyatSnapshot\" >= 0 AND \"KdvOraniSnapshot\" >= 0 AND \"KdvOraniSnapshot\" <= 100");
                table.HasCheckConstraint("CK_FinansIsKayitlari_Proje", "\"ProjeId\" IS NOT NULL OR NULLIF(BTRIM(\"ProjeNo\"), '') IS NOT NULL");
            });
            entity.HasIndex(x => new { x.KaynakTuru, x.KaynakKayitId })
                .IsUnique()
                .HasFilter("\"KaynakKayitId\" IS NOT NULL");
            entity.HasIndex(x => new { x.Durum, x.IptalEdildi, x.KaynakAktif });
            entity.HasIndex(x => new { x.ProjeId, x.FinansDonemi });
            entity.HasIndex(x => x.UretimTarihi);
            entity.Property(x => x.ProjeNo).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Musteri).HasMaxLength(250).IsRequired();
            entity.Property(x => x.IsTuru).HasConversion<int>();
            entity.Property(x => x.IsAdi).HasMaxLength(250).IsRequired();
            entity.Property(x => x.OzelIsTuru).HasMaxLength(150);
            entity.Property(x => x.HesaplamaYontemi).HasConversion<int?>();
            entity.Property(x => x.RaporGrubu).HasMaxLength(150);
            entity.Property(x => x.Aciklama).HasMaxLength(2000);
            entity.Property(x => x.TalepEdenKisi).HasMaxLength(200);
            entity.Property(x => x.TalepEdenBolum).HasMaxLength(200);
            entity.Property(x => x.SandikNo).HasMaxLength(100);
            entity.Property(x => x.SandikAdi).HasMaxLength(250);
            entity.Property(x => x.SandikTipi).HasMaxLength(100);
            entity.Property(x => x.Boy).HasPrecision(18, 4);
            entity.Property(x => x.En).HasPrecision(18, 4);
            entity.Property(x => x.Yukseklik).HasPrecision(18, 4);
            entity.Property(x => x.Adet).HasPrecision(18, 4);
            entity.Property(x => x.Birim).HasMaxLength(30).IsRequired();
            entity.Property(x => x.BirimM3).HasPrecision(18, 6);
            entity.Property(x => x.ToplamM3).HasPrecision(18, 6);
            entity.Property(x => x.FiyatlandirmaBirimiSnapshot).HasConversion<int>();
            entity.Property(x => x.BirimFiyatSnapshot).HasPrecision(18, 6);
            entity.Property(x => x.ParaBirimiSnapshot).HasMaxLength(3).IsRequired();
            entity.Property(x => x.KdvOraniSnapshot).HasPrecision(7, 4);
            entity.Property(x => x.Durum).HasConversion<int>();
            entity.Property(x => x.KaynakTuru).HasMaxLength(50).IsRequired();
            entity.Property(x => x.KaynakKayitId).HasMaxLength(150);
            entity.Property(x => x.KaynakAktif).HasDefaultValue(true);
            entity.Property(x => x.IptalEdildi).HasDefaultValue(false);
            entity.Property(x => x.IptalAciklamasi).HasMaxLength(1000);

            entity.HasOne(x => x.Proje).WithMany().HasForeignKey(x => x.ProjeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinansUrun).WithMany().HasForeignKey(x => x.FinansUrunId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DuzenliIs).WithMany(x => x.OlusanKayitlar).HasForeignKey(x => x.DuzenliIsId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureFinansSiparisVeFatura(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansSiparis>(entity =>
        {
            entity.ToTable("FinansSiparisleri");
            entity.HasIndex(x => x.KayitNo).IsUnique();
            entity.HasIndex(x => x.PoNumarasi).IsUnique();
            entity.HasIndex(x => new { x.Durum, x.SiparisTarihi });
            entity.Property(x => x.KayitNo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PoNumarasi).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Durum).HasConversion<int>();
            entity.Property(x => x.IptalEdildi).HasDefaultValue(false);
        });

        modelBuilder.Entity<FinansSiparisKalemi>(entity =>
        {
            entity.ToTable("FinansSiparisKalemleri", table =>
            {
                table.HasCheckConstraint("CK_FinansSiparisKalemleri_Miktar", "\"Adet\" >= 0 AND \"M3\" >= 0 AND (\"Adet\" > 0 OR \"M3\" > 0)");
                table.HasCheckConstraint("CK_FinansSiparisKalemleri_Tutar", "\"BirimFiyatSnapshot\" >= 0 AND \"NetTutarSnapshot\" >= 0 AND \"KdvTutariSnapshot\" >= 0 AND \"ToplamTutarSnapshot\" >= 0");
            });
            entity.HasIndex(x => new { x.FinansSiparisId, x.FinansIsKaydiId }).IsUnique();
            entity.Property(x => x.Adet).HasPrecision(18, 4);
            entity.Property(x => x.M3).HasPrecision(18, 6);
            entity.Property(x => x.FiyatlandirmaBirimiSnapshot).HasConversion<int>();
            entity.Property(x => x.BirimFiyatSnapshot).HasPrecision(18, 6);
            entity.Property(x => x.ParaBirimiSnapshot).HasMaxLength(3).IsRequired();
            entity.Property(x => x.KdvOraniSnapshot).HasPrecision(7, 4);
            entity.Property(x => x.NetTutarSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.KdvTutariSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.ToplamTutarSnapshot).HasPrecision(18, 4);
            entity.HasOne(x => x.FinansSiparis).WithMany(x => x.Kalemler).HasForeignKey(x => x.FinansSiparisId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinansIsKaydi).WithMany(x => x.SiparisKalemleri).HasForeignKey(x => x.FinansIsKaydiId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinansUrun).WithMany().HasForeignKey(x => x.FinansUrunId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinansFatura>(entity =>
        {
            entity.ToTable("FinansFaturalari", table =>
            {
                table.HasCheckConstraint(
                    "CK_FinansFaturalari_BelgeTutarlari",
                    "(\"BelgeParaBirimiSnapshot\" IS NULL AND \"BelgeNetTutarSnapshot\" IS NULL AND " +
                    "\"BelgeKdvTutariSnapshot\" IS NULL AND \"BelgeToplamTutarSnapshot\" IS NULL) OR " +
                    "(\"BelgeParaBirimiSnapshot\" IS NOT NULL AND \"BelgeNetTutarSnapshot\" IS NOT NULL AND " +
                    "\"BelgeKdvTutariSnapshot\" IS NOT NULL AND \"BelgeToplamTutarSnapshot\" IS NOT NULL AND " +
                    "\"BelgeNetTutarSnapshot\" >= 0 AND \"BelgeKdvTutariSnapshot\" >= 0 AND " +
                    "\"BelgeToplamTutarSnapshot\" >= 0 AND " +
                    "ABS((\"BelgeNetTutarSnapshot\" + \"BelgeKdvTutariSnapshot\") - \"BelgeToplamTutarSnapshot\") <= 0.02)");
            });
            entity.HasIndex(x => x.KayitNo).IsUnique();
            entity.HasIndex(x => x.FaturaNumarasi).IsUnique();
            entity.HasIndex(x => new { x.Durum, x.FaturaTarihi });
            entity.Property(x => x.KayitNo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.FaturaNumarasi).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BelgeParaBirimiSnapshot).HasMaxLength(3);
            entity.Property(x => x.BelgeNetTutarSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.BelgeKdvTutariSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.BelgeToplamTutarSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.MutabakatFarkiSnapshot).HasPrecision(18, 4).HasDefaultValue(0m);
            entity.Property(x => x.MutabakatAciklamasi).HasMaxLength(1000);
            entity.Property(x => x.Durum).HasConversion<int>();
            entity.Property(x => x.IptalEdildi).HasDefaultValue(false);
            entity.HasOne(x => x.FinansSiparis).WithMany(x => x.Faturalar).HasForeignKey(x => x.FinansSiparisId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinansFaturaKalemi>(entity =>
        {
            entity.ToTable("FinansFaturaKalemleri", table =>
            {
                table.HasCheckConstraint("CK_FinansFaturaKalemleri_Miktar", "\"Adet\" >= 0 AND \"M3\" >= 0 AND (\"Adet\" > 0 OR \"M3\" > 0)");
                table.HasCheckConstraint("CK_FinansFaturaKalemleri_Tutar", "\"NetTutarSnapshot\" >= 0 AND \"KdvTutariSnapshot\" >= 0 AND \"ToplamTutarSnapshot\" >= 0");
            });
            entity.HasIndex(x => new { x.FinansFaturaId, x.FinansSiparisKalemiId }).IsUnique();
            entity.Property(x => x.Adet).HasPrecision(18, 4);
            entity.Property(x => x.M3).HasPrecision(18, 6);
            entity.Property(x => x.NetTutarSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.KdvTutariSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.ToplamTutarSnapshot).HasPrecision(18, 4);
            entity.HasOne(x => x.FinansFatura).WithMany(x => x.Kalemler).HasForeignKey(x => x.FinansFaturaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinansSiparisKalemi).WithMany(x => x.FaturaKalemleri).HasForeignKey(x => x.FinansSiparisKalemiId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFinansDuzenliIs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansDuzenliIs>(entity =>
        {
            entity.ToTable("FinansDuzenliIsleri", table =>
            {
                table.HasCheckConstraint("CK_FinansDuzenliIsleri_Tarih", "\"BitisTarihi\" IS NULL OR \"BitisTarihi\" >= \"BaslangicTarihi\"");
                table.HasCheckConstraint("CK_FinansDuzenliIsleri_Gun", "\"OlusturmaGunu\" BETWEEN 1 AND 31");
                table.HasCheckConstraint("CK_FinansDuzenliIsleri_Fiyat", "\"Miktar\" > 0 AND \"BirimFiyat\" >= 0 AND \"KdvOrani\" >= 0 AND \"KdvOrani\" <= 100");
            });
            entity.HasIndex(x => new { x.Aktif, x.BaslangicTarihi, x.BitisTarihi });
            entity.Property(x => x.ManuelProjeNo).HasMaxLength(100);
            entity.Property(x => x.ManuelProjeAdi).HasMaxLength(250);
            entity.Property(x => x.IsAdi).HasMaxLength(250).IsRequired();
            entity.Property(x => x.IsTuru).HasConversion<int>();
            entity.Property(x => x.OzelIsTuru).HasMaxLength(150);
            entity.Property(x => x.HesaplamaYontemi).HasConversion<int>();
            entity.Property(x => x.RaporGrubu).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Musteri).HasMaxLength(250).IsRequired();
            entity.Property(x => x.TekrarSikligi).HasConversion<int>();
            entity.Property(x => x.Miktar).HasPrecision(18, 4);
            entity.Property(x => x.Birim).HasMaxLength(30).IsRequired();
            entity.Property(x => x.BirimFiyat).HasPrecision(18, 6);
            entity.Property(x => x.ParaBirimi).HasMaxLength(3).IsRequired();
            entity.Property(x => x.KdvOrani).HasPrecision(7, 4);
            entity.Property(x => x.Aktif).HasDefaultValue(true);
            entity.HasOne(x => x.Proje).WithMany().HasForeignKey(x => x.ProjeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinansUrun).WithMany().HasForeignKey(x => x.FinansUrunId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFinansGider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansGiderKategori>(entity =>
        {
            entity.ToTable("FinansGiderKategorileri");
            entity.HasIndex(x => x.Ad).IsUnique();
            entity.Property(x => x.Ad).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<FinansGiderKalemi>(entity =>
        {
            entity.ToTable("FinansGiderKalemleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_FinansGiderKalemleri_Varsayilanlar",
                    "(\"VarsayilanMiktar\" IS NULL OR \"VarsayilanMiktar\" > 0) AND " +
                    "(\"VarsayilanBirimFiyat\" IS NULL OR \"VarsayilanBirimFiyat\" >= 0) AND " +
                    "(\"VarsayilanKdvOrani\" IS NULL OR " +
                    "(\"VarsayilanKdvOrani\" >= 0 AND \"VarsayilanKdvOrani\" <= 100))");
            });
            entity.HasIndex(x => x.Kod).IsUnique();
            entity.HasIndex(x => new { x.FinansGiderKategoriId, x.Ad }).IsUnique();
            entity.Property(x => x.Kod).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Ad).HasMaxLength(150).IsRequired();
            entity.Property(x => x.VarsayilanFirmaVeyaKisi).HasMaxLength(250);
            entity.Property(x => x.VarsayilanMiktar).HasPrecision(18, 4);
            entity.Property(x => x.VarsayilanBirim).HasMaxLength(30);
            entity.Property(x => x.VarsayilanBirimFiyat).HasPrecision(18, 6);
            entity.Property(x => x.VarsayilanParaBirimi).HasMaxLength(3);
            entity.Property(x => x.VarsayilanKdvDahil).HasDefaultValue(false);
            entity.Property(x => x.VarsayilanKdvOrani).HasPrecision(7, 4);
            entity.Property(x => x.Aktif).HasDefaultValue(true);
            entity.HasOne(x => x.Kategori).WithMany(x => x.Kalemler).HasForeignKey(x => x.FinansGiderKategoriId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinansGider>(entity =>
        {
            entity.ToTable("FinansGiderleri", table =>
            {
                table.HasCheckConstraint("CK_FinansGiderleri_Miktar", "\"Miktar\" > 0 AND \"BirimFiyat\" >= 0 AND \"Tutar\" >= 0");
                table.HasCheckConstraint("CK_FinansGiderleri_Kdv", "\"KdvOrani\" >= 0 AND \"KdvOrani\" <= 100 AND \"Matrah\" >= 0 AND \"KdvTutari\" >= 0 AND \"ToplamTutar\" >= 0");
            });
            entity.HasIndex(x => new { x.FinansDonemi, x.IptalEdildi });
            entity.HasIndex(x => new { x.FinansGiderKategoriId, x.FinansGiderKalemiId });
            entity.Property(x => x.AltKategori).HasMaxLength(200);
            entity.Property(x => x.FirmaVeyaKisi).HasMaxLength(250);
            entity.Property(x => x.Miktar).HasPrecision(18, 4);
            entity.Property(x => x.Birim).HasMaxLength(30).IsRequired();
            entity.Property(x => x.BirimFiyat).HasPrecision(18, 6);
            entity.Property(x => x.Tutar).HasPrecision(18, 4);
            entity.Property(x => x.ParaBirimi).HasMaxLength(3).IsRequired();
            entity.Property(x => x.KdvOrani).HasPrecision(7, 4);
            entity.Property(x => x.Matrah).HasPrecision(18, 4);
            entity.Property(x => x.KdvTutari).HasPrecision(18, 4);
            entity.Property(x => x.ToplamTutar).HasPrecision(18, 4);
            entity.Property(x => x.ManuelProjeNo).HasMaxLength(100);
            entity.Property(x => x.IsTuru).HasConversion<int?>();
            entity.Property(x => x.IptalEdildi).HasDefaultValue(false);
            entity.HasOne(x => x.Kategori).WithMany(x => x.Giderler).HasForeignKey(x => x.FinansGiderKategoriId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GiderKalemi).WithMany().HasForeignKey(x => x.FinansGiderKalemiId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Proje).WithMany().HasForeignKey(x => x.ProjeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFinansDenetim(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansDegisiklikGecmisi>(entity =>
        {
            entity.ToTable("FinansDegisiklikGecmisleri");
            entity.HasIndex(x => new { x.VarlikTuru, x.VarlikId, x.CreatedDate });
            entity.Property(x => x.VarlikTuru).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Islem).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AlanAdi).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IslemYapan).HasMaxLength(100).IsRequired().HasDefaultValue("SYSTEM");
            entity.Property(x => x.EskiDeger).HasColumnType("text");
            entity.Property(x => x.YeniDeger).HasColumnType("text");
            entity.Property(x => x.Aciklama).HasMaxLength(1000);
        });
    }
}
