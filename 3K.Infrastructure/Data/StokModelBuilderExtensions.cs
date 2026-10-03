using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Stok kaydı ve stok hareketi eşlemeleri.</summary>
internal static class StokModelBuilderExtensions
{
    public static void ConfigureStokModule(this ModelBuilder modelBuilder)
    {
        // --- StokKaydi.DurumId → LookupStokDurum.Id ---
        modelBuilder.Entity<StokKaydi>()
            .HasOne(sk => sk.DurumLookup)
            .WithMany()
            .HasForeignKey(sk => sk.DurumId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- StokHareketi.IslemTipiId → LookupIslemTipi.Id ---
        modelBuilder.Entity<StokHareketi>()
            .HasOne(sh => sh.IslemTipiLookup)
            .WithMany()
            .HasForeignKey(sh => sh.IslemTipiId)
            .OnDelete(DeleteBehavior.Restrict);

        // StokKaydi (1) - StokHareketi (0..*)
        modelBuilder.Entity<StokKaydi>()
            .HasMany(sk => sk.StokHareketleri)
            .WithOne(sh => sh.StokKaydi)
            .HasForeignKey(sh => sh.StokKaydiId)
            .OnDelete(DeleteBehavior.Cascade);

        // StokHareketi - Kullanici
        modelBuilder.Entity<StokHareketi>()
            .HasOne(sh => sh.Kullanici)
            .WithMany()
            .HasForeignKey(sh => sh.KullaniciId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
