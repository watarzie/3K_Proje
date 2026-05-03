using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class CekiSatiri : BaseEntity
    {
        public int CekiId { get; set; }
        public int SiraNo { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        /// <summary>
        /// Madde 7: Birim artık serbest metin değil, Enum tabanlı dropdown.
        /// </summary>
        public int BirimId { get; set; } = (int)Birim.Adet;
        public string CekideGecenSandikNo { get; set; } = string.Empty;
        public string? FiiliSandikNo { get; set; }
        public string? Remarks { get; set; }

        // ===== Genel Durum (Otomatik hesaplanır) =====
        public int DurumId { get; set; } = (int)UrunDurum.Bekliyor;

        // ===== Grid Modülü Alanları =====
        /// <summary>
        /// Grid Durumu: Lookup Id
        /// </summary>
        public int GridDurumuId { get; set; } = (int)GridDurum.Bekliyor;
        /// <summary>
        /// Grid'e gelen adet. TAM GELDİ → IstenenAdet, EKSİK GELDİ → kullanıcı girer, diğer → 0
        /// </summary>
        public int GridGelenAdet { get; set; } = 0;
        /// <summary>
        /// Trafo sevk edilen adet. Sadece TRAFO SEVK durumunda aktif.
        /// </summary>
        public int TrafoSevkAdet { get; set; } = 0;
        /// <summary>
        /// Grid Sevk Durumu: SevkEdildi=1, Bekliyor=2, SevkEdilmedi=3
        /// Lookup tablosuna bağlı değil, enum ile yönetilir.
        /// </summary>
        public int GridSevkDurumuId { get; set; } = 3; // SevkEdilmedi
        /// <summary>
        /// Grid'den 3K'ya sevk edilen adet.
        /// </summary>
        public int? GridSevkMiktari { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridAciklama { get; set; }
        public int? GridPersonelId { get; set; }

        // ===== 3K Modülü Alanları =====
        public int UcKDurumuId { get; set; } = (int)UcKDurum.Bekliyor;
        /// <summary>
        /// 3K Karşılama Tipi: UcKDurum enum Id'si
        /// </summary>
        public int UcKKarsilamaTipiId { get; set; } = (int)UcKDurum.Bekliyor;
        /// <summary>
        /// 3K'ya gelen / karşılanan adet (tüm kaynaklardan toplam).
        /// </summary>
        public int GelenMiktar { get; set; } = 0;
        public DateTime? TeslimTarihi { get; set; }
        /// <summary>
        /// Projeden Karşılandı / Başka Projeye Verildi durumunda referans proje no.
        /// </summary>
        public string? KaynakHedefProjeNo { get; set; }
        /// <summary>
        /// 3K açıklama / not alanı (karşılama detayı).
        /// </summary>
        public string? UcKAciklama { get; set; }

        // ===== Kümülatif Takip Alanları =====
        /// <summary>
        /// FB, stok veya tedarikçiden karşılanan toplam adet (legacy, geriye dönük uyumluluk).
        /// </summary>
        public int KarsilananMiktar { get; set; } = 0;

        // ===== Madde 2: Parçalı Karşılama Detay Kolonları =====
        /// <summary>
        /// Stoktan karşılanan adet.
        /// </summary>
        public int StokKarsilanan { get; set; } = 0;

        /// <summary>
        /// Başka projeden (FB) karşılanan adet.
        /// </summary>
        public int ProjeKarsilanan { get; set; } = 0;

        /// <summary>
        /// Başka projeye gönderilen adet (bu üründen çıkan miktar).
        /// </summary>
        public int ProjeGonderilen { get; set; } = 0;

        /// <summary>
        /// Tedarikçiden karşılanan adet.
        /// </summary>
        public int TedarikciKarsilanan { get; set; } = 0;

        /// <summary>
        /// Hatalı gelen ürün adedi.
        /// </summary>
        public int HataliMiktar { get; set; } = 0;

        /// <summary>
        /// Geri gönderilen toplam miktar.
        /// </summary>
        public int GeriGonderilenMiktar { get; set; } = 0;

        /// <summary>
        /// GeriGonderildi durumunda zorunlu sebep: LookupGeriGonderilmeSebebi Id
        /// </summary>
        public int? GeriGonderilmeSebebiId { get; set; }

        /// <summary>
        /// FB'den (Projeden) karşılandığında kaynak proje ID'si.
        /// </summary>
        public int? KaynakProjeId { get; set; }

        // ===== Diğer =====
        public bool IsManuelEklenen { get; set; } = false;
        public string? EklemeNedeni { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }

        /// <summary>
        /// Grid tarafı eksik hesabı: Miktar - GridGelenAdet - TrafoSevkAdet
        /// İptal ise 0, Sipariste ise açık eksik kalır.
        /// </summary>
        public int GridEksikMiktar
        {
            get
            {
                if (GridDurumuId == (int)GridDurum.Iptal || GridDurumuId == (int)GridDurum.GridKapandi) return 0;
                return IstenenAdet - GridGelenAdet - TrafoSevkAdet;
            }
        }

        /// <summary>
        /// Madde 3 + 10: 3K tarafı kümülatif eksik hesabı.
        /// Grid'in sevk adedini değil, 3K'nın teslim aldığı miktarı baz alır.
        /// Eksik = IstenenAdet - (GelenMiktar + StokKarsilanan + ProjeKarsilanan + TedarikciKarsilanan)
        /// Toplam == IstenenAdet ise ürün eksik sayılmaz, rapordan düşer (Madde 3).
        /// </summary>
        public int EksikMiktar
        {
            get
            {
                if (GridDurumuId == (int)GridDurum.GridKapandi) return 0;
                var toplam = GelenMiktar + StokKarsilanan + ProjeKarsilanan + TedarikciKarsilanan;
                return Math.Max(IstenenAdet - toplam, 0);
            }
        }

        /// <summary>
        /// Kümülatif toplam tamamlanan adet (tüm kaynaklardan).
        /// GelenMiktar + Stok + Proje + Tedarikçi
        /// </summary>
        public int KumulatifToplam
        {
            get
            {
                return GelenMiktar + StokKarsilanan + ProjeKarsilanan + TedarikciKarsilanan;
            }
        }

        /// <summary>
        /// Kalan miktar — tüm karşılama kaynakları ve Trafo sevk düşülür. Hatalı ürün varsa kalan ASLA 0 olmaz.
        /// Madde 11: HataliUyumsuzGonderim durumunda da kalan 0 olmaz.
        /// </summary>
        public int KalanMiktar
        {
            get
            {
                // Grid Kapandı → kalan 0
                if (GridDurumuId == (int)GridDurum.GridKapandi) return 0;
                var kalan = IstenenAdet - GelenMiktar - StokKarsilanan - ProjeKarsilanan - TedarikciKarsilanan - TrafoSevkAdet;
                // İŞ KURALI: Hatalı ürün veya Hatalı/Uyumsuz Gönderim varsa kalan en az 1
                if ((HataliMiktar > 0 || DurumId == (int)UrunDurum.HataliUyumsuzGonderim) && kalan <= 0) return 1;
                return Math.Max(kalan, 0);
            }
        }

        // Navigation Properties
        public virtual Ceki Ceki { get; set; } = null!;
        public virtual Kullanici? Paketleyen { get; set; }
        public virtual Kullanici? KontrolEden { get; set; }
        public virtual Kullanici? GridPersonel { get; set; }
        public virtual Proje? KaynakProje { get; set; }
        public virtual LookupUrunDurum? DurumLookup { get; set; }
        public virtual LookupGridDurum? GridDurumLookup { get; set; }
        public virtual LookupUcKDurum? UcKDurumLookup { get; set; }
        public virtual LookupGeriGonderilmeSebebi? GeriGonderilmeSebebiLookup { get; set; }
        public virtual LookupBirim? BirimLookup { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
