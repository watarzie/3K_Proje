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

        // ======= Ana Tablo DbSet'leri =======
        public DbSet<Proje> Projeler { get; set; } = null!;
        public DbSet<Ceki> Cekiler { get; set; } = null!;
        public DbSet<CekiSatiri> CekiSatirlari { get; set; } = null!;
        public DbSet<Sandik> Sandiklar { get; set; } = null!;
        public DbSet<SandikIcerik> SandikIcerikleri { get; set; } = null!;
        public DbSet<Kullanici> Kullanicilar { get; set; } = null!;
        public DbSet<IslemOnayKurali> IslemOnayKurallari { get; set; } = null!;
        public DbSet<StokKaydi> StokKayitlari { get; set; } = null!;
        public DbSet<StokHareketi> StokHareketleri { get; set; } = null!;
        public DbSet<HareketGecmisi> HareketGecmisleri { get; set; } = null!;
        public DbSet<ProjeTransfer> ProjeTransferleri { get; set; } = null!;
        public DbSet<OnayBekleyenIslem> OnayBekleyenIslemler { get; set; } = null!;

        // ======= RBAC (Rol Tabanlı Erişim Kontrolü) DbSet'leri =======
        public DbSet<Rol> Roller { get; set; } = null!;
        public DbSet<MenuTanimi> MenuTanimlari { get; set; } = null!;
        public DbSet<RolYetki> RolYetkileri { get; set; } = null!;

        // ======= Lookup (Parametre) Tablo DbSet'leri =======
        public DbSet<LookupProjeDurum> LookupProjeDurumlari { get; set; } = null!;
        public DbSet<LookupSandikDurum> LookupSandikDurumlari { get; set; } = null!;
        public DbSet<LookupSandikTipi> LookupSandikTipleri { get; set; } = null!;
        public DbSet<LookupDepoLokasyon> LookupDepoLokasyonlari { get; set; } = null!;
        public DbSet<LookupUrunDurum> LookupUrunDurumlari { get; set; } = null!;
        public DbSet<LookupGridDurum> LookupGridDurumlari { get; set; } = null!;
        public DbSet<LookupGridSevkDurum> LookupGridSevkDurumlari { get; set; } = null!;
        public DbSet<LookupUcKDurum> LookupUcKDurumlari { get; set; } = null!;
        public DbSet<LookupYetkiTipi> LookupYetkiTipleri { get; set; } = null!;
        public DbSet<LookupStokDurum> LookupStokDurumlari { get; set; } = null!;
        public DbSet<LookupIslemTipi> LookupIslemTipleri { get; set; } = null!;
        public DbSet<LookupGeriGonderilmeSebebi> LookupGeriGonderilmeSebepleri { get; set; } = null!;
        public DbSet<LookupProjeTipi> LookupProjeTipleri { get; set; } = null!;
        public DbSet<LookupBirim> LookupBirimler { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===============================================================
            // 1. LOOKUP TABLOLARI — Deger unique index (AlternateKey kaldırıldı)
            // ===============================================================
            ConfigureLookupTable<LookupProjeDurum>(modelBuilder);
            ConfigureLookupTable<LookupSandikDurum>(modelBuilder);
            ConfigureLookupTable<LookupSandikTipi>(modelBuilder);
            ConfigureLookupTable<LookupDepoLokasyon>(modelBuilder);
            ConfigureLookupTable<LookupUrunDurum>(modelBuilder);
            ConfigureLookupTable<LookupGridDurum>(modelBuilder);
            ConfigureLookupTable<LookupGridSevkDurum>(modelBuilder);
            ConfigureLookupTable<LookupUcKDurum>(modelBuilder);
            ConfigureLookupTable<LookupYetkiTipi>(modelBuilder);
            ConfigureLookupTable<LookupStokDurum>(modelBuilder);
            ConfigureLookupTable<LookupIslemTipi>(modelBuilder);
            ConfigureLookupTable<LookupGeriGonderilmeSebebi>(modelBuilder);
            ConfigureLookupTable<LookupProjeTipi>(modelBuilder);
            ConfigureLookupTable<LookupBirim>(modelBuilder);

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
            // 3. FK İLİŞKİLERİ — Entity ↔ Lookup Tabloları (ID bazlı)
            // ===============================================================

            // --- Proje.DurumId → LookupProjeDurum.Id ---
            modelBuilder.Entity<Proje>()
                .HasOne(p => p.DurumLookup)
                .WithMany()
                .HasForeignKey(p => p.DurumId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Proje.ProjeTipiId → LookupProjeTipi.Id ---
            modelBuilder.Entity<Proje>()
                .HasOne(p => p.ProjeTipiLookup)
                .WithMany()
                .HasForeignKey(p => p.ProjeTipiId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.DurumId → LookupSandikDurum.Id ---
            modelBuilder.Entity<Sandik>()
                .HasOne(s => s.DurumLookup)
                .WithMany()
                .HasForeignKey(s => s.DurumId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.TipId → LookupSandikTipi.Id ---
            modelBuilder.Entity<Sandik>()
                .HasOne(s => s.TipLookup)
                .WithMany()
                .HasForeignKey(s => s.TipId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Sandik.DepoLokasyonId → LookupDepoLokasyon.Id ---
            modelBuilder.Entity<Sandik>()
                .HasOne(s => s.DepoLokasyonLookup)
                .WithMany()
                .HasForeignKey(s => s.DepoLokasyonId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.DurumId → LookupUrunDurum.Id ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.DurumLookup)
                .WithMany()
                .HasForeignKey(cs => cs.DurumId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.GridDurumuId → LookupGridDurum.Id ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.GridDurumLookup)
                .WithMany()
                .HasForeignKey(cs => cs.GridDurumuId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.GridSevkDurumuId → LookupGridSevkDurum.Id ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne<LookupGridSevkDurum>()
                .WithMany()
                .HasForeignKey(cs => cs.GridSevkDurumuId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.UcKDurumuId → LookupUcKDurum.Id ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.UcKDurumLookup)
                .WithMany()
                .HasForeignKey(cs => cs.UcKDurumuId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.GridPersonel → Kullanici ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.GridPersonel)
                .WithMany()
                .HasForeignKey(cs => cs.GridPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- Computed properties — DB'de kolon değil ---
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.EksikMiktar);
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.GridEksikMiktar);
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.KumulatifToplam);
            modelBuilder.Entity<CekiSatiri>()
                .Ignore(cs => cs.KalanMiktar);

            // --- CekiSatiri.BirimId → LookupBirim.Id (Madde 7) ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.BirimLookup)
                .WithMany()
                .HasForeignKey(cs => cs.BirimId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CekiSatiri.KaynakProjeId → Proje (FB kaynak takibi) ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.KaynakProje)
                .WithMany()
                .HasForeignKey(cs => cs.KaynakProjeId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- CekiSatiri.GeriGonderilmeSebebiId → LookupGeriGonderilmeSebebi ---
            modelBuilder.Entity<CekiSatiri>()
                .HasOne(cs => cs.GeriGonderilmeSebebiLookup)
                .WithMany()
                .HasForeignKey(cs => cs.GeriGonderilmeSebebiId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

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

            // --- Kullanici.RolId → Rol ---
            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Rol)
                .WithMany(r => r.Kullanicilar)
                .HasForeignKey(k => k.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================================================
            // ONAY BEKLEYEN ISLEMLER KONFIGURASYONU
            // ===============================================================
            modelBuilder.Entity<OnayBekleyenIslem>()
                .Property(o => o.PayloadJson)
                .HasColumnType("text")
                .IsRequired();

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

            // ===============================================================
            // 4b. RBAC İLİŞKİLERİ — Rol ↔ MenuTanimi ↔ RolYetki
            // ===============================================================

            // --- MenuTanimi self-referencing tree (Parent ↔ Children) ---
            modelBuilder.Entity<MenuTanimi>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuTanimi>()
                .HasIndex(m => m.Kod)
                .IsUnique();

            // --- RolYetki bridge: Rol ↔ MenuTanimi (composite unique) ---
            modelBuilder.Entity<RolYetki>()
                .HasOne(ry => ry.Rol)
                .WithMany(r => r.Yetkiler)
                .HasForeignKey(ry => ry.RolId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolYetki>()
                .HasOne(ry => ry.MenuTanimi)
                .WithMany(m => m.Yetkiler)
                .HasForeignKey(ry => ry.MenuTanimiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolYetki>()
                .HasIndex(ry => new { ry.RolId, ry.MenuTanimiId })
                .IsUnique();

            // --- RolYetki.YetkiTipiId → LookupYetkiTipi.Id ---
            modelBuilder.Entity<RolYetki>()
                .HasOne(ry => ry.YetkiTipiLookup)
                .WithMany()
                .HasForeignKey(ry => ry.YetkiTipiId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // --- HareketGecmisi.IslemTipiId → LookupIslemTipi.Id ---
            modelBuilder.Entity<HareketGecmisi>()
                .HasOne(hg => hg.IslemTipiLookup)
                .WithMany()
                .HasForeignKey(hg => hg.IslemTipiId)
                .OnDelete(DeleteBehavior.SetNull);

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

            // --- SandikIcerik.BirimId → LookupBirim.Id (Madde 7) ---
            modelBuilder.Entity<SandikIcerik>()
                .HasOne(si => si.BirimLookup)
                .WithMany()
                .HasForeignKey(si => si.BirimId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // CekiSatiri (1) - SandikIcerik (0..*) — nullable FK (Saha/Yedek projelerde CekiSatiriId null olabilir)
            modelBuilder.Entity<CekiSatiri>()
                .HasMany(cs => cs.SandikIcerikleri)
                .WithOne(si => si.CekiSatiri)
                .HasForeignKey(si => si.CekiSatiriId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

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


            // ===============================================================
            // 5. SEED DATA — Lookup Tabloları
            // ===============================================================
            SeedLookupData(modelBuilder);

            // ===============================================================
            // 6. SEED DATA — RBAC (Roller, Menüler, Yetkiler)
            // ===============================================================
            SeedRbacData(modelBuilder);
        }

        /// <summary>
        /// Lookup tablosu konfigürasyonu: Deger alanı unique index olarak tanımlanır.
        /// AlternateKey KALDIRILDI — FK ilişkileri artık Id (PK) üzerinden kurulur.
        /// </summary>
        private static void ConfigureLookupTable<T>(ModelBuilder modelBuilder) where T : LookupBase
        {
            modelBuilder.Entity<T>().HasIndex(l => l.Deger).IsUnique();
        }

        private static void SeedLookupData(ModelBuilder modelBuilder)
        {
            // ProjeDurum
            modelBuilder.Entity<LookupProjeDurum>().HasData(
                new LookupProjeDurum { Id = 1, Anahtar = 1, Deger = "Hazırlanıyor" },
                new LookupProjeDurum { Id = 2, Anahtar = 2, Deger = "Devam" },
                new LookupProjeDurum { Id = 3, Anahtar = 3, Deger = "Tamamlandı" },
                new LookupProjeDurum { Id = 4, Anahtar = 4, Deger = "Beklemede" },
                new LookupProjeDurum { Id = 5, Anahtar = 5, Deger = "Sevk Edildi" },
                new LookupProjeDurum { Id = 6, Anahtar = 6, Deger = "Eksik Sevk Edildi" }
            );

            // SandikDurum (Madde 6: Kapandı eklendi)
            modelBuilder.Entity<LookupSandikDurum>().HasData(
                new LookupSandikDurum { Id = 1, Anahtar = 1, Deger = "Boş" },
                new LookupSandikDurum { Id = 2, Anahtar = 2, Deger = "Hazırlanıyor" },
                new LookupSandikDurum { Id = 3, Anahtar = 3, Deger = "Hazır" },
                new LookupSandikDurum { Id = 4, Anahtar = 4, Deger = "Sevk Edildi" }
            );

            // SandikTipi (fiziksel sandık cinsi)
            modelBuilder.Entity<LookupSandikTipi>().HasData(
                new LookupSandikTipi { Id = 1, Anahtar = 1, Deger = "Ahşap Kapalı" },
                new LookupSandikTipi { Id = 2, Anahtar = 2, Deger = "Katlanır Sandık" }
            );

            // DepoLokasyon
            modelBuilder.Entity<LookupDepoLokasyon>().HasData(
                new LookupDepoLokasyon { Id = 1, Anahtar = 1, Deger = "Belirsiz" },
                new LookupDepoLokasyon { Id = 2, Anahtar = 2, Deger = "3K" },
                new LookupDepoLokasyon { Id = 4, Anahtar = 4, Deger = "Seymen" },
                new LookupDepoLokasyon { Id = 5, Anahtar = 5, Deger = "Grid" }
            );

            // UrunDurum
            modelBuilder.Entity<LookupUrunDurum>().HasData(
                new LookupUrunDurum { Id = 1, Anahtar = 1, Deger = "Bekliyor" },
                new LookupUrunDurum { Id = 2, Anahtar = 2, Deger = "Kısmi Geldi" },
                new LookupUrunDurum { Id = 3, Anahtar = 3, Deger = "Tamamlandı" },
                new LookupUrunDurum { Id = 4, Anahtar = 4, Deger = "Eksik" },
                new LookupUrunDurum { Id = 5, Anahtar = 5, Deger = "Stoktan Karşılandı" },
                new LookupUrunDurum { Id = 6, Anahtar = 6, Deger = "FB'den Karşılandı" },
                new LookupUrunDurum { Id = 7, Anahtar = 7, Deger = "Sonra Gidecek" },
                new LookupUrunDurum { Id = 8, Anahtar = 8, Deger = "Sandık Değişti" },
                new LookupUrunDurum { Id = 9, Anahtar = 9, Deger = "İptal/Pasif" },
                new LookupUrunDurum { Id = 10, Anahtar = 10, Deger = "Teslim Alındı" },
                new LookupUrunDurum { Id = 11, Anahtar = 11, Deger = "Geri Gönderildi" },
                new LookupUrunDurum { Id = 12, Anahtar = 12, Deger = "Kısmi Tamamlandı" },
                new LookupUrunDurum { Id = 13, Anahtar = 13, Deger = "Kayıp" },
                new LookupUrunDurum { Id = 14, Anahtar = 14, Deger = "Grid'de Hazır" },
                new LookupUrunDurum { Id = 15, Anahtar = 15, Deger = "Grid'de Eksik" },
                new LookupUrunDurum { Id = 16, Anahtar = 16, Deger = "Siparişte" },
                new LookupUrunDurum { Id = 17, Anahtar = 17, Deger = "Gelmedi" },
                new LookupUrunDurum { Id = 18, Anahtar = 18, Deger = "Trafo Sevk" },
                new LookupUrunDurum { Id = 19, Anahtar = 19, Deger = "Başka Projeye Verildi" },
                new LookupUrunDurum { Id = 20, Anahtar = 20, Deger = "Hatalı Ürün" },
                new LookupUrunDurum { Id = 21, Anahtar = 21, Deger = "Hatalı/Uyumsuz Gönderim" }
            );

            // GridDurum
            modelBuilder.Entity<LookupGridDurum>().HasData(
                new LookupGridDurum { Id = 1, Anahtar = 1, Deger = "Bekliyor" },
                new LookupGridDurum { Id = 2, Anahtar = 2, Deger = "Üretimde" },
                new LookupGridDurum { Id = 3, Anahtar = 3, Deger = "Stok Hazır" },
                new LookupGridDurum { Id = 4, Anahtar = 4, Deger = "Sevk Edildi" },
                new LookupGridDurum { Id = 5, Anahtar = 5, Deger = "Kısmi Sevk Edildi" },
                new LookupGridDurum { Id = 6, Anahtar = 6, Deger = "Bekletiliyor" },
                new LookupGridDurum { Id = 7, Anahtar = 7, Deger = "İptal Edildi" },
                new LookupGridDurum { Id = 8, Anahtar = 8, Deger = "Tam Geldi" },
                new LookupGridDurum { Id = 9, Anahtar = 9, Deger = "Eksik Geldi" },
                new LookupGridDurum { Id = 10, Anahtar = 10, Deger = "Gelmedi" },
                new LookupGridDurum { Id = 11, Anahtar = 11, Deger = "Trafo Sevk" },
                new LookupGridDurum { Id = 12, Anahtar = 12, Deger = "İptal" },
                new LookupGridDurum { Id = 13, Anahtar = 13, Deger = "Siparişte" },
                new LookupGridDurum { Id = 14, Anahtar = 14, Deger = "Grid Kapandı" }
            );

            // GridSevkDurum
            modelBuilder.Entity<LookupGridSevkDurum>().HasData(
                new LookupGridSevkDurum { Id = 1, Anahtar = 1, Deger = "Sevk Edildi" },
                new LookupGridSevkDurum { Id = 2, Anahtar = 2, Deger = "Bekliyor" },
                new LookupGridSevkDurum { Id = 3, Anahtar = 3, Deger = "Sevk Edilmedi" }
            );

            // UcKDurum
            modelBuilder.Entity<LookupUcKDurum>().HasData(
                new LookupUcKDurum { Id = 1, Anahtar = 1, Deger = "Bekliyor" },
                new LookupUcKDurum { Id = 2, Anahtar = 2, Deger = "Sevk Adeti Tam Geldi" },
                new LookupUcKDurum { Id = 3, Anahtar = 3, Deger = "Sevk Adeti Eksik Geldi" },
                new LookupUcKDurum { Id = 4, Anahtar = 4, Deger = "Gelmedi" },
                new LookupUcKDurum { Id = 5, Anahtar = 5, Deger = "Tamamlandı" },
                new LookupUcKDurum { Id = 6, Anahtar = 6, Deger = "Kontrol Edildi" },
                new LookupUcKDurum { Id = 7, Anahtar = 7, Deger = "İade Edildi" },
                new LookupUcKDurum { Id = 8, Anahtar = 8, Deger = "Projeden Karşılandı" },
                new LookupUcKDurum { Id = 9, Anahtar = 9, Deger = "Stoktan Karşılandı" },
                new LookupUcKDurum { Id = 10, Anahtar = 10, Deger = "Tedarikçiden Geldi" },
                new LookupUcKDurum { Id = 11, Anahtar = 11, Deger = "Başka Projeye Verildi" },
                new LookupUcKDurum { Id = 12, Anahtar = 12, Deger = "Geri Gönderildi" },
                new LookupUcKDurum { Id = 13, Anahtar = 13, Deger = "Hatalı Ürün" }
            );

            // GeriGonderilmeSebebi
            modelBuilder.Entity<LookupGeriGonderilmeSebebi>().HasData(
                new LookupGeriGonderilmeSebebi { Id = 1, Anahtar = 1, Deger = "Tadilat" },
                new LookupGeriGonderilmeSebebi { Id = 2, Anahtar = 2, Deger = "Iptal" },
                new LookupGeriGonderilmeSebebi { Id = 3, Anahtar = 3, Deger = "Projeye Geri Dönüş" },
                new LookupGeriGonderilmeSebebi { Id = 4, Anahtar = 4, Deger = "Hatalı Ürün" }
            );

            // YetkiTipi
            modelBuilder.Entity<LookupYetkiTipi>().HasData(
                new LookupYetkiTipi { Id = 1, Anahtar = 1, Deger = "N" },
                new LookupYetkiTipi { Id = 2, Anahtar = 2, Deger = "R" },
                new LookupYetkiTipi { Id = 3, Anahtar = 3, Deger = "W" }
            );

            // StokDurum
            modelBuilder.Entity<LookupStokDurum>().HasData(
                new LookupStokDurum { Id = 1, Anahtar = 1, Deger = "Aktif" },
                new LookupStokDurum { Id = 2, Anahtar = 2, Deger = "Tukendi" },
                new LookupStokDurum { Id = 3, Anahtar = 3, Deger = "Rezerve" }
            );

            // IslemTipi
            modelBuilder.Entity<LookupIslemTipi>().HasData(
                new LookupIslemTipi { Id = 1, Anahtar = 1, Deger = "Çeki Yüklendi" },
                new LookupIslemTipi { Id = 2, Anahtar = 2, Deger = "Proje Oluşturuldu" },
                new LookupIslemTipi { Id = 3, Anahtar = 3, Deger = "Grid Durum Güncellendi" },
                new LookupIslemTipi { Id = 4, Anahtar = 4, Deger = "Grid Toplu Sevk Edildi" },
                new LookupIslemTipi { Id = 5, Anahtar = 5, Deger = "3K Durum Güncellendi" },
                new LookupIslemTipi { Id = 6, Anahtar = 6, Deger = "3K Teslim Alındı" },
                new LookupIslemTipi { Id = 7, Anahtar = 7, Deger = "3K Toplu Teslim Alındı" },
                new LookupIslemTipi { Id = 8, Anahtar = 8, Deger = "Manuel Ürün Eklendi" },
                new LookupIslemTipi { Id = 9, Anahtar = 9, Deger = "Sandık Ürün Taşıma" },
                new LookupIslemTipi { Id = 10, Anahtar = 10, Deger = "Ürün Güncellendi" },
                new LookupIslemTipi { Id = 11, Anahtar = 11, Deger = "Ürün İptal Edildi" },
                new LookupIslemTipi { Id = 12, Anahtar = 12, Deger = "Stoktan Karşılandı" },
                new LookupIslemTipi { Id = 13, Anahtar = 13, Deger = "F.B.'den Karşılandı" },
                new LookupIslemTipi { Id = 14, Anahtar = 14, Deger = "Sandık Manuel Kapatma" },
                new LookupIslemTipi { Id = 15, Anahtar = 15, Deger = "Toplu Sandık Kapatıldı" },
                new LookupIslemTipi { Id = 16, Anahtar = 16, Deger = "Fiili Sandık Değiştirildi" },
                new LookupIslemTipi { Id = 17, Anahtar = 17, Deger = "Lokasyon Güncelleme" },
                new LookupIslemTipi { Id = 18, Anahtar = 18, Deger = "Sandık Otomatik Hazırlandı" },
                new LookupIslemTipi { Id = 19, Anahtar = 19, Deger = "Excel İndirildi" },
                new LookupIslemTipi { Id = 20, Anahtar = 20, Deger = "PDF İndirildi" },
                new LookupIslemTipi { Id = 21, Anahtar = 21, Deger = "Sandık Oluşturuldu" },
                new LookupIslemTipi { Id = 22, Anahtar = 22, Deger = "Kullanıcı Oluşturuldu" },
                new LookupIslemTipi { Id = 23, Anahtar = 23, Deger = "Proje Sevk Edildi" },
                new LookupIslemTipi { Id = 24, Anahtar = 24, Deger = "Sandık Sevk Edildi" },
                new LookupIslemTipi { Id = 25, Anahtar = 25, Deger = "Saha/Yedek Malzeme Eklendi" },
                new LookupIslemTipi { Id = 26, Anahtar = 26, Deger = "Toplu Durum Güncellendi" },
                new LookupIslemTipi { Id = 27, Anahtar = 27, Deger = "Not Eklendi" },
                new LookupIslemTipi { Id = 28, Anahtar = 28, Deger = "Manuel Ürün Sandığa Eklendi" },
                new LookupIslemTipi { Id = 29, Anahtar = 29, Deger = "Sandık Kapandı" },
                new LookupIslemTipi { Id = 30, Anahtar = 30, Deger = "3K Durum Sıfırlandı" },
                new LookupIslemTipi { Id = 31, Anahtar = 31, Deger = "Grid Durum Sıfırlandı" },
                new LookupIslemTipi { Id = 32, Anahtar = 32, Deger = "Manuel Ürün Silindi" },
                new LookupIslemTipi { Id = 33, Anahtar = 33, Deger = "Sandık Silindi" }
            );

            // ProjeTipi
            modelBuilder.Entity<LookupProjeTipi>().HasData(
                new LookupProjeTipi { Id = 1, Anahtar = 1, Deger = "Normal" },
                new LookupProjeTipi { Id = 2, Anahtar = 2, Deger = "Saha" },
                new LookupProjeTipi { Id = 3, Anahtar = 3, Deger = "Yedek" }
            );

            // Birim (Madde 7)
            modelBuilder.Entity<LookupBirim>().HasData(
                new LookupBirim { Id = 1, Anahtar = 1, Deger = "Adet" },
                new LookupBirim { Id = 2, Anahtar = 2, Deger = "Set" },
                new LookupBirim { Id = 3, Anahtar = 3, Deger = "Metre" },
                new LookupBirim { Id = 4, Anahtar = 4, Deger = "Kg" },
                new LookupBirim { Id = 5, Anahtar = 5, Deger = "Litre" },
                new LookupBirim { Id = 6, Anahtar = 6, Deger = "Takım" },
                new LookupBirim { Id = 7, Anahtar = 7, Deger = "Paket" },
                new LookupBirim { Id = 8, Anahtar = 8, Deger = "Ton" },
                new LookupBirim { Id = 9, Anahtar = 9, Deger = "Metrekare" },
                new LookupBirim { Id = 10, Anahtar = 10, Deger = "Metreküp" }
            );


            SeedApprovalRules(modelBuilder);
        }

        private static void SeedApprovalRules(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IslemOnayKurali>().HasData(
                new IslemOnayKurali { Id = 1, LookupUcKDurumId = 8, OnayGerektirirMi = true }, // ProjedenKarşılandı
                new IslemOnayKurali { Id = 2, LookupUcKDurumId = 9, OnayGerektirirMi = true }, // StoktanKarşılandı
                new IslemOnayKurali { Id = 3, LookupUcKDurumId = 10, OnayGerektirirMi = true }, // TedarikçidenGeldi
                
                new IslemOnayKurali { Id = 4, LookupUcKDurumId = 2, OnayGerektirirMi = false }, // TamGeldi
                new IslemOnayKurali { Id = 5, LookupUcKDurumId = 3, OnayGerektirirMi = false }, // EksikGeldi
                new IslemOnayKurali { Id = 6, LookupUcKDurumId = 4, OnayGerektirirMi = false }, // Gelmedi
                new IslemOnayKurali { Id = 7, LookupUcKDurumId = 11, OnayGerektirirMi = false }, // BaşkaProjeyeVerildi
                new IslemOnayKurali { Id = 8, LookupUcKDurumId = 12, OnayGerektirirMi = false }, // GeriGönderildi
                new IslemOnayKurali { Id = 9, LookupUcKDurumId = 13, OnayGerektirirMi = false } // HatalıÜrün
            );
        }

        /// <summary>
        /// RBAC seed data: Roller, MenuTanimi ağacı, ve Admin yetkilerini oluşturur.
        /// </summary>
        private static void SeedRbacData(ModelBuilder modelBuilder)
        {
            // ======= ROLLER =======
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Ad = "Admin" },
                new Rol { Id = 2, Ad = "Personel3K" },
                new Rol { Id = 3, Ad = "PersonelGrid" },
                new Rol { Id = 4, Ad = "Yonetici" }
            );

            // ======= MENÜ TANIMLARI (ağaç yapısı) =======
            // Root menüler
            modelBuilder.Entity<MenuTanimi>().HasData(
                new MenuTanimi { Id = 1, Kod = "dashboard", LabelKey = "MENU.DASHBOARD", Icon = "ri-dashboard-line", Route = "/dashboard", Sira = 1, ParentId = null },
                new MenuTanimi { Id = 2, Kod = "projeler", LabelKey = "MENU.PROJELER", Icon = "ri-folder-line", Route = null, Sira = 2, ParentId = null },
                new MenuTanimi { Id = 5, Kod = "sandik-yonetimi", LabelKey = "MENU.SANDIK_YONETIMI", Icon = "ri-archive-line", Route = "/sandik-yonetimi", Sira = 3, ParentId = null },
                new MenuTanimi { Id = 7, Kod = "depo-durumu", LabelKey = "MENU.DEPO_DURUMU", Icon = "ri-building-2-line", Route = "/depo-durumu", Sira = 4, ParentId = null },
                new MenuTanimi { Id = 8, Kod = "stok", LabelKey = "MENU.STOK_MODULU", Icon = "ri-stack-line", Route = "/stok", Sira = 5, ParentId = null },
                new MenuTanimi { Id = 10, Kod = "hareket-gecmisi", LabelKey = "MENU.HAREKET_GECMISI", Icon = "ri-history-line", Route = "/hareket-gecmisi", Sira = 8, ParentId = null },
                new MenuTanimi { Id = 11, Kod = "kullanicilar", LabelKey = "MENU.KULLANICI_YETKI", Icon = "ri-user-settings-line", Route = "/kullanicilar", Sira = 9, ParentId = null },
                new MenuTanimi { Id = 12, Kod = "rol-yonetimi", LabelKey = "MENU.ROL_YONETIMI", Icon = "ri-shield-user-line", Route = "/rol-yonetimi", Sira = 10, ParentId = null }
            );

            // Alt menüler (Projeler children)
            modelBuilder.Entity<MenuTanimi>().HasData(
                new MenuTanimi { Id = 3, Kod = "aktif-projeler", LabelKey = "MENU.AKTIF_PROJELER", Icon = "", Route = "/projeler", Sira = 1, ParentId = 2 },
                new MenuTanimi { Id = 4, Kod = "sevk-edilen", LabelKey = "MENU.SEVK_EDILEN", Icon = "", Route = "/projeler/sevk-edilen", Sira = 2, ParentId = 2 },
                // Yetki kontrollü butonlar — sidebar'da GÖRÜNMEZler (Route=null).
                new MenuTanimi { Id = 14, Kod = "grid-modulu", LabelKey = "MENU.GRID_MODULU", Icon = "", Route = null, Sira = 3, ParentId = 2 },
                new MenuTanimi { Id = 15, Kod = "3k-modulu", LabelKey = "MENU.3K_MODULU", Icon = "", Route = null, Sira = 4, ParentId = 2 },
                new MenuTanimi { Id = 16, Kod = "proje-sevk-et", LabelKey = "MENU.PROJE_SEVK_ET", Icon = "", Route = null, Sira = 5, ParentId = 2 },
                // Saha ve Yedek Menüleri
                new MenuTanimi { Id = 17, Kod = "saha-yonetimi", LabelKey = "MENU.SAHA_YONETIMI", Icon = "ri-tools-line", Route = "/saha-yonetimi", Sira = 6, ParentId = null },
                new MenuTanimi { Id = 18, Kod = "yedek-yonetimi", LabelKey = "MENU.YEDEK_YONETIMI", Icon = "ri-box-3-line", Route = "/yedek-yonetimi", Sira = 7, ParentId = null },
                // Onay Merkezi
                new MenuTanimi { Id = 99, Kod = "islem-onay-merkezi", LabelKey = "MENU.ISLEM_ONAY", Icon = "ri-check-double-line", Route = "/onay-merkezi", Sira = 11, ParentId = null }
            );

            // ======= ADMIN ROL YETKİLERİ (tüm menülere W=3) =======
            // Not: MenuTanimi Id'leri: 1,2,3,4,5,7,8,10,11,12,14,15,16,17,18,99
            var menuIds = new[] { 1, 2, 3, 4, 5, 7, 8, 10, 11, 12, 14, 15, 16, 17, 18 };
            var adminYetkiler = new List<RolYetki>();
            for (int i = 0; i < menuIds.Length; i++)
            {
                adminYetkiler.Add(new RolYetki { Id = i + 1, RolId = 1, MenuTanimiId = menuIds[i], YetkiTipiId = (int)_3K.Core.Enums.YetkiTipi.W });
            }
            adminYetkiler.Add(new RolYetki { Id = 99, RolId = 1, MenuTanimiId = 99, YetkiTipiId = (int)_3K.Core.Enums.YetkiTipi.W });
            modelBuilder.Entity<RolYetki>().HasData(adminYetkiler);
        }

        // Audit alanları (CreatedDate/By, UpdatedDate/By) artık
        // AuditInterceptor tarafından otomatik doldurulur.
    }
}
