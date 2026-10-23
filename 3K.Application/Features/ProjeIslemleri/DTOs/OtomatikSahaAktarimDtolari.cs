namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    /// <summary>Otomatik sahaya aktarım önizlemesi; veri değiştirmez, uygulama <see cref="PlanOzeti"/> ile gelir.</summary>
    public sealed class OtomatikSahaAktarimOnizlemeDto
    {
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string PlanOzeti { get; init; } = string.Empty;
        public bool AktarilacakVarMi { get; init; }
        public int AktarilacakSandikSayisi { get; init; }
        public int EksikSatirSayisi { get; init; }
        public decimal EksikToplamAdet { get; init; }
        public int AtlananSayisi { get; init; }

        /// <summary>Seçilen hedef; <c>null</c> ve <see cref="YeniSahaProjesi"/> false ise kullanıcı seçim yapmalıdır.</summary>
        public int? HedefSahaProjeId { get; init; }
        public string? HedefSahaProjeNo { get; init; }
        public bool YeniSahaProjesi { get; init; }
        public bool HedefSecimiGerekli { get; init; }
        public string YeniSahaProjeNoOnerisi { get; init; } = string.Empty;
        public List<OtomatikSahaHedefDto> HedefSecenekleri { get; init; } = [];

        public List<OtomatikSahaSandikDto> Sandiklar { get; init; } = [];
        public OtomatikSahaEksiklerSandigiDto? EksiklerSandigi { get; init; }
        public List<OtomatikSahaAtlananDto> Atlananlar { get; init; } = [];
    }

    /// <summary>Ana projeye bağlı, sevk edilmemiş saha projesi.</summary>
    public sealed class OtomatikSahaHedefDto
    {
        public int SahaProjeId { get; init; }
        public string SahaProjeNo { get; init; } = string.Empty;
        public string DurumMetni { get; init; } = string.Empty;
        public int SandikSayisi { get; init; }
    }

    /// <summary>Sevk edilmemiş ana sandığın saha projesindeki karşılığı.</summary>
    public sealed class OtomatikSahaSandikDto
    {
        public int KaynakSandikId { get; init; }
        public string KaynakSandikNo { get; init; } = string.Empty;
        public string? SandikAdi { get; init; }
        public string HedefSandikNo { get; init; } = string.Empty;
        /// <summary>Hedef sahada aynı numaralı boş sandık varsa o kullanılır.</summary>
        public bool MevcutSandigaEklenir { get; init; }
        public bool NumaraDegisti { get; init; }
        public int UrunSayisi { get; init; }
        public decimal ToplamAdet { get; init; }
        public List<OtomatikSahaUrunDto> Urunler { get; init; } = [];
    }

    /// <summary>Sevk edilmiş sandıkların eksiklerinin toplandığı saha "1" sandığı.</summary>
    public sealed class OtomatikSahaEksiklerSandigiDto
    {
        public string SandikNo { get; init; } = string.Empty;
        public string? SandikAdi { get; init; }
        public bool MevcutSandigaEklenir { get; init; }
        public decimal ToplamAdet { get; init; }
        public List<OtomatikSahaUrunDto> Urunler { get; init; } = [];
    }

    public sealed class OtomatikSahaUrunDto
    {
        public int CekiSatiriId { get; init; }
        public int SiraNo { get; init; }
        public string BarkodNo { get; init; } = string.Empty;
        public string Aciklama { get; init; } = string.Empty;
        public string? KaynakSandikNo { get; init; }
        public decimal IstenenAdet { get; init; }
        public decimal Miktar { get; init; }
        public string Birim { get; init; } = string.Empty;
    }

    public sealed class OtomatikSahaAtlananDto
    {
        public string? SandikNo { get; init; }
        public int? CekiSatiriId { get; init; }
        public string? Urun { get; init; }
        /// <summary>Kısa neden (tek cümle); ekranda doğrudan gösterilir.</summary>
        public string Neden { get; init; } = string.Empty;
        /// <summary>Ürün ürün ayrıntı (ör. elle sandık aktarımının ret gerekçeleri); ekranda açılır listededir.</summary>
        public List<string> Ayrintilar { get; init; } = [];
    }

    /// <summary>Uygulanan otomatik aktarım; geri alma <see cref="SahaAktarimId"/> ile yapılır.</summary>
    public sealed class OtomatikSahaAktarimSonucDto
    {
        public int SahaProjeId { get; init; }
        public string SahaProjeNo { get; init; } = string.Empty;
        public int SahaAktarimId { get; init; }
        public int AktarilanSandikSayisi { get; init; }
        public int EksikSatirSayisi { get; init; }
        public int ToplamSatirSayisi { get; init; }
        public decimal ToplamAdet { get; init; }
    }

    /// <summary>Bir saha aktarımının (elle ya da otomatik) tamamının geri alınma sonucu.</summary>
    public sealed class SahaAktarimiGeriAlSonucDto
    {
        public int SahaAktarimId { get; init; }
        public int SahaProjeId { get; init; }
        public int GeriAlinanSatirSayisi { get; init; }
        public decimal GeriAlinanToplamMiktar { get; init; }
        public int BosalanSandikSayisi { get; init; }
    }
}
