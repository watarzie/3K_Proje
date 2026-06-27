using _3K.Core.Enums;
using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    public class SahaAktarimKalemi : BaseEntity
    {
        public int SahaAktarimId { get; set; }
        public int KaynakProjeId { get; set; }
        public int SahaProjeId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public int? SahaCekiSatiriId { get; set; }
        public int? KaynakSandikId { get; set; }
        public int? SahaSandikId { get; set; }
        public decimal Miktar { get; set; }
        public int AktarimTipiId { get; set; } = (int)SahaAktarimTipi.UrunBazli;
        public int DurumId { get; set; } = (int)SahaAktarimDurum.Planlandi;
        public bool SevkiyatKapsamindaMi { get; set; }
        public bool DuzeltmeyeAcikMi { get; set; }
        public DateTime? SevkTarihi { get; set; }
        public DateTime? GeriAlmaTarihi { get; set; }
        public string? Aciklama { get; set; }
        public string? GeriAlmaAciklama { get; set; }

        public virtual SahaAktarim SahaAktarim { get; set; } = null!;
        public virtual Proje KaynakProje { get; set; } = null!;
        public virtual Proje SahaProje { get; set; } = null!;
        public virtual CekiSatiri KaynakCekiSatiri { get; set; } = null!;
        public virtual CekiSatiri? SahaCekiSatiri { get; set; }
        public virtual Sandik? KaynakSandik { get; set; }
        public virtual Sandik? SahaSandik { get; set; }
    }
}
