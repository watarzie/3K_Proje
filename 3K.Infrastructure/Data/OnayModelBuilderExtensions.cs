using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Infrastructure.Data;

/// <summary>İşlem onay kuyruğu, onay yetkileri ve operasyon kurallarının eşlemesi; 3K onay kurallarının başlangıç verisi.</summary>
internal static class OnayModelBuilderExtensions
{
    public static void ConfigureOnayModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OnayBekleyenIslem>()
            .Property(o => o.PayloadJson)
            .HasColumnType("text")
            .IsRequired();

        modelBuilder.Entity<OnayBekleyenIslem>()
            .Property(o => o.IslemKodu)
            .HasMaxLength(100)
            .HasDefaultValue(OnayIslemKodlari.Genel)
            .IsRequired();

        modelBuilder.Entity<OnayBekleyenIslem>(e =>
        {
            e.Property(o => o.ReferansTipi).HasMaxLength(80);
            e.Property(o => o.HedefUrl).HasMaxLength(500);
            e.Property(o => o.KararAciklamasi).HasMaxLength(1000);
            e.Property(o => o.CalistirmaHatasi).HasMaxLength(1000);
            e.Property(o => o.CalistirmaDurumu)
                .HasDefaultValue(OnayCalistirmaDurumu.Bekliyor);

            e.HasIndex(o => new { o.TalepEdenKullaniciId, o.CreatedDate })
                .IsDescending(false, true);
            e.HasIndex(o => new { o.OnaylayanKullaniciId, o.KararTarihi })
                .IsDescending(false, true);
            e.HasIndex(o => new { o.Durum, o.CalistirmaDurumu });
            e.HasIndex(o => o.ProjeId);
        });

        modelBuilder.Entity<OnayBekleyenIslem>()
            .HasIndex(o => new { o.Durum, o.IslemKodu });

        modelBuilder.Entity<OnayBekleyenIslem>()
            .HasOne(o => o.TalepEdenKullanici)
            .WithMany()
            .HasForeignKey(o => o.TalepEdenKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OnayBekleyenIslem>()
            .HasOne(o => o.OnaylayanKullanici)
            .WithMany()
            .HasForeignKey(o => o.OnaylayanKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OnayBekleyenIslem>()
            .HasOne(o => o.Proje)
            .WithMany()
            .HasForeignKey(o => o.ProjeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OnayIslemYetki>(e =>
        {
            e.Property(o => o.IslemKodu)
                .HasMaxLength(100)
                .IsRequired();

            e.HasIndex(o => new { o.IslemKodu, o.RolId }).IsUnique();

            e.HasOne(o => o.Rol)
                .WithMany()
                .HasForeignKey(o => o.RolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnayOperasyonKurali>(e =>
        {
            e.ToTable("OnayOperasyonKurallari", table =>
                table.HasCheckConstraint(
                    "CK_OnayOperasyonKurallari_IslemKodu_Dolu",
                    "length(btrim(\"IslemKodu\")) > 0"));

            e.Property(kural => kural.IslemKodu)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(kural => kural.OnayGerektirirMi)
                .HasDefaultValue(true);

            e.HasIndex(kural => kural.IslemKodu)
                .IsUnique();
        });
    }

    public static void SeedApprovalRules(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IslemOnayKurali>().HasData(
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 1, LookupUcKDurumId = 8, OnayGerektirirMi = true }, // ProjedenKarşılandı
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 2, LookupUcKDurumId = 9, OnayGerektirirMi = true }, // StoktanKarşılandı
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 3, LookupUcKDurumId = 10, OnayGerektirirMi = true }, // TedarikçidenGeldi
            
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 4, LookupUcKDurumId = 2, OnayGerektirirMi = false }, // TamGeldi
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 5, LookupUcKDurumId = 3, OnayGerektirirMi = false }, // EksikGeldi
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 6, LookupUcKDurumId = 4, OnayGerektirirMi = false }, // Gelmedi
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 7, LookupUcKDurumId = 11, OnayGerektirirMi = false }, // BaşkaProjeyeVerildi
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 8, LookupUcKDurumId = 12, OnayGerektirirMi = false }, // GeriGönderildi
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 9, LookupUcKDurumId = 13, OnayGerektirirMi = false }, // HatalıÜrün
            new IslemOnayKurali { CreatedDate = new DateTime(2026, 5, 11), Id = 10, LookupUcKDurumId = 14, OnayGerektirirMi = false } // FazlaGeldi
        );
    }
}
