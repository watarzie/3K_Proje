using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Bildirim, bildirim aboneliği ve kullanıcı bildirimi eşlemeleri.</summary>
internal static class BildirimModelBuilderExtensions
{
    public static void ConfigureBildirimModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bildirim>(e =>
        {
            e.Property(b => b.Baslik).HasMaxLength(160).IsRequired();
            e.Property(b => b.Mesaj).HasMaxLength(1000).IsRequired();
            e.Property(b => b.HedefUrl).HasMaxLength(500);
            e.Property(b => b.ReferansTipi).HasMaxLength(80);

            e.HasIndex(b => new { b.TipId, b.ReferansTipi, b.ReferansId })
                .IsUnique()
                .HasFilter("\"ReferansId\" IS NOT NULL");
            e.HasIndex(b => b.CreatedDate).IsDescending();
            e.HasIndex(b => new { b.TipId, b.CreatedDate }).IsDescending(false, true);

            e.HasOne(b => b.OlusturanKullanici)
                .WithMany(k => k.OlusturduguBildirimler)
                .HasForeignKey(b => b.OlusturanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<KullaniciBildirimi>(e =>
        {
            e.Property(kb => kb.OkunduMu).HasDefaultValue(false);
            e.HasIndex(kb => new { kb.BildirimId, kb.KullaniciId }).IsUnique();
            e.HasIndex(kb => new { kb.KullaniciId, kb.OkunduMu, kb.Id }).IsDescending(false, false, true);

            e.HasOne(kb => kb.Bildirim)
                .WithMany(b => b.Alicilar)
                .HasForeignKey(kb => kb.BildirimId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(kb => kb.Kullanici)
                .WithMany(k => k.Bildirimleri)
                .HasForeignKey(kb => kb.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BildirimAboneligi>(e =>
        {
            e.HasIndex(ba => new { ba.KullaniciId, ba.TipId }).IsUnique();
            e.HasIndex(ba => ba.TipId);

            e.HasOne(ba => ba.Kullanici)
                .WithMany(k => k.BildirimAbonelikleri)
                .HasForeignKey(ba => ba.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
