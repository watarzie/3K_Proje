using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ======= Ana Tablo DbSet'leri =======
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
        public DbSet<ProjeTransfer> ProjeTransferleri { get; set; } = null!;

        // ======= Lookup (Parametre) Tablo DbSet'leri =======
        public DbSet<LookupProjeDurum> LookupProjeDurumlari { get; set; } = null!;
        public DbSet<LookupSandikDurum> LookupSandikDurumlari { get; set; } = null!;
        public DbSet<LookupSandikTipi> LookupSandikTipleri { get; set; } = null!;
        public DbSet<LookupDepoLokasyon> LookupDepoLokasyonlari { get; set; } = null!;
        public DbSet<LookupUrunDurum> LookupUrunDurumlari { get; set; } = null!;
        public DbSet<LookupGridDurum> LookupGridDurumlari { get; set; } = null!;
        public DbSet<LookupUcKDurum> LookupUcKDurumlari { get; set; } = null!;
        public DbSet<LookupKullaniciRol> LookupKullaniciRolleri { get; set; } = null!;
        public DbSet<LookupStokDurum> LookupStokDurumlari { get; set; } = null!;
        public DbSet<LookupIslemTipi> LookupIslemTipleri { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===============================================================
            // 1. LOOKUP TABLOLARI — AlternateKey (Deger üzerinden FK sağlar)
            // ===============================================================
            ConfigureLookupTable<LookupProjeDurum>(modelBuilder);
            ConfigureLookupTable<LookupSandikDurum>(modelBuilder);
            ConfigureLookupTable<LookupSandikTipi>(modelBuilder);
            ConfigureLookupTable<LookupDepoLokasyon>(modelBuilder);
            ConfigureLookupTable<LookupUrunDurum>(modelBuilder);
            ConfigureLookupTable<LookupGridDurum>(modelBuilder);
            ConfigureLookupTable<LookupUcKDurum>(modelBuilder);
            ConfigureLookupTable<LookupKullaniciRol>(modelBuilder);
            ConfigureLookupTable<LookupStokDurum>(modelBuilder);
            ConfigureLookupTable<LookupIslemTipi>(modelBuilder);

            // ===============================================================
            // 2. UNIQUE CONSTRAINTS
            // ===============================================================
            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.Email)
                .IsUnique();

            modelBuilder.Entity<Sandik>()
                .HasIndex(s => new { s.ProjeId, s.SandikNo })
                .IsUnique();

            // ===============================================================
            // 3. FK İLİŞKİLERİ — Entity ↔ Lookup Tabloları
            // ===============================================================

            // --- Proje.Durum → LookupProjeDurum.Deger ---
            modelBuilder.Entity<Proje>()
                .HasOne<LookupProjeDurum>()
                .WithMany()
                .HasForeignKey(p => p.Durum)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.Durum → LookupSandikDurum.Deger ---
            modelBuilder.Entity<Sandik>()
                .HasOne<LookupSandikDurum>()
                .WithMany()
                .HasForeignKey(s => s.Durum)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.Tip → LookupSandikTipi.Deger ---
            modelBuilder.Entity<Sandik>()
                .HasOne<LookupSandikTipi>()
                .WithMany()
                .HasForeignKey(s => s.Tip)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.DepoLokasyonu → LookupDepoLokasyon.Deger ---
            modelBuilder.Entity<Sandik>()
                .HasOne<LookupDepoLokasyon>()
                .WithMany()
                .HasForeignKey(s => s.DepoLokasyonu)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.Durum → LookupUrunDurum.Deger ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne<LookupUrunDurum>()
                .WithMany()
                .HasForeignKey(cs => cs.Durum)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.GridDurumu → LookupGridDurum.Deger ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne<LookupGridDurum>()
                .WithMany()
                .HasForeignKey(cs => cs.GridDurumu)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.UcKDurumu → LookupUcKDurum.Deger ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne<LookupUcKDurum>()
                .WithMany()
                .HasForeignKey(cs => cs.UcKDurumu)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.GridPersonel → Kullanici ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.GridPersonel)
                .WithMany()
                .HasForeignKey(cs => cs.GridPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- EksikMiktar, GridEksikMiktar computed properties — DB'de kolon değil ---
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.EksikMiktar);
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.GridEksikMiktar);

            // --- ProjeTransfer ilişkileri ---
            modelBuilder.Entity<ProjeTransfer>()
                .HasOne(pt => pt.KaynakProje)
                .WithMany()
                .HasForeignKey(pt => pt.KaynakProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjeTransfer>()
                .HasOne(pt => pt.HedefProje)
                .WithMany()
                .HasForeignKey(pt => pt.HedefProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjeTransfer>()
                .HasOne(pt => pt.KaynakCekiSatiri)
                .WithMany()
                .HasForeignKey(pt => pt.KaynakCekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjeTransfer>()
                .HasOne(pt => pt.HedefCekiSatiri)
                .WithMany()
                .HasForeignKey(pt => pt.HedefCekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjeTransfer>()
                .HasOne(pt => pt.Kullanici)
                .WithMany()
                .HasForeignKey(pt => pt.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Kullanici.Rol → LookupKullaniciRol.Deger ---
            modelBuilder.Entity<Kullanici>()
                .HasOne<LookupKullaniciRol>()
                .WithMany()
                .HasForeignKey(k => k.Rol)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- StokKaydi.Durum → LookupStokDurum.Deger ---
            modelBuilder.Entity<StokKaydi>()
                .HasOne<LookupStokDurum>()
                .WithMany()
                .HasForeignKey(sk => sk.Durum)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // --- StokHareketi.IslemTipi → LookupIslemTipi.Deger ---
            modelBuilder.Entity<StokHareketi>()
                .HasOne<LookupIslemTipi>()
                .WithMany()
                .HasForeignKey(sh => sh.IslemTipi)
                .HasPrincipalKey(l => l.Deger)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================================================
            // 4. ANA TABLO İLİŞKİLERİ
            // ===============================================================

            // Proje (1) - Ceki (1..*)
            modelBuilder.Entity<Proje>()
                .HasMany(p => p.Cekiler)
                .WithOne(c => c.Proje)
                .HasForeignKey(c => c.ProjeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ceki (1) - CekiSatiri (1..*)
            modelBuilder.Entity<Ceki>()
                .HasMany(c => c.CekiSatirlari)
                .WithOne(cs => cs.Ceki)
                .HasForeignKey(cs => cs.CekiId)
                .OnDelete(DeleteBehavior.Cascade);

            // Proje (1) - Sandik (1..*)
            modelBuilder.Entity<Proje>()
                .HasMany(p => p.Sandiklar)
                .WithOne(s => s.Proje)
                .HasForeignKey(s => s.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sandik (1) - SandikIcerik (1..*)
            modelBuilder.Entity<Sandik>()
                .HasMany(s => s.SandikIcerikleri)
                .WithOne(si => si.Sandik)
                .HasForeignKey(si => si.SandikId)
                .OnDelete(DeleteBehavior.Cascade);

            // CekiSatiri (1) - SandikIcerik (0..*)
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.SandikIcerikleri)
                .WithOne(si => si.CekiSatiri)
                .HasForeignKey(si => si.CekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            // CekiSatiri - Paketleyen
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.Paketleyen)
                .WithMany()
                .HasForeignKey(cs => cs.PaketleyenId)
                .OnDelete(DeleteBehavior.SetNull);

            // CekiSatiri - KontrolEden
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.KontrolEden)
                .WithMany()
                .HasForeignKey(cs => cs.KontrolEdenId)
                .OnDelete(DeleteBehavior.SetNull);

            // CekiSatiri (1) - FBTransfer (0..*)
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.FBTransferleri)
                .WithOne(fb => fb.CekiSatiri)
                .HasForeignKey(fb => fb.CekiSatiriId)
                .OnDelete(DeleteBehavior.Cascade);

            // FBTransfer - Kullanici
            modelBuilder.Entity<FBTransfer>()
                .HasOne(fb => fb.Kullanici)
                .WithMany()
                .HasForeignKey(fb => fb.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // CekiSatiri (1) - StokHareketi (0..*)
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.StokHareketleri)
                .WithOne(sh => sh.CekiSatiri)
                .HasForeignKey(sh => sh.CekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict);

            // StokKaydi (1) - StokHareketi (0..*)
            modelBuilder.Entity<StokKaydi>()
                .HasMany(sk => sk.StokHareketleri)
                .WithOne(sh => sh.StokKaydi)
                .HasForeignKey(sh => sh.StokKaydiId)
                .OnDelete(DeleteBehavior.Cascade);

            // Proje (1) - StokHareketi (0..*)
            modelBuilder.Entity<Proje>()
                .HasMany(p => p.StokHareketleri)
                .WithOne(sh => sh.Proje)
                .HasForeignKey(sh => sh.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);

            // StokHareketi - Kullanici
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

            // Revizyon İlişkileri
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

            // ===============================================================
            // 5. SEED DATA — Lookup Tabloları
            // ===============================================================
            SeedLookupData(modelBuilder);
        }

        /// <summary>
        /// Lookup tablosu konfigürasyonu: Deger alanı AlternateKey olarak tanımlanır.
        /// </summary>
        private static void ConfigureLookupTable<T>(ModelBuilder modelBuilder) where T : LookupBase
        {
            modelBuilder.Entity<T>().HasAlternateKey(l => l.Deger);
        }

        private static void SeedLookupData(ModelBuilder modelBuilder)
        {
            // ProjeDurum
            modelBuilder.Entity<LookupProjeDurum>().HasData(
                new LookupProjeDurum { Id = 1, Anahtar = 0, Deger = "Hazirlaniyor" },
                new LookupProjeDurum { Id = 2, Anahtar = 1, Deger = "Devam" },
                new LookupProjeDurum { Id = 3, Anahtar = 2, Deger = "Tamamlandi" },
                new LookupProjeDurum { Id = 4, Anahtar = 3, Deger = "Beklemede" },
                new LookupProjeDurum { Id = 5, Anahtar = 4, Deger = "SevkEdildi" },
                new LookupProjeDurum { Id = 6, Anahtar = 5, Deger = "EksikSevkEdildi" }
            );

            // SandikDurum
            modelBuilder.Entity<LookupSandikDurum>().HasData(
                new LookupSandikDurum { Id = 1, Anahtar = 0, Deger = "Bos" },
                new LookupSandikDurum { Id = 2, Anahtar = 1, Deger = "Hazirlaniyor" },
                new LookupSandikDurum { Id = 3, Anahtar = 2, Deger = "Hazir" },
                new LookupSandikDurum { Id = 4, Anahtar = 3, Deger = "Sevkedildi" }
            );

            // SandikTipi
            modelBuilder.Entity<LookupSandikTipi>().HasData(
                new LookupSandikTipi { Id = 1, Anahtar = 1, Deger = "Proje" },
                new LookupSandikTipi { Id = 2, Anahtar = 2, Deger = "Yedek" },
                new LookupSandikTipi { Id = 3, Anahtar = 3, Deger = "Saha" }
            );

            // DepoLokasyon
            modelBuilder.Entity<LookupDepoLokasyon>().HasData(
                new LookupDepoLokasyon { Id = 1, Anahtar = 0, Deger = "Belirsiz" },
                new LookupDepoLokasyon { Id = 2, Anahtar = 1, Deger = "Grid" },
                new LookupDepoLokasyon { Id = 3, Anahtar = 2, Deger = "UcK" },
                new LookupDepoLokasyon { Id = 4, Anahtar = 3, Deger = "Protest" }
            );

            // UrunDurum
            modelBuilder.Entity<LookupUrunDurum>().HasData(
                new LookupUrunDurum { Id = 1, Anahtar = 0, Deger = "Bekliyor" },
                new LookupUrunDurum { Id = 2, Anahtar = 1, Deger = "KismiGeldi" },
                new LookupUrunDurum { Id = 3, Anahtar = 2, Deger = "Tamamlandi" },
                new LookupUrunDurum { Id = 4, Anahtar = 3, Deger = "Eksik" },
                new LookupUrunDurum { Id = 5, Anahtar = 4, Deger = "StoktanKarsilandi" },
                new LookupUrunDurum { Id = 6, Anahtar = 5, Deger = "FBdenKarsilandi" },
                new LookupUrunDurum { Id = 7, Anahtar = 6, Deger = "SonraGidecek" },
                new LookupUrunDurum { Id = 8, Anahtar = 7, Deger = "SandikDegisti" },
                new LookupUrunDurum { Id = 9, Anahtar = 8, Deger = "IptalVeyaPasif" },
                new LookupUrunDurum { Id = 10, Anahtar = 9, Deger = "TeslimAlindi" },
                new LookupUrunDurum { Id = 11, Anahtar = 10, Deger = "GeriGonderildi" },
                new LookupUrunDurum { Id = 12, Anahtar = 11, Deger = "KismiTamamlandi" },
                new LookupUrunDurum { Id = 13, Anahtar = 12, Deger = "Kayip" },
                new LookupUrunDurum { Id = 14, Anahtar = 13, Deger = "GriddeHazir" },
                new LookupUrunDurum { Id = 15, Anahtar = 14, Deger = "GriddeEksik" },
                new LookupUrunDurum { Id = 16, Anahtar = 15, Deger = "Sipariste" },
                new LookupUrunDurum { Id = 17, Anahtar = 16, Deger = "Gelmedi" },
                new LookupUrunDurum { Id = 18, Anahtar = 17, Deger = "TrafoSevk" },
                new LookupUrunDurum { Id = 19, Anahtar = 18, Deger = "BaskaProyeVerildi" },
                new LookupUrunDurum { Id = 20, Anahtar = 19, Deger = "HataliUrun" }
            );

            // GridDurum
            modelBuilder.Entity<LookupGridDurum>().HasData(
                new LookupGridDurum { Id = 1, Anahtar = 0, Deger = "Bekliyor" },
                new LookupGridDurum { Id = 2, Anahtar = 1, Deger = "Uretimde" },
                new LookupGridDurum { Id = 3, Anahtar = 2, Deger = "StokHazir" },
                new LookupGridDurum { Id = 4, Anahtar = 3, Deger = "SevkEdildi" },
                new LookupGridDurum { Id = 5, Anahtar = 4, Deger = "KismiSevkEdildi" },
                new LookupGridDurum { Id = 6, Anahtar = 5, Deger = "Bekletiliyor" },
                new LookupGridDurum { Id = 7, Anahtar = 6, Deger = "IptalEdildi" },
                new LookupGridDurum { Id = 8, Anahtar = 7, Deger = "TamGeldi" },
                new LookupGridDurum { Id = 9, Anahtar = 8, Deger = "EksikGeldi" },
                new LookupGridDurum { Id = 10, Anahtar = 9, Deger = "Gelmedi" },
                new LookupGridDurum { Id = 11, Anahtar = 10, Deger = "TrafoSevk" },
                new LookupGridDurum { Id = 12, Anahtar = 11, Deger = "Iptal" },
                new LookupGridDurum { Id = 13, Anahtar = 12, Deger = "Sipariste" }
            );

            // UcKDurum
            modelBuilder.Entity<LookupUcKDurum>().HasData(
                new LookupUcKDurum { Id = 1, Anahtar = 0, Deger = "Bekliyor" },
                new LookupUcKDurum { Id = 2, Anahtar = 1, Deger = "TamGeldi" },
                new LookupUcKDurum { Id = 3, Anahtar = 2, Deger = "EksikGeldi" },
                new LookupUcKDurum { Id = 4, Anahtar = 3, Deger = "Gelmedi" },
                new LookupUcKDurum { Id = 5, Anahtar = 4, Deger = "Paketlendi" },
                new LookupUcKDurum { Id = 6, Anahtar = 5, Deger = "KontrolEdildi" },
                new LookupUcKDurum { Id = 7, Anahtar = 6, Deger = "IadeEdildi" },
                new LookupUcKDurum { Id = 8, Anahtar = 7, Deger = "ProjedenKarsilandi" },
                new LookupUcKDurum { Id = 9, Anahtar = 8, Deger = "StoktanKarsilandi" },
                new LookupUcKDurum { Id = 10, Anahtar = 9, Deger = "TedarikcidenGeldi" },
                new LookupUcKDurum { Id = 11, Anahtar = 10, Deger = "BaskaProyeVerildi" },
                new LookupUcKDurum { Id = 12, Anahtar = 11, Deger = "HataliUrun" }
            );

            // KullaniciRol
            modelBuilder.Entity<LookupKullaniciRol>().HasData(
                new LookupKullaniciRol { Id = 1, Anahtar = 0, Deger = "Admin" },
                new LookupKullaniciRol { Id = 2, Anahtar = 1, Deger = "Personel3K" },
                new LookupKullaniciRol { Id = 3, Anahtar = 2, Deger = "PersonelGrid" },
                new LookupKullaniciRol { Id = 4, Anahtar = 3, Deger = "Yonetici" }
            );

            // StokDurum
            modelBuilder.Entity<LookupStokDurum>().HasData(
                new LookupStokDurum { Id = 1, Anahtar = 0, Deger = "Aktif" },
                new LookupStokDurum { Id = 2, Anahtar = 1, Deger = "Tukendi" },
                new LookupStokDurum { Id = 3, Anahtar = 2, Deger = "Rezerve" }
            );

            // IslemTipi
            modelBuilder.Entity<LookupIslemTipi>().HasData(
                new LookupIslemTipi { Id = 1, Anahtar = 0, Deger = "CekiYuklendi" },
                new LookupIslemTipi { Id = 2, Anahtar = 1, Deger = "SandikOlusturuldu" },
                new LookupIslemTipi { Id = 3, Anahtar = 2, Deger = "SandikBolundu" },
                new LookupIslemTipi { Id = 4, Anahtar = 3, Deger = "SandikDegisti" },
                new LookupIslemTipi { Id = 5, Anahtar = 4, Deger = "UrunTasindi" },
                new LookupIslemTipi { Id = 6, Anahtar = 5, Deger = "FBTransferi" },
                new LookupIslemTipi { Id = 7, Anahtar = 6, Deger = "StokKullanimi" },
                new LookupIslemTipi { Id = 8, Anahtar = 7, Deger = "EksikKapatildi" },
                new LookupIslemTipi { Id = 9, Anahtar = 8, Deger = "PDFAlindi" },
                new LookupIslemTipi { Id = 10, Anahtar = 9, Deger = "MailGonderildi" },
                new LookupIslemTipi { Id = 11, Anahtar = 10, Deger = "UrunGuncellendi" },
                new LookupIslemTipi { Id = 12, Anahtar = 11, Deger = "KullaniciOlusturuldu" },
                new LookupIslemTipi { Id = 13, Anahtar = 12, Deger = "ProjeOlusturuldu" }
            );
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                }
                else if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
