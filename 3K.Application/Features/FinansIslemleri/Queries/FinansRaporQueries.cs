using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansIsRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeRaporlar];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>Gider raporu: Giderler sekmesindeki indirme düğmesi ve Raporlar sekmesi.</summary>
public sealed class FinansGiderRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderler, YetkiKodlari.Finans.SekmeRaporlar];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansAylikRaporDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibiAylik, YetkiKodlari.Finans.SekmeRaporlar];
    public string Format { get; init; } = "pdf";
    public int Yil { get; init; }
    public int Ay { get; init; }
    public IReadOnlyCollection<string> Gruplar { get; init; } = Array.Empty<string>();
}

public sealed class FinansSiparisDurumRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibiAylik, YetkiKodlari.Finans.SekmeRaporlar];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansOzetRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeRaporlar];
    public string Tur { get; init; } = "genel";
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>Alacak (vade/tahsilat) raporu (D3).</summary>
public sealed class FinansAlacakRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeRaporlar];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>Rapor önizlemesi: rapor dosyasıyla aynı satırlar, aynı sekme izni.</summary>
public sealed class FinansRaporOnizlemeQuery : FinansQuery<FinansRaporOnizlemeModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeRaporlar];
    public string Tur { get; init; } = "is";
    public FinansListeFiltre Filtre { get; init; } = new();
}

