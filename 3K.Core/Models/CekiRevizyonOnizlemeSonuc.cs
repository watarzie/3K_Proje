namespace _3K.Core.Models
{
    public class CekiRevizyonOnizlemeSonuc
    {
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public int AnaCekiId { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public int ToplamIsaretliSatirSayisi { get; set; }
        public int EklenenSatirSayisi { get; set; }
        public int GuncellenenSatirSayisi { get; set; }
        public int SilinecekSatirSayisi { get; set; }
        public int RiskliSatirSayisi { get; set; }
        public int EngelliSatirSayisi { get; set; }
        public bool UygulanabilirMi { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public List<string> Uyarilar { get; set; } = new();
        public List<CekiRevizyonSandikEtkisi> SandikEtkileri { get; set; } = new();
        public List<CekiRevizyonOnizlemeSatiri> Satirlar { get; set; } = new();
    }

    /// <summary>
    /// Revizyon uygulandığında satır değişikliklerine ek olarak oluşacak veya
    /// güncellenecek sandığın fiziksel/isimsel alanlarını gösterir.
    /// Mevcut sandıklarda yalnızca gerçekten değişecek alan çiftleri doludur.
    /// </summary>
    public sealed class CekiRevizyonSandikEtkisi
    {
        public string SandikNo { get; set; } = string.Empty;
        public bool YeniSandikMi { get; set; }
        public bool DurumYenidenHesaplanacakMi { get; set; }
        public bool BosKalirsaSilinecekMi { get; set; }
        public int? EskiDurumId { get; set; }
        public int MevcutIcerikSayisi { get; set; }
        public int MevcutCekiIcerigiSayisi { get; set; }
        public int TamamlanmisCekiIcerigiSayisi { get; set; }
        public string? EskiAd { get; set; }
        public string? YeniAd { get; set; }
        public string? EskiAdIngilizce { get; set; }
        public string? YeniAdIngilizce { get; set; }
        public decimal? EskiEn { get; set; }
        public decimal? YeniEn { get; set; }
        public decimal? EskiBoy { get; set; }
        public decimal? YeniBoy { get; set; }
        public decimal? EskiYukseklik { get; set; }
        public decimal? YeniYukseklik { get; set; }
        public decimal? EskiNetKg { get; set; }
        public decimal? YeniNetKg { get; set; }
        public decimal? EskiGrossKg { get; set; }
        public decimal? YeniGrossKg { get; set; }
    }

    public class CekiRevizyonOnizlemeSatiri
    {
        public int ExcelSatirNo { get; set; }
        public string CheckKodu { get; set; } = string.Empty;
        public string IslemTipi { get; set; } = string.Empty;
        public string RiskSeviyesi { get; set; } = "Güvenli";
        public bool UygulanabilirMi { get; set; } = true;
        public string Mesaj { get; set; } = string.Empty;
        public int? MevcutCekiSatiriId { get; set; }
        public int? EskiSiraNo { get; set; }
        public int YeniSiraNo { get; set; }
        public string? BarkodNo { get; set; }
        public string? PozNo { get; set; }
        public string? Tanim { get; set; }
        public string? EskiKoliNo { get; set; }
        public string? YeniKoliNo { get; set; }
        public decimal? EskiIstenenAdet { get; set; }
        public decimal? YeniIstenenAdet { get; set; }
        public bool IslemGormusMu { get; set; }
        public decimal IslemGorenAdet { get; set; }
        public CekiRevizyonGeriAlmaEtkisi? GeriAlmaEtkisi { get; set; }
        public List<string> Degisiklikler { get; set; } = new();
        /// <summary>
        /// Satırın uygulanmasını engelleyen nedenler. Uyarilar alanı geriye
        /// uyumluluk için korunur; yeni istemciler engelleri bu alandan gösterir.
        /// </summary>
        public List<string> Engeller { get; set; } = new();
        public List<CekiRevizyonSorunu> Sorunlar { get; set; } = new();
        public List<string> Uyarilar { get; set; } = new();
    }

    /// <summary>
    /// U satırında otomatik geri alınacak, D satırında satırla birlikte
    /// kaldırılacak operasyon izlerinin talep anındaki typed özetidir.
    /// </summary>
    public sealed class CekiRevizyonGeriAlmaEtkisi
    {
        public int GridDurumuId { get; set; }
        public decimal GridGelenAdet { get; set; }
        public decimal TrafoSevkAdet { get; set; }
        public int GridSevkDurumuId { get; set; }
        public decimal GridSevkMiktari { get; set; }
        public decimal YenidenSevkGerekliAdet { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridAciklama { get; set; }
        public int? GridPersonelId { get; set; }

        public int UcKDurumuId { get; set; }
        public int UcKKarsilamaTipiId { get; set; }
        public decimal GelenMiktar { get; set; }
        public DateTime? TeslimTarihi { get; set; }
        public string? KaynakHedefProjeNo { get; set; }
        public string? UcKAciklama { get; set; }
        public decimal KarsilananMiktar { get; set; }
        public decimal StokKarsilanan { get; set; }
        public decimal ProjeKarsilanan { get; set; }
        public decimal ProjeGonderilen { get; set; }
        public decimal TedarikciKarsilanan { get; set; }
        public decimal HataliMiktar { get; set; }
        public decimal GeriGonderilenMiktar { get; set; }
        public int? GeriGonderilmeSebebiId { get; set; }
        public int? KaynakProjeId { get; set; }
        public int? KaliteDurumId { get; set; }
        public int? SurecDurumId { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }

        public int SandikIcerikSayisi { get; set; }
        public decimal TahsisMiktari { get; set; }
        public decimal KonulanAdet { get; set; }
        public decimal EksikAdet { get; set; }
        public decimal SandikStokKarsilanan { get; set; }
        public decimal SandikProjeKarsilanan { get; set; }
        public decimal SandikTedarikciKarsilanan { get; set; }

        public int StokHareketSayisi { get; set; }
        public decimal StoktanKarsilananMiktar { get; set; }
        public decimal FazlaTeslimStogaAktarilanMiktar { get; set; }
        public decimal DigerStokHareketMiktari { get; set; }
        public int GelenAktifProjeTransferSayisi { get; set; }
        public decimal GelenAktifProjeTransferMiktari { get; set; }
        public List<CekiRevizyonStokHareketEtkisi> StokHareketleri { get; set; } = new();
        public List<CekiRevizyonGelenTransferEtkisi> GelenAktifProjeTransferleri { get; set; } = new();
    }

    public sealed class CekiRevizyonStokHareketEtkisi
    {
        public int StokHareketiId { get; set; }
        public int StokKaydiId { get; set; }
        public int IslemTipiId { get; set; }
        public decimal Miktar { get; set; }
    }

    public sealed class CekiRevizyonGelenTransferEtkisi
    {
        public int ProjeTransferiId { get; set; }
        public int KaynakProjeId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public decimal Miktar { get; set; }
    }
}
