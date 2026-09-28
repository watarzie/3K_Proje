using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Rol yetkileri ve menü tanımı ağacının eşlemesi; roller, menüler ve Admin yetkilerinin başlangıç verisi.</summary>
internal static class RbacModelBuilderExtensions
{
    public static void ConfigureRbacModule(this ModelBuilder modelBuilder)
    {
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
    }

    /// <summary>
    /// RBAC seed data: Roller, MenuTanimi ağacı, ve Admin yetkilerini oluşturur.
    /// </summary>
    public static void SeedRbacData(this ModelBuilder modelBuilder)
    {
        // ======= ROLLER =======
        modelBuilder.Entity<Rol>().HasData(
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Ad = "Admin" },
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Ad = "Personel3K" },
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Ad = "PersonelGrid" },
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Ad = "Yonetici" },
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Ad = "Kalite" },
            new Rol { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Ad = "Surec" }
        );

        // ======= MENÜ TANIMLARI (ağaç yapısı) =======
        // Root menüler
        modelBuilder.Entity<MenuTanimi>().HasData(
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Kod = "dashboard", LabelKey = "MENU.DASHBOARD", Icon = "ri-dashboard-line", Route = "/dashboard", Sira = 1, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Kod = "projeler", LabelKey = "MENU.PROJELER", Icon = "ri-folder-line", Route = null, Sira = 2, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Kod = "sandik-yonetimi", LabelKey = "MENU.SANDIK_YONETIMI", Icon = "ri-archive-line", Route = "/sandik-yonetimi", Sira = 3, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Kod = "depo-durumu", LabelKey = "MENU.DEPO_DURUMU", Icon = "ri-building-2-line", Route = "/depo-durumu", Sira = 4, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Kod = "stok", LabelKey = "MENU.STOK_MODULU", Icon = "ri-stack-line", Route = "/stok", Sira = 5, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 46, Kod = "ambalaj-uretim-listesi", LabelKey = "MENU.AMBALAJ_URETIM_LISTESI", Icon = "ri-hammer-line", Route = "/ambalaj-uretim-listesi", Sira = 8, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 47, Kod = "finans-yonetimi", LabelKey = "MENU.FINANS_YONETIMI", Icon = "ri-line-chart-line", Route = "/finans-yonetimi", Sira = 9, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Kod = "hareket-gecmisi", LabelKey = "MENU.HAREKET_GECMISI", Icon = "ri-history-line", Route = "/hareket-gecmisi", Sira = 10, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 11, Kod = "kullanicilar", LabelKey = "MENU.KULLANICI_YETKI", Icon = "ri-user-settings-line", Route = "/kullanicilar", Sira = 11, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 12, Kod = "rol-yonetimi", LabelKey = "MENU.ROL_YONETIMI", Icon = "ri-shield-user-line", Route = "/rol-yonetimi", Sira = 12, ParentId = null }
        );

        // Alt menüler ve yetki kontrollü modül düğümleri
        modelBuilder.Entity<MenuTanimi>().HasData(
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Kod = "aktif-projeler", LabelKey = "MENU.AKTIF_PROJELER", Icon = "", Route = "/projeler", Sira = 1, ParentId = 2 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Kod = "sevk-edilen", LabelKey = "MENU.SEVK_EDILEN", Icon = "", Route = "/projeler/sevk-edilen", Sira = 2, ParentId = 2 },
            // Sandık Yönetimi altında yetki kontrollü modüller — sidebar'da GÖRÜNMEZler (Route=null).
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 14, Kod = "grid-modulu", LabelKey = "MENU.GRID_MODULU", Icon = "", Route = null, Sira = 1, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 15, Kod = "3k-modulu", LabelKey = "MENU.3K_MODULU", Icon = "", Route = null, Sira = 2, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 16, Kod = "proje-sevk-et", LabelKey = "MENU.PROJE_SEVK_ET", Icon = "", Route = null, Sira = 5, ParentId = 2 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 24, Kod = "proje-sil", LabelKey = "MENU.PROJE_SIL", Icon = "", Route = null, Sira = 6, ParentId = 2 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 44, Kod = "ceki-yukle", LabelKey = "MENU.CEKI_YUKLE", Icon = "", Route = null, Sira = 7, ParentId = 2 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 45, Kod = "planlanan-sevk-tarihi", LabelKey = "MENU.PLANLANAN_SEVK_TARIHI", Icon = "", Route = null, Sira = 8, ParentId = 2 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 28, Kod = "eksik-saha-projesi", LabelKey = "MENU.EKSIK_SAHA_PROJESI", Icon = "", Route = null, Sira = 7, ParentId = 17 },
            // Saha ve Yedek Menüleri
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 17, Kod = "saha-yonetimi", LabelKey = "MENU.SAHA_YONETIMI", Icon = "ri-tools-line", Route = "/saha-yonetimi", Sira = 6, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 29, Kod = "saha-grid-modulu", LabelKey = "MENU.SAHA_GRID_MODULU", Icon = "", Route = null, Sira = 1, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 30, Kod = "saha-3k-modulu", LabelKey = "MENU.SAHA_3K_MODULU", Icon = "", Route = null, Sira = 2, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 31, Kod = "saha-raporu", LabelKey = "MENU.SAHA_RAPORU", Icon = "", Route = null, Sira = 3, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 32, Kod = "saha-sandiklar", LabelKey = "MENU.SAHA_SANDIKLAR", Icon = "", Route = null, Sira = 4, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 33, Kod = "saha-sevk-et", LabelKey = "MENU.SAHA_SEVK_ET", Icon = "", Route = null, Sira = 5, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 34, Kod = "saha-proje-sil", LabelKey = "MENU.SAHA_PROJE_SIL", Icon = "", Route = null, Sira = 6, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 18, Kod = "yedek-yonetimi", LabelKey = "MENU.YEDEK_YONETIMI", Icon = "ri-box-3-line", Route = "/yedek-yonetimi", Sira = 7, ParentId = null },
            // Onay Merkezi
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 99, Kod = "islem-onay-merkezi", LabelKey = "MENU.ISLEM_ONAY", Icon = "ri-check-double-line", Route = "/onay-merkezi", Sira = 13, ParentId = null },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 43, Kod = "onay-kurallari-yonet", LabelKey = "MENU.ONAY_KURALLARI_YONET", Icon = "", Route = null, Sira = 1, ParentId = 99 },
            // Kalite & Süreç — Sandık Yönetimi altında yetki kontrollü butonlar.
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 20, Kod = "kalite-modulu", LabelKey = "MENU.KALITE_MODULU", Icon = "", Route = null, Sira = 3, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 21, Kod = "surec-modulu", LabelKey = "MENU.SUREC_MODULU", Icon = "", Route = null, Sira = 4, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 22, Kod = "eksik-raporu", LabelKey = "MENU.EKSIK_RAPORU", Icon = "", Route = null, Sira = 5, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 23, Kod = "gerceklesen-ceki-raporu", LabelKey = "MENU.GERCEKLESEN_CEKI_RAPORU", Icon = "", Route = null, Sira = 6, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 25, Kod = "ceki-verisi-duzenle", LabelKey = "MENU.CEKI_VERISI_DUZENLE", Icon = "", Route = null, Sira = 7, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 26, Kod = "3k-is-listesi", LabelKey = "MENU.3K_IS_LISTESI", Icon = "ri-list-check-3-line", Route = "/uck-is-listesi", Sira = 8, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 27, Kod = "ceki-verisi-sil", LabelKey = "MENU.CEKI_VERISI_SIL", Icon = "", Route = null, Sira = 9, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 35, Kod = "ceki-revizyon-yukle", LabelKey = "MENU.CEKI_REVIZYON_YUKLE", Icon = "", Route = null, Sira = 10, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 36, Kod = "3k-sandik-durum-raporu", LabelKey = "MENU.3K_SANDIK_DURUM_RAPORU", Icon = "", Route = null, Sira = 11, ParentId = 5 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 37, Kod = "saha-sevk-sonrasi-eksik-raporu", LabelKey = "MENU.SAHA_SEVK_SONRASI_EKSIK_RAPORU", Icon = "", Route = null, Sira = 7, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 38, Kod = "saha-3k-sandik-durum-raporu", LabelKey = "MENU.SAHA_3K_SANDIK_DURUM_RAPORU", Icon = "", Route = null, Sira = 8, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 42, Kod = "saha-gerceklesen-ceki-raporu", LabelKey = "MENU.SAHA_GERCEKLESEN_CEKI_RAPORU", Icon = "", Route = null, Sira = 9, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 40, Kod = "sahaya-aktar", LabelKey = "MENU.SAHAYA_AKTAR", Icon = "", Route = null, Sira = 10, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 41, Kod = "saha-aktarim-geri-al", LabelKey = "MENU.SAHA_AKTARIM_GERI_AL", Icon = "", Route = null, Sira = 11, ParentId = 17 },
            new MenuTanimi { CreatedDate = new DateTime(2026, 5, 11), Id = 39, Kod = "stok-sil", LabelKey = "MENU.STOK_SIL", Icon = "", Route = null, Sira = 1, ParentId = 8 }
        );

        // ======= ADMIN ROL YETKİLERİ (tüm menülere W=3) =======
        // Yeni modüller güvenli varsayılanla yalnız Admin rolüne W olarak verilir.
        // Diğer roller mevcut Rol Yönetimi ekranından açıkça yetkilendirilir.
        var menuIds = new[] { 1, 2, 3, 4, 5, 7, 8, 10, 11, 12, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45 };
        var adminYetkiler = new List<RolYetki>();
        for (int i = 0; i < menuIds.Length; i++)
        {
            adminYetkiler.Add(new RolYetki { CreatedDate = new DateTime(2026, 5, 11), Id = i + 1, RolId = 1, MenuTanimiId = menuIds[i], YetkiTipiId = (int)_3K.Core.Enums.YetkiTipi.W });
        }
        adminYetkiler.Add(new RolYetki { CreatedDate = new DateTime(2026, 5, 11), Id = 99, RolId = 1, MenuTanimiId = 99, YetkiTipiId = (int)_3K.Core.Enums.YetkiTipi.W });
        // Yeni modüllere başlangıç yetkisi koddan atanmaz. Ambalaj ve Finans
        // R/W seçimleri mevcut Rol Yönetimi akışıyla RolYetkileri tablosuna yazılır.
        modelBuilder.Entity<RolYetki>().HasData(adminYetkiler);
    }
}
