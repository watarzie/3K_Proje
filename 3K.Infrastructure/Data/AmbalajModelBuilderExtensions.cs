using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Data;

/// <summary>Ambalaj üretim modülünün tablo, dizin, kısıt ve ilişki eşlemeleri.</summary>
internal static class AmbalajModelBuilderExtensions
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
                    "WHEN \"SandikCinsi\" IN (1, 2) AND NOT \"BagimsizKayitMi\" AND \"KaynakKayitId\" IS NOT NULL AND \"KaynakModul\" IN (1, 2, 3) " +
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
            entity.Property(x => x.SarfOrani).HasPrecision(7, 6).HasDefaultValue(AmbalajHesaplayici.VarsayilanSarfOrani);
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
                .OnDelete(DeleteBehavior.Restrict);

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
}
