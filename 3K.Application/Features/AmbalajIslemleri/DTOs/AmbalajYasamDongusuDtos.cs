using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.DTOs;

public sealed record AmbalajFormSurumuDto(int Id, Guid FormKimligi, int Surum,
    DateTime OlusturmaTarihi, int OlusturanKullaniciId, string? Aciklama,
    AmbalajUretimFormuModel Form);

public sealed class AmbalajGerceklesmeDto
{
    public int Id { get; set; }
    public Guid GerceklesmeKimligi { get; set; }
    public int Surum { get; set; }
    public int KayitId { get; set; }
    public DateTime Tarih { get; set; }
    public int? ProjeId { get; set; }
    public string ProjeNo { get; set; } = string.Empty;
    public string SandikNo { get; set; } = string.Empty;
    public AmbalajSandikCinsi SandikCinsi { get; set; }
    public AmbalajSandikTuru Tur { get; set; }
    public int Adet { get; set; }
    public bool M3HesaplanabilirMi { get; set; }
    public decimal? NetM3 { get; set; }
    public decimal? SarfM3 { get; set; }
    public decimal? SarfDahilM3 { get; set; }
}

public sealed record AmbalajGerceklesmeOzetDto(string Anahtar, int Adet,
    decimal? NetM3, decimal? SarfM3, decimal? SarfDahilM3);

public sealed class AmbalajGerceklesenRaporDto
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public string M3Tanimi { get; set; } = "İşlenen m³ net ahşap üretimidir; sarf ve sarf dahil toplam ayrı gösterilir.";
    public bool M3Gorunur { get; set; }
    public bool SarfGorunur { get; set; }
    public int TarihiBelirsizEskiKayitSayisi { get; set; }
    public IReadOnlyList<AmbalajGerceklesmeDto> Kayitlar { get; set; } = [];
    public AmbalajGerceklesmeOzetDto Toplam { get; set; } = new("Toplam", 0, null, null, null);
    public IReadOnlyList<AmbalajGerceklesmeOzetDto> Projeler { get; set; } = [];
    public IReadOnlyList<AmbalajGerceklesmeOzetDto> Gunler { get; set; } = [];
    public IReadOnlyList<AmbalajGerceklesmeOzetDto> Aylar { get; set; } = [];
    public IReadOnlyList<AmbalajGerceklesmeOzetDto> Cinsler { get; set; } = [];
}
