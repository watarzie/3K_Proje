using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Normal projeden saha projesine aktarım ve aktarım kalemi eşlemeleri.</summary>
internal static class SahaAktarimModelBuilderExtensions
{
    public static void ConfigureSahaAktarimModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SahaAktarim>(e =>
        {
            e.HasIndex(p => p.SahaProjeId);
            e.HasIndex(p => p.KaynakProjeId);

            e.HasOne(p => p.SahaProje)
                .WithMany()
                .HasForeignKey(p => p.SahaProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.KaynakProje)
                .WithMany()
                .HasForeignKey(p => p.KaynakProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Kullanici)
                .WithMany()
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sahadan sahaya taşıma: kaynak saha silinse de hedefteki taşıma başlığı kalır.
            e.HasIndex(p => p.KaynakSahaProjeId);
            e.HasOne(p => p.KaynakSahaProje)
                .WithMany()
                .HasForeignKey(p => p.KaynakSahaProjeId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(p => p.IslemAnahtari)
                .IsUnique()
                .HasFilter("\"IslemAnahtari\" IS NOT NULL");
        });

        // Taşıma izi: yalnız kök normal satır silinince iz de gider. Taşıma başlığı, kaynak/hedef saha projesi, sandık ya
        // da saha satırı silinirse bağ boşalır, iz kalır (numaralar kopyadır, uzunluk sınırı kaynak kolonlar gibi yoktur);
        // böylece A→B→C zincirinde ara saha B silinse de ürünün A→B adımı okunur.
        modelBuilder.Entity<SahaTasimaKalemi>(e =>
        {
            e.ToTable("SahaTasimaKalemleri");
            e.Property(p => p.Miktar).HasPrecision(18, 4);
            e.HasIndex(p => p.SahaAktarimId);
            e.HasIndex(p => p.SahaAktarimKalemiId);
            e.HasIndex(p => p.OncekiSahaAktarimId);
            e.HasIndex(p => p.KaynakCekiSatiriId);
            e.HasIndex(p => p.SahaCekiSatiriId);
            e.HasIndex(p => p.KaynakSahaProjeId);
            e.HasIndex(p => p.HedefSahaProjeId);
            e.HasIndex(p => p.KaynakSahaSandikId);
            e.HasIndex(p => p.HedefSahaSandikId);

            e.HasOne(p => p.SahaAktarim)
                .WithMany(p => p.TasimaKalemleri)
                .HasForeignKey(p => p.SahaAktarimId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.SahaAktarimKalemi)
                .WithMany()
                .HasForeignKey(p => p.SahaAktarimKalemiId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.OncekiSahaAktarim)
                .WithMany()
                .HasForeignKey(p => p.OncekiSahaAktarimId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.KaynakCekiSatiri)
                .WithMany()
                .HasForeignKey(p => p.KaynakCekiSatiriId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.SahaCekiSatiri)
                .WithMany()
                .HasForeignKey(p => p.SahaCekiSatiriId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.KaynakSahaProje)
                .WithMany()
                .HasForeignKey(p => p.KaynakSahaProjeId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.HedefSahaProje)
                .WithMany()
                .HasForeignKey(p => p.HedefSahaProjeId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.KaynakSahaSandik)
                .WithMany()
                .HasForeignKey(p => p.KaynakSahaSandikId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.HedefSahaSandik)
                .WithMany()
                .HasForeignKey(p => p.HedefSahaSandikId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SahaAktarimKalemi>(e =>
        {
            e.Property(p => p.Miktar).HasPrecision(18, 4);
            e.HasIndex(p => p.SahaAktarimId);
            e.HasIndex(p => p.KaynakProjeId);
            e.HasIndex(p => p.SahaProjeId);
            e.HasIndex(p => p.SahaCekiSatiriId);
            e.HasIndex(p => p.KaynakSandikId);
            e.HasIndex(p => p.SahaSandikId);
            e.HasIndex(p => p.DurumId);
            e.HasIndex(p => p.AktarimTipiId);
            e.HasIndex(p => new { p.KaynakCekiSatiriId, p.DurumId });

            e.HasOne(p => p.SahaAktarim)
                .WithMany(p => p.Kalemler)
                .HasForeignKey(p => p.SahaAktarimId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.KaynakProje)
                .WithMany()
                .HasForeignKey(p => p.KaynakProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.SahaProje)
                .WithMany()
                .HasForeignKey(p => p.SahaProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.KaynakCekiSatiri)
                .WithMany()
                .HasForeignKey(p => p.KaynakCekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.SahaCekiSatiri)
                .WithMany()
                .HasForeignKey(p => p.SahaCekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.KaynakSandik)
                .WithMany()
                .HasForeignKey(p => p.KaynakSandikId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.SahaSandik)
                .WithMany()
                .HasForeignKey(p => p.SahaSandikId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
