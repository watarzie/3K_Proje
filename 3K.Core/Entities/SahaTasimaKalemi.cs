using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Sahadan sahaya taşımanın satır izi: bir saha satırı (sayaçlarıyla) kaynak saha projesinin sandığından hedef saha
    /// projesinin sandığına geçtiğinde yazılır. Satır kopyalanmaz; <see cref="KaynakCekiSatiriId"/> hep kök normal satırı
    /// gösterir. Proje/sandık silinse de iz okunabilsin diye numaralar anlık kopya olarak tutulur; A→B→C zincirinde ara
    /// saha B silinse de ürünün A→B adımı kalır (başlık bağı boşalır).
    /// </summary>
    public class SahaTasimaKalemi : BaseEntity
    {
        /// <summary>Taşımanın başlığı (hedef saha projesine ait aktarım kaydı); hedef saha silinirse boşalır.</summary>
        public int? SahaAktarimId { get; set; }
        /// <summary>Taşınan aktarım defteri kaydı; kayıt taşımayla birlikte hedef sahaya geçer.</summary>
        public int? SahaAktarimKalemiId { get; set; }
        /// <summary>Satırın taşımadan önce bağlı olduğu aktarım başlığı.</summary>
        public int? OncekiSahaAktarimId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public int? SahaCekiSatiriId { get; set; }
        public int? KaynakSahaProjeId { get; set; }
        public string KaynakSahaProjeNo { get; set; } = string.Empty;
        public int? KaynakSahaSandikId { get; set; }
        public string KaynakSahaSandikNo { get; set; } = string.Empty;
        public int? HedefSahaProjeId { get; set; }
        public string HedefSahaProjeNo { get; set; } = string.Empty;
        public int? HedefSahaSandikId { get; set; }
        public string HedefSahaSandikNo { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;

        public virtual SahaAktarim? SahaAktarim { get; set; }
        public virtual SahaAktarimKalemi? SahaAktarimKalemi { get; set; }
        public virtual SahaAktarim? OncekiSahaAktarim { get; set; }
        public virtual CekiSatiri KaynakCekiSatiri { get; set; } = null!;
        public virtual CekiSatiri? SahaCekiSatiri { get; set; }
        public virtual Proje? KaynakSahaProje { get; set; }
        public virtual Sandik? KaynakSahaSandik { get; set; }
        public virtual Proje? HedefSahaProje { get; set; }
        public virtual Sandik? HedefSahaSandik { get; set; }
    }
}
