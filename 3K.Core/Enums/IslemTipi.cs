namespace _3K.Core.Enums
{
    /// <summary>
    /// LookupIslemTipi Id'leriyle birebir eşleşir. 13 kayıt.
    /// </summary>
    public enum IslemTipi
    {
        CekiYuklendi = 1,
        ProjeOlusturuldu = 2,
        GridDurumGuncellendi = 3,
        GridTopluSevkEdildi = 4,
        UcKDurumGuncellendi = 5,
        UcKTeslimAlindi = 6,
        UcKTopluTeslimAlindi = 7,
        ManuelUrunEklendi = 8,
        UrunTasindi = 9,
        UrunGuncellendi = 10,
        UrunIptalEdildi = 11,
        StoktanKarsilandi = 12,
        FBDenKarsilandi = 13,
        SandikKapatildi = 14,
        TopluSandikKapatildi = 15,
        FiiliSandikDegistirildi = 16,
        SandikLokasyonGuncellendi = 17,
        SandikOtomatikHazirlandi = 18,
        ExcelIndirildi = 19,
        PDFIndirildi = 20,
        SandikOlusturuldu = 21,
        KullaniciOlusturuldu = 22,
        ProjeSevkEdildi = 23,
        SandikSevkEdildi = 24,
        SahaYedekMalzemeEklendi = 25,
        /// <summary>Madde 5: Toplu durum güncelleme işlemi.</summary>
        TopluDurumGuncellendi = 26,
        /// <summary>Madde 9: Not ekleme işlemi.</summary>
        NotEklendi = 27,
        /// <summary>Madde 8: Sandık içine manuel ürün ekleme.</summary>
        ManuelUrunSandikEklendi = 28,
        /// <summary>Madde 6: Sandık otomatik/manuel kapatma.</summary>
        SandikKapandi = 29,
        /// <summary>3K durum sıfırlama (geri alma) işlemi.</summary>
        UcKDurumSifirlandi = 30,
        /// <summary>Grid durum sıfırlama (geri alma) işlemi.</summary>
        GridDurumSifirlandi = 31,
        /// <summary>Manuel eklenen ürün silindi.</summary>
        ManuelUrunSilindi = 32,
        /// <summary>Sandık silindi.</summary>
        SandikSilindi = 33
    }
}
