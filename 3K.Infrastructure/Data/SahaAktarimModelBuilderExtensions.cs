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
