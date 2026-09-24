using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

public static class UretimModelBuilderExtensions
{
    public static void ConfigureUretimYasamDongusuModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AmbalajUretimKaydi>().Property(x => x.Version).IsRowVersion();
        modelBuilder.Entity<AmbalajUretimFormuSurumu>(e =>
        {
            e.ToTable("AmbalajUretimFormuSurumleri");
            e.HasIndex(x => x.IdempotencyAnahtari).IsUnique();
            e.HasIndex(x => new { x.FormKimligi, x.Surum }).IsUnique();
            e.HasIndex(x => new { x.KapsamAnahtari, x.Surum }).IsUnique();
            e.Property(x => x.IstekHash).HasMaxLength(64);
            e.Property(x => x.KapsamAnahtari).HasMaxLength(200);
            e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.Aciklama).HasMaxLength(1000);
            e.HasOne<Proje>().WithMany().HasForeignKey(x => x.ProjeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AmbalajUretimFormuKaydi>(e =>
        {
            e.ToTable("AmbalajUretimFormuKayitlari");
            e.HasIndex(x => new { x.FormSurumuId, x.AmbalajUretimKaydiId }).IsUnique();
            e.HasOne(x => x.FormSurumu).WithMany(x => x.Kayitlar).HasForeignKey(x => x.FormSurumuId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AmbalajUretimKaydi>().WithMany().HasForeignKey(x => x.AmbalajUretimKaydiId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AmbalajUretimGerceklesmesi>(e =>
        {
            e.ToTable("AmbalajUretimGerceklesmeleri");
            e.HasIndex(x => new { x.GerceklesmeKimligi, x.Surum }).IsUnique();
            e.HasIndex(x => new { x.AmbalajUretimKaydiId, x.Surum }).IsUnique();
            e.HasIndex(x => x.OncekiSurumId).IsUnique().HasFilter("\"OncekiSurumId\" IS NOT NULL");
            e.HasIndex(x => x.GerceklesmeTarihi);
            e.Property(x => x.Boy).HasPrecision(18, 4);
            e.Property(x => x.En).HasPrecision(18, 4);
            e.Property(x => x.Yukseklik).HasPrecision(18, 4);
            e.Property(x => x.NetM3).HasPrecision(18, 6);
            e.Property(x => x.SarfM3).HasPrecision(18, 6);
            e.Property(x => x.ProjeNo).HasMaxLength(100);
            e.Property(x => x.ProjeAdi).HasMaxLength(250);
            e.Property(x => x.SandikNo).HasMaxLength(100);
            e.Property(x => x.SandikAdi).HasMaxLength(250);
            e.Property(x => x.FormulVersiyonu).HasMaxLength(50);
            e.Property(x => x.Gerekce).HasMaxLength(1000);
            e.HasOne<AmbalajUretimKaydi>().WithMany().HasForeignKey(x => x.AmbalajUretimKaydiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AmbalajUretimGerceklesmesi>().WithMany().HasForeignKey(x => x.OncekiSurumId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
