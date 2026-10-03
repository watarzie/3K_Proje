using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Lookup (parametre) tablolarının eşlemesi ve başlangıç verisi.</summary>
internal static class LookupModelBuilderExtensions
{
    public static void ConfigureLookupTables(this ModelBuilder modelBuilder)
    {
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
        ConfigureLookupTable<LookupKaliteDurum>(modelBuilder);
        ConfigureLookupTable<LookupSurecDurum>(modelBuilder);
    }

    /// <summary>
    /// Lookup tablosu konfigürasyonu: Deger alanı unique index olarak tanımlanır.
    /// AlternateKey KALDIRILDI — FK ilişkileri artık Id (PK) üzerinden kurulur.
    /// </summary>
    private static void ConfigureLookupTable<T>(ModelBuilder modelBuilder) where T : LookupBase
    {
        modelBuilder.Entity<T>().HasIndex(l => l.Deger).IsUnique();
    }

    public static void SeedLookupData(this ModelBuilder modelBuilder)
    {
        // ProjeDurum
        modelBuilder.Entity<LookupProjeDurum>().HasData(
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Hazırlanıyor" },
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Devam" },
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Tamamlandı" },
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Beklemede" },
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Sevk Edildi" },
            new LookupProjeDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "Eksik Sevk Edildi" }
        );

        // SandikDurum (Madde 6: Kapandı eklendi)
        modelBuilder.Entity<LookupSandikDurum>().HasData(
            new LookupSandikDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Boş" },
            new LookupSandikDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Hazırlanıyor" },
            new LookupSandikDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Kapandı" },
            new LookupSandikDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Sevk Edildi" }
        );

        // SandikTipi (fiziksel sandık cinsi)
        modelBuilder.Entity<LookupSandikTipi>().HasData(
            new LookupSandikTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Ahşap Kapalı" },
            new LookupSandikTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Katlanır Sandık" },
            new LookupSandikTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Kontrplak Sandık" }
        );

        // DepoLokasyon
        modelBuilder.Entity<LookupDepoLokasyon>().HasData(
            new LookupDepoLokasyon { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Belirsiz" },
            new LookupDepoLokasyon { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "3K" },
            new LookupDepoLokasyon { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Seymen" },
            new LookupDepoLokasyon { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Grid" }
        );

        // UrunDurum
        modelBuilder.Entity<LookupUrunDurum>().HasData(
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Bekliyor" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Kısmi Geldi" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Tamamlandı" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Eksik" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Stoktan Karşılandı" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "FB'den Karşılandı" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Anahtar = 7, Deger = "Sonra Gidecek" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Anahtar = 8, Deger = "Sandık Değişti" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 9, Anahtar = 9, Deger = "İptal/Pasif" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Anahtar = 10, Deger = "Teslim Alındı" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 11, Anahtar = 11, Deger = "Geri Gönderildi" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 12, Anahtar = 12, Deger = "Kısmi Tamamlandı" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 13, Anahtar = 13, Deger = "Kayıp" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 14, Anahtar = 14, Deger = "Grid'de Hazır" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 15, Anahtar = 15, Deger = "Grid'de Eksik" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 16, Anahtar = 16, Deger = "Siparişte" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 17, Anahtar = 17, Deger = "Gelmedi" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 18, Anahtar = 18, Deger = "Trafo Sevk" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 19, Anahtar = 19, Deger = "Başka Projeye Verildi" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 20, Anahtar = 20, Deger = "Hatalı Ürün" },
            new LookupUrunDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 21, Anahtar = 21, Deger = "Hatalı/Uyumsuz Gönderim" }
        );

        // GridDurum
        modelBuilder.Entity<LookupGridDurum>().HasData(
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Bekliyor" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Üretimde" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Stok Hazır" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Sevk Edildi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Kısmi Sevk Edildi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "Bekletiliyor" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Anahtar = 7, Deger = "İptal Edildi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Anahtar = 8, Deger = "Tam Geldi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 9, Anahtar = 9, Deger = "Eksik Geldi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Anahtar = 10, Deger = "Gelmedi" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 11, Anahtar = 11, Deger = "Trafo Sevk" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 12, Anahtar = 12, Deger = "İptal" },
            new LookupGridDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 14, Anahtar = 14, Deger = "Grid Kapandı" }
        );

        // GridSevkDurum
        modelBuilder.Entity<LookupGridSevkDurum>().HasData(
            new LookupGridSevkDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Sevk Edildi" },
            new LookupGridSevkDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Bekliyor" },
            new LookupGridSevkDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Sevk Edilmedi" },
            new LookupGridSevkDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Yeniden Sevk Gerekli" }
        );

        // UcKDurum
        modelBuilder.Entity<LookupUcKDurum>().HasData(
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Bekliyor" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Sevk Adeti Tam Geldi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Sevk Adeti Eksik Geldi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Gelmedi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Tamamlandı" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "Kontrol Edildi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Anahtar = 7, Deger = "İade Edildi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Anahtar = 8, Deger = "Projeden Karşılandı" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 9, Anahtar = 9, Deger = "Stoktan Karşılandı" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Anahtar = 10, Deger = "Tedarikçiden Geldi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 11, Anahtar = 11, Deger = "Başka Projeye Verildi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 12, Anahtar = 12, Deger = "Geri Gönderildi" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 13, Anahtar = 13, Deger = "Hatalı Ürün" },
            new LookupUcKDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 14, Anahtar = 14, Deger = "Fazla Geldi" }
        );

        // GeriGonderilmeSebebi
        modelBuilder.Entity<LookupGeriGonderilmeSebebi>().HasData(
            new LookupGeriGonderilmeSebebi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Tadilat" },
            new LookupGeriGonderilmeSebebi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Iptal" },
            new LookupGeriGonderilmeSebebi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Projeye Geri Dönüş" },
            new LookupGeriGonderilmeSebebi { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Hatalı Ürün" }
        );

        // YetkiTipi
        modelBuilder.Entity<LookupYetkiTipi>().HasData(
            new LookupYetkiTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "N" },
            new LookupYetkiTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "R" },
            new LookupYetkiTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "W" }
        );

        // StokDurum
        modelBuilder.Entity<LookupStokDurum>().HasData(
            new LookupStokDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Aktif" },
            new LookupStokDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Tukendi" },
            new LookupStokDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Rezerve" }
        );

        // IslemTipi
        modelBuilder.Entity<LookupIslemTipi>().HasData(
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Çeki Yüklendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Proje Oluşturuldu" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Grid Durum Güncellendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Grid Toplu Sevk Edildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "3K Durum Güncellendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "3K Teslim Alındı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Anahtar = 7, Deger = "3K Toplu Teslim Alındı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Anahtar = 8, Deger = "Manuel Ürün Eklendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 9, Anahtar = 9, Deger = "Sandık Ürün Taşıma" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Anahtar = 10, Deger = "Ürün Güncellendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 11, Anahtar = 11, Deger = "Ürün İptal Edildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 12, Anahtar = 12, Deger = "Stoktan Karşılandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 13, Anahtar = 13, Deger = "F.B.'den Karşılandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 14, Anahtar = 14, Deger = "Sandık Manuel Kapatma" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 15, Anahtar = 15, Deger = "Toplu Sandık Kapatıldı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 16, Anahtar = 16, Deger = "Fiili Sandık Değiştirildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 17, Anahtar = 17, Deger = "Lokasyon Güncelleme" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 18, Anahtar = 18, Deger = "Sandık Otomatik Hazırlandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 19, Anahtar = 19, Deger = "Excel İndirildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 20, Anahtar = 20, Deger = "PDF İndirildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 21, Anahtar = 21, Deger = "Sandık Oluşturuldu" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 22, Anahtar = 22, Deger = "Kullanıcı Oluşturuldu" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 23, Anahtar = 23, Deger = "Proje Sevk Edildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 24, Anahtar = 24, Deger = "Sandık Sevk Edildi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 25, Anahtar = 25, Deger = "Saha/Yedek Malzeme Eklendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 26, Anahtar = 26, Deger = "Toplu Durum Güncellendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 27, Anahtar = 27, Deger = "Not Eklendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 28, Anahtar = 28, Deger = "Manuel Ürün Sandığa Eklendi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 29, Anahtar = 29, Deger = "Sandık Kapandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 30, Anahtar = 30, Deger = "3K Durum Sıfırlandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 31, Anahtar = 31, Deger = "Grid Durum Sıfırlandı" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 32, Anahtar = 32, Deger = "Manuel Ürün Silindi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 33, Anahtar = 33, Deger = "Sandık Silindi" },
            new LookupIslemTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 34, Anahtar = 34, Deger = "Fazla Teslim Stoka Aktarıldı" }
        );

        // ProjeTipi
        modelBuilder.Entity<LookupProjeTipi>().HasData(
            new LookupProjeTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Normal" },
            new LookupProjeTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Saha" },
            new LookupProjeTipi { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Yedek" }
        );

        // Birim (Madde 7)
        modelBuilder.Entity<LookupBirim>().HasData(
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Adet" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Set" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Metre" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Kg" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Litre" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "Takım" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 7, Anahtar = 7, Deger = "Paket" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 8, Anahtar = 8, Deger = "Ton" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 9, Anahtar = 9, Deger = "Metrekare" },
            new LookupBirim { CreatedDate = new DateTime(2026, 5, 11), Id = 10, Anahtar = 10, Deger = "Metreküp" }
        );

        // KaliteDurum
        modelBuilder.Entity<LookupKaliteDurum>().HasData(
            new LookupKaliteDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Onaylandı" },
            new LookupKaliteDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "Tadilatta" }
        );

        // SurecDurum
        modelBuilder.Entity<LookupSurecDurum>().HasData(
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 1, Anahtar = 1, Deger = "Ambar" },
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 2, Anahtar = 2, Deger = "İmalat" },
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 3, Anahtar = 3, Deger = "Tedarik" },
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 4, Anahtar = 4, Deger = "Tedarik 3K Teslim" },
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 5, Anahtar = 5, Deger = "Siparişte" },
            new LookupSurecDurum { CreatedDate = new DateTime(2026, 5, 11), Id = 6, Anahtar = 6, Deger = "Tamamlandı" }
        );
    }
}
