using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Infrastructure.Data;

/// <summary>Sandık, sandık içeriği, sevkiyat ve sandıklar arası taşıma eşlemeleri.</summary>
internal static class SandikModelBuilderExtensions
{
    public static void ConfigureSandikModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sandik>()
            .HasIndex(s => new { s.ProjeId, s.SandikNo })
            .IsUnique();

        modelBuilder.Entity<Sandik>()
            .HasIndex(s => new { s.ProjeId, s.DurumId, s.SandikNo });

        modelBuilder.Entity<Sandik>()
            .Property(s => s.SevkiyatDuzeltmeAcikMi)
            .HasDefaultValue(false);

        // Sandık satırının tamamını tek bir iyimser eşzamanlılık belirteciyle korur.
        // Npgsql, uint + IsRowVersion eşleşmesini PostgreSQL xmin kolonuna map eder.
        // DurumId gibi değiştirilebilir alanlar ayrıca concurrency token yapılmamalıdır;
        // aksi halde detached entity güncellemelerinde yeni değer yanlışlıkla eski değer
        // kabul edilerek yalancı concurrency conflict üretilebilir.
        modelBuilder.Entity<Sandik>()
            .Property(s => s.Version)
            .IsRowVersion();

        // --- Sandik.DurumId → LookupSandikDurum.Id ---
        modelBuilder.Entity<Sandik>()
            .HasOne(s => s.DurumLookup)
            .WithMany()
            .HasForeignKey(s => s.DurumId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Sandik.TipId → LookupSandikTipi.Id ---
        modelBuilder.Entity<Sandik>()
            .HasOne(s => s.TipLookup)
            .WithMany()
            .HasForeignKey(s => s.TipId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Sandik.DepoLokasyonId → LookupDepoLokasyon.Id ---
        modelBuilder.Entity<Sandik>()
            .HasOne(s => s.DepoLokasyonLookup)
            .WithMany()
            .HasForeignKey(s => s.DepoLokasyonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SandikIcerik>(e =>
        {
            e.ToTable("SandikIcerikleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_SandikIcerikleri_TahsisMiktari_NonNegative",
                    "\"TahsisMiktari\" >= 0");
                table.HasCheckConstraint(
                    "CK_SandikIcerikleri_KonulanAdet_TahsisAraliginda",
                    "\"TahsisMiktari\" = 0 OR " +
                    "(\"KonulanAdet\" >= 0 AND \"KonulanAdet\" <= \"TahsisMiktari\")");
            });

            e.Property(p => p.TahsisMiktari)
                .HasPrecision(18, 4)
                .HasDefaultValue(0m);
            e.Property(p => p.KonulanAdet).HasPrecision(18, 4);
            e.Property(p => p.EksikAdet).HasPrecision(18, 4);
            e.Property(p => p.AktifGridSevkKarsilananMiktari).HasPrecision(18, 4);
            e.Property(p => p.StokKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.ProjeKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.TedarikciKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.Miktar).HasPrecision(18, 4);
            e.Property(p => p.UrunBilgisi).HasMaxLength(SandikIcerikUrunBilgisiKurali.EnFazlaUzunluk);

            // Npgsql, uint + IsRowVersion eşleşmesini PostgreSQL'in xmin sistem
            // kolonuna map eder; ayrıca fiziksel bir Version kolonu gerekmez.
            e.Property(p => p.Version).IsRowVersion();

            // Çeki satırına bağlı ürün aynı sandıkta tek tahsis kaydı olmalıdır.
            // CekiSatiriId null olan manuel ürünler birbirinden bağımsızdır.
            e.HasIndex(p => new { p.SandikId, p.CekiSatiriId })
                .IsUnique()
                .HasFilter("\"CekiSatiriId\" IS NOT NULL AND \"TahsisMiktari\" > 0");

            // Kısmi benzersiz indeks manuel içerikleri kapsamaz; sandığa göre okuma tüm satırlar için ayrı indeksle yapılır.
            e.HasIndex(p => p.SandikId);
        });

        modelBuilder.Entity<SandikTopluTasimaIslemi>(e =>
        {
            e.ToTable("SandikTopluTasimaIslemleri", table => table.HasCheckConstraint(
                "CK_SandikTopluTasimaIslemleri_SatirSayisi", "\"SatirSayisi\" BETWEEN 1 AND 250"));
            e.Property(p => p.IstekHash).HasMaxLength(64).IsRequired();
            e.Property(p => p.CreatedDate).HasColumnType("timestamp without time zone");
            e.Property(p => p.UpdatedDate).HasColumnType("timestamp without time zone");
            e.HasIndex(p => p.IslemAnahtari).IsUnique();
            e.HasIndex(p => new { p.ProjeId, p.CreatedDate });
        });

        modelBuilder.Entity<SandikUrunTransferi>(e =>
        {
            e.ToTable("SandikUrunTransferleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_SandikUrunTransferleri_Miktar",
                    "\"Miktar\" > 0");
                table.HasCheckConstraint(
                    "CK_SandikUrunTransferleri_Kirilimlar",
                    "\"StokKarsilanan\" >= 0 AND " +
                    "\"ProjeKarsilanan\" >= 0 AND " +
                    "\"TedarikciKarsilanan\" >= 0 AND " +
                    "\"StokKarsilanan\" + \"ProjeKarsilanan\" + \"TedarikciKarsilanan\" <= \"Miktar\"");
            });
            e.Property(p => p.Miktar).HasPrecision(18, 4);
            e.Property(p => p.StokKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.ProjeKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.TedarikciKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.KaynakSandikNo).HasMaxLength(100);
            e.Property(p => p.HedefSandikNo).HasMaxLength(100);
            e.Property(p => p.BarkodNo).HasMaxLength(200);
            e.Property(p => p.UrunAdi).HasMaxLength(1000);

            e.HasIndex(p => p.IslemAnahtari).IsUnique();
            e.HasIndex(p => new { p.ProjeId, p.CreatedDate });
            e.HasIndex(p => new { p.CekiSatiriId, p.CreatedDate });
            e.HasIndex(p => new { p.KaynakSandikId, p.CreatedDate });
            e.HasIndex(p => new { p.HedefSandikId, p.CreatedDate });

            e.HasOne(p => p.Proje)
                .WithMany()
                .HasForeignKey(p => p.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.CekiSatiri)
                .WithMany()
                .HasForeignKey(p => p.CekiSatiriId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.KaynakSandik)
                .WithMany()
                .HasForeignKey(p => p.KaynakSandikId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.HedefSandik)
                .WithMany()
                .HasForeignKey(p => p.HedefSandikId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.BirimLookup)
                .WithMany()
                .HasForeignKey(p => p.BirimId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Kullanici)
                .WithMany()
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SevkSeferi>(e =>
        {
            e.Property(p => p.SeferNo).HasMaxLength(40).IsRequired();
            e.Property(p => p.AracPlaka).HasMaxLength(30);
            e.Property(p => p.Sofor).HasMaxLength(100);
            e.Property(p => p.DorsePlaka).HasMaxLength(30);
            e.Property(p => p.Aciklama).HasMaxLength(500);
            e.Property(p => p.IstekHash).HasMaxLength(64);
            e.HasIndex(p => p.SeferNo).IsUnique();
            // Toplu sevkin tekrar anahtarı; tekil sevkte boş olduğundan kısmi benzersiz indeks.
            e.HasIndex(p => p.IslemAnahtari).IsUnique().HasFilter("\"IslemAnahtari\" IS NOT NULL");
            e.HasIndex(p => p.SevkTarihi);
            e.HasIndex(p => p.AracPlaka);

            e.HasOne(p => p.Kullanici)
                .WithMany()
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sevkiyat>(e =>
        {
            e.Property(p => p.Aciklama).HasMaxLength(500);
            e.Property(p => p.AracPlaka).HasMaxLength(30);
            e.HasIndex(p => new { p.ProjeId, p.SevkiyatNo }).IsUnique();
            e.HasIndex(p => p.SevkTarihi);

            // Proje silinince sevkiyatı da silinir; sefer yalnız gruplayan başlıktır, sevkiyatı varken silinemez.
            e.HasOne(p => p.SevkSeferi)
                .WithMany(p => p.Sevkiyatlar)
                .HasForeignKey(p => p.SevkSeferiId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Proje)
                .WithMany(p => p.Sevkiyatlar)
                .HasForeignKey(p => p.ProjeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Kullanici)
                .WithMany()
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SevkiyatSandik>(e =>
        {
            e.HasIndex(p => new { p.SevkiyatId, p.SandikId }).IsUnique();
            e.HasIndex(p => p.SandikId);

            e.HasOne(p => p.Sevkiyat)
                .WithMany(p => p.Sandiklar)
                .HasForeignKey(p => p.SevkiyatId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Sandik)
                .WithMany(p => p.SevkiyatSandiklari)
                .HasForeignKey(p => p.SandikId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sandik (1) - SandikIcerik (1..*)
        modelBuilder.Entity<Sandik>()
            .HasMany(s => s.SandikIcerikleri)
            .WithOne(si => si.Sandik)
            .HasForeignKey(si => si.SandikId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- SandikIcerik.BirimId → LookupBirim.Id (Madde 7) ---
        modelBuilder.Entity<SandikIcerik>()
            .HasOne(si => si.BirimLookup)
            .WithMany()
            .HasForeignKey(si => si.BirimId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
