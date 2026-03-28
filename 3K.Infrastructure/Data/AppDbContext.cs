using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Tablo (DbSet) Tanımlamaları
        public DbSet<Proje> Projeler { get; set; } = null!;
        public DbSet<Ceki> Cekiler { get; set; } = null!;
        public DbSet<CekiSatiri> CekiSatirlari { get; set; } = null!;
        public DbSet<Sandik> Sandiklar { get; set; } = null!;
        public DbSet<SandikIcerik> SandikIcerikleri { get; set; } = null!;
        public DbSet<Kullanici> Kullanicilar { get; set; } = null!;
        public DbSet<FBTransfer> FBTransferleri { get; set; } = null!;
        public DbSet<StokKaydi> StokKayitlari { get; set; } = null!;
        public DbSet<StokHareketi> StokHareketleri { get; set; } = null!;
        public DbSet<Revizyon> Revizyonlar { get; set; } = null!;
        public DbSet<HareketGecmisi> HareketGecmisleri { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======= Enum → String Conversion'lar =======
            modelBuilder.Entity<Proje>()
                .Property(p => p.Durum)
                .HasConversion<string>();

            modelBuilder.Entity<Sandik>()
                .Property(s => s.Durum)
                .HasConversion<string>();

            modelBuilder.Entity<CekiSatiri>()
                .Property(cs => cs.Durum)
                .HasConversion<string>();

            modelBuilder.Entity<Kullanici>()
                .Property(k => k.Rol)
                .HasConversion<string>();

            modelBuilder.Entity<StokKaydi>()
                .Property(sk => sk.Durum)
                .HasConversion<string>();

            // ======= Unique Constraints =======
            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.Email)
                .IsUnique();

            modelBuilder.Entity<Sandik>()
                .HasIndex(s => new { s.ProjeId, s.SandikNo })
                .IsUnique();

            // ======= İlişkiler =======

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

            // CekiSatiri (1) - SandikIcerik (0..*) İlişkisi
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.SandikIcerikleri)
                .WithOne(si => si.CekiSatiri)
                .HasForeignKey(si => si.CekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            // CekiSatiri - Paketleyen İlişkisi
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.Paketleyen)
                .WithMany()
                .HasForeignKey(cs => cs.PaketleyenId)
                .OnDelete(DeleteBehavior.SetNull);

            // CekiSatiri - KontrolEden İlişkisi
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.KontrolEden)
                .WithMany()
                .HasForeignKey(cs => cs.KontrolEdenId)
                .OnDelete(DeleteBehavior.SetNull);

            // CekiSatiri (1) - FBTransfer (0..*) İlişkisi
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.FBTransferleri)
                .WithOne(fb => fb.CekiSatiri)
                .HasForeignKey(fb => fb.CekiSatiriId)
                .OnDelete(DeleteBehavior.Cascade);

            // FBTransfer - Kullanici İlişkisi
            modelBuilder.Entity<FBTransfer>()
                .HasOne(fb => fb.Kullanici)
                .WithMany()
                .HasForeignKey(fb => fb.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Proje (1) - StokHareketi (0..*) İlişkisi
            modelBuilder.Entity<Proje>()
                .HasMany(p => p.StokHareketleri)
                .WithOne(sh => sh.Proje)
                .HasForeignKey(sh => sh.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            // StokHareketi - Kullanici İlişkisi
            modelBuilder.Entity<StokHareketi>()
                .HasOne(sh => sh.Kullanici)
                .WithMany()
                .HasForeignKey(sh => sh.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Revizyon İlişkileri (explicit FK ile)
            modelBuilder.Entity<Revizyon>()
                .HasOne(r => r.Proje)
                .WithMany(p => p.Revizyonlar)
                .HasForeignKey(r => r.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revizyon>()
                .HasOne(r => r.Kullanici)
                .WithMany(k => k.Revizyonlar)
                .HasForeignKey(r => r.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Audit alanları otomatik güncelleme
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}