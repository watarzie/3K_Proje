using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansIsKaydiGetirQuery : FinansQuery<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Kayit;
    public int Id { get; init; }
}

public sealed class FinansIsKayitlariSecimQuery : FinansQuery<IReadOnlyList<FinansIsKaydiModel>>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Kayit;
    public IReadOnlyCollection<int> Ids { get; init; } = Array.Empty<int>();
}

public sealed class FinansAylikIslerQuery : FinansQuery<FinansAylikSayfaliSonuc>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibiAylik];
    public int Yil { get; init; }
    public int Ay { get; init; }
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>
/// İş Takibi proje → sandık kırılımı: projenin (isteğe bağlı dönemdeki) bütün üretim kayıtları sandık başına; sayfalama
/// ve kayıt sınırı ekranın değil sorgunun kuralıdır.
/// </summary>
public sealed class FinansProjeSandikKirilimiQuery : FinansQuery<FinansProjeSandikKirilimiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibiAylik];
    public int? ProjeId { get; init; }
    public string? ProjeNo { get; init; }
    public string? Musteri { get; init; }
    public int? Yil { get; init; }
    public int? Ay { get; init; }
    public FinansListeFiltre Filtre { get; init; } = new();

    public FinansProjeSandikKirilimiIstegi Istek() => new(ProjeId, ProjeNo, Musteri, Yil, Ay, Filtre);
}

public sealed class FinansProjeSecenekleriQuery : FinansQuery<FinansSayfaliSonuc<FinansProjeSecenekModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.Modul];
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
