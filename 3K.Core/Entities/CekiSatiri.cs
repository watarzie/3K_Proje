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
        public string Birim { get; set; } = string.Empty;
        public string CekideGecenSandikNo { get; set; } = string.Empty;
        public string? FiiliSandikNo { get; set; }
        public string? Remarks { get; set; }

        // ===== Genel Durum (Otomatik hesaplanır) =====
        public string Durum { get; set; } = "Bekliyor";

        // ===== Grid Modülü Alanları =====
        /// <summary>
        /// Grid Durumu: TamGeldi, EksikGeldi, Gelmedi, TrafoSevk, Iptal, Sipariste, Bekliyor
        /// </summary>
        public string GridDurumu { get; set; } = "Bekliyor";
        /// <summary>
        /// Grid'e gelen adet. TAM GELDİ → IstenenAdet, EKSİK GELDİ → kullanıcı girer, diğer → 0
        /// </summary>
        public int GridGelenAdet { get; set; } = 0;
        /// <summary>
        /// Trafo sevk edilen adet. Sadece TRAFO SEVK durumunda aktif.
        /// </summary>
        public int TrafoSevkAdet { get; set; } = 0;
        /// <summary>
        /// Grid Sevk Durumu: SevkEdildi, Bekliyor, SevkEdilmedi
        /// </summary>
        public string GridSevkDurumu { get; set; } = "SevkEdilmedi";
        /// <summary>
        /// Grid'den 3K'ya sevk edilen adet.
        /// </summary>
        public int? GridSevkMiktari { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridNotu { get; set; }
        public int? GridPersonelId { get; set; }

        // ===== 3K Modülü Alanları =====
        public string UcKDurumu { get; set; } = "Bekliyor";
        /// <summary>
        /// 3K Karşılama Tipi: Bekliyor, TamGeldi, EksikGeldi, ProjedenKarsilandi, StoktanKarsilandi,
        /// TedarikcidenGeldi, BaskaProyeVerildi, HataliUrun
        /// </summary>
        public string UcKKarsilamaTipi { get; set; } = "Bekliyor";
        /// <summary>
        /// 3K'ya gelen / karşılanan adet (tüm kaynaklardan toplam).
        /// </summary>
        public int GelenMiktar { get; set; } = 0;
        public DateTime? TeslimTarihi { get; set; }
        public string? UcKNotu { get; set; }
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
        /// FB, stok veya tedarikçiden karşılanan toplam adet.
        /// GelenMiktar + KarsilananMiktar = toplam tamamlanan.
        /// </summary>
        public int KarsilananMiktar { get; set; } = 0;

        /// <summary>
        /// Hatalı gelen ürün adedi.
        /// İŞ KURALI: HataliMiktar varken KalanMiktar ASLA 0 yapılamaz.
        /// </summary>
        public int HataliMiktar { get; set; } = 0;

        /// <summary>
        /// GeriGonderildi durumunda zorunlu sebep: "Tadilat" veya "Iptal"
        /// </summary>
        public string? GeriGonderilmeSebebi { get; set; }

        /// <summary>
        /// FB'den (Projeden) karşılandığında kaynak proje ID'si.
        /// Grid personeli hangi projeden geldiğini görmek için kullanır.
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
                if (GridDurumu == "Iptal") return 0;
                return IstenenAdet - GridGelenAdet - TrafoSevkAdet;
            }
        }

        /// <summary>
        /// 3K tarafı kümülatif eksik hesabı:
        /// IstenenAdet - GelenMiktar - KarsilananMiktar
        /// NOT: HataliMiktar düşülmez — hatalı varken kalan ASLA 0 olamaz.
        /// </summary>
        public int EksikMiktar => IstenenAdet - GelenMiktar - KarsilananMiktar;

        /// <summary>
        /// Kümülatif toplam tamamlanan adet (tüm kaynaklardan).
        /// </summary>
        public int KumulatifToplam => GelenMiktar + KarsilananMiktar;

        /// <summary>
        /// Kalan miktar — Hatalı ürün varsa kalan ASLA 0 olmaz.
        /// </summary>
        public int KalanMiktar
        {
            get
            {
                var kalan = IstenenAdet - GelenMiktar - KarsilananMiktar;
                // İŞ KURALI: Hatalı ürün varsa kalan en az 1 (eksik giderilmemiş)
                if (HataliMiktar > 0 && kalan <= 0) return 1;
                return Math.Max(kalan, 0);
            }
        }

        // Navigation Properties
        public virtual Ceki Ceki { get; set; } = null!;
        public virtual Kullanici? Paketleyen { get; set; }
        public virtual Kullanici? KontrolEden { get; set; }
        public virtual Kullanici? GridPersonel { get; set; }
        public virtual Proje? KaynakProje { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
