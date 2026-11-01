namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    /// <summary>Sahadan sahaya taşıma önizlemesi; engel varsa taşıma yapılamaz, veri değişmez.</summary>
    public sealed class SahadanSahayaTasimaOnizlemeDto
    {
        public string KaynakSahaProjeNo { get; init; } = string.Empty;
        public string HedefSahaProjeNo { get; init; } = string.Empty;
        public string HedefSandikNo { get; init; } = string.Empty;
        public bool YeniSandik { get; init; }
        public bool TasinabilirMi { get; init; }
        public int ManuelIcerikSayisi { get; init; }
        public decimal ToplamMiktar { get; init; }
        public List<string> BosalacakSandikNolari { get; init; } = [];
        public List<string> Engeller { get; init; } = [];
        public List<SahadanSahayaTasimaSatiriDto> Satirlar { get; init; } = [];
    }

    public sealed class SahadanSahayaTasimaSatiriDto
    {
        public int SahaCekiSatiriId { get; init; }
        public string KaynakProjeNo { get; init; } = string.Empty;
        public int SiraNo { get; init; }
        public string BarkodNo { get; init; } = string.Empty;
        public string Aciklama { get; init; } = string.Empty;
        public string KaynakSandikNo { get; init; } = string.Empty;
        public decimal Miktar { get; init; }
        public decimal KonulanAdet { get; init; }
        public string Birim { get; init; } = string.Empty;
    }

    /// <summary>Uygulanan sahadan sahaya taşıma; aynı işlem anahtarıyla tekrar gelen istek aynı sonucu alır.</summary>
    public sealed class SahadanSahayaTasimaSonucDto
    {
        public int SahaAktarimId { get; init; }
        public int HedefSahaProjeId { get; init; }
        public string HedefSahaProjeNo { get; init; } = string.Empty;
        public int HedefSandikId { get; init; }
        public string HedefSandikNo { get; init; } = string.Empty;
        public int TasinanSatirSayisi { get; init; }
        public int TasinanManuelIcerikSayisi { get; init; }
        public decimal ToplamMiktar { get; init; }
        public List<string> BosalanSandikNolari { get; init; } = [];
    }
}
