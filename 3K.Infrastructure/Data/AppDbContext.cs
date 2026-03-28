using 
using Microsoft.EntityFrameworkCore;
3K.Core.Entities;

namespace 3K.Infrastructure.Data
{
    public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Tablo (DbSet) Tanımlamaları
    public DbSet<Proje> Projeler { get; set; }
    public DbSet<Ceki> Cekiler { get; set; }
    public DbSet<CekiSatiri> CekiSatirlari { get; set; }
    public DbSet<Sandik> Sandiklar { get; set; }
    public DbSet<SandikIcerik> SandikIcerikleri { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<FBTransfer> FBTransferleri { get; set; }
    public DbSet<StokKaydi> StokKayitlari { get; set; }
    public DbSet<StokHareketi> StokHareketleri { get; set; }
    public DbSet<Revizyon> Revizyonlar { get; set; }
    public DbSet<HareketGecmisi> HareketGecmisleri { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Proje (1) - Ceki (1..*) İlişkisi
        modelBuilder.Entity<Proje>()
            .HasMany(p => p.Cekiler)
            .WithOne(c => c.Proje)
            .HasForeignKey(c => c.ProjeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ceki (1) - CekiSatiri (1..*) İlişkisi
        modelBuilder.Entity<Ceki>()
            .HasMany(c => c.CekiSatirlari)
            .WithOne(cs => cs.Ceki)
            .HasForeignKey(cs => cs.CekiId)
            .OnDelete(DeleteBehavior.Cascade);

        // Proje (1) - Sandik (1..*) İlişkisi
        modelBuilder.Entity<Proje>()
            .HasMany(p => p.Sandiklar)
            .WithOne(s => s.Proje)
            .HasForeignKey(s => s.ProjeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sandik (1) - SandikIcerik (1..*) İlişkisi
        modelBuilder.Entity<Sandik>()
            .HasMany(s => s.SandikIcerikleri)
            .WithOne(si => si.Sandik)
            .HasForeignKey(si => si.SandikId)
            .OnDelete(DeleteBehavior.Cascade);

        // CekiSatiri (1) - SandikIcerik (0..1) İlişkisi 
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.SandikIcerik)
            .WithOne(si => si.CekiSatiri)
            .HasForeignKey<SandikIcerik>(si => si.CekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict);

        // CekiSatiri (1) - FBTransfer (0..*) İlişkisi
        modelBuilder.Entity<CekiSatiri>()
            .HasMany(cs => cs.FBTransferleri)
            .WithOne(fb => fb.CekiSatiri)
            .HasForeignKey(fb => fb.CekiSatiriId)
            .OnDelete(DeleteBehavior.Cascade);

        // CekiSatiri (1) - StokHareketi (0..*) İlişkisi
        modelBuilder.Entity<CekiSatiri>()
            .HasMany(cs => cs.StokHareketleri)
            .WithOne(sh => sh.CekiSatiri)
            .HasForeignKey(sh => sh.CekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict);

        // StokKaydi (1) - StokHareketi (0..*) İlişkisi
        modelBuilder.Entity<StokKaydi>()
            .HasMany(sk => sk.StokHareketleri)
            .WithOne(sh => sh.StokKaydi)
            .HasForeignKey(sh => sh.StokKaydiId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Revizyon ve Kullanıcı (1 - 0..*) İlişkisi (EF Core Shadow Property ile FK oluşturur)
        modelBuilder.Entity<Kullanici>()
            .HasMany(k => k.Revizyonlar)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
}