using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Hareket geçmişi ve arşiv tablosu eşlemeleri.</summary>
internal static class HareketGecmisiModelBuilderExtensions
{
    public static void ConfigureHareketGecmisiModule(this ModelBuilder modelBuilder)
    {
        // --- HareketGecmisi.IslemTipiId → LookupIslemTipi.Id ---
        modelBuilder.Entity<HareketGecmisi>()
            .HasOne(hg => hg.IslemTipiLookup)
            .WithMany()
            .HasForeignKey(hg => hg.IslemTipiId)
            .OnDelete(DeleteBehavior.SetNull);

        // HareketGecmisi İlişkileri
        modelBuilder.Entity<HareketGecmisi>()
            .HasOne(hg => hg.Proje)
            .WithMany(p => p.HareketGecmisleri)
            .HasForeignKey(hg => hg.ProjeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HareketGecmisi>()
            .HasOne(hg => hg.Kullanici)
            .WithMany(k => k.HareketGecmisleri)
            .HasForeignKey(hg => hg.KullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        // Proje hareketleri en yeniden eskiye okunur; arşiv tablosuyla aynı indeks proje yabancı anahtarını da karşılar.
        modelBuilder.Entity<HareketGecmisi>()
            .HasIndex(hg => new { hg.ProjeId, hg.Tarih })
            .IsDescending(false, true);

        // HareketGecmisiArsiv — arsiv tablosu (FK yok, self-contained)
        modelBuilder.Entity<HareketGecmisiArsiv>(entity =>
        {
            entity.ToTable("HareketGecmisleriArsiv");
            entity.HasIndex(e => new { e.ProjeId, e.Tarih }).IsDescending(false, true);
            entity.HasIndex(e => e.IslemTipiId);
        });
    }
}
