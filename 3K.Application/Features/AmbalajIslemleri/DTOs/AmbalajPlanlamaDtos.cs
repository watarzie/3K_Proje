namespace _3K.Application.Features.AmbalajIslemleri.DTOs;

public sealed record AmbalajPlanlamaProjeOzetDto(
    int ProjeId,
    string ProjeNo,
    string? FbNo,
    string Musteri,
    int ProjeTipiId,
    string ProjeTipiMetni,
    int ToplamSandikAdedi,
    int OlculuSandikSayisi,
    int EksikOlculuSandikSayisi,
    IReadOnlyList<string> EksikOlculuSandiklar,
    decimal ToplamHacimM3,
    string? FirinPartiNo,
    int UretimeAlinanSandikAdedi,
    int IlaveSandikSayisi,
    int IcSandikSayisi,
    decimal UretimHacimM3,
    int ProjeSandiklariDurumId,
    int IlaveSandiklarDurumId,
    int IcSandiklarDurumId,
    string? IlaveFirinPartiNo,
    string? IcSandikFirinPartiNo,
    int ProjeSandikSayisi,
    decimal ProjeSandiklariHacimM3,
    decimal IlaveSandiklarHacimM3,
    decimal IcSandiklarHacimM3);

public sealed class AmbalajPlanlamaProjeFiltreOzetiDto
{
    public int ProjeSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public decimal ToplamHacimM3 { get; init; }
    public int EksikOlculuProjeSayisi { get; init; }
}

public sealed class AmbalajPlanlamaProjeleriSayfasiDto
{
    public IReadOnlyList<AmbalajPlanlamaProjeOzetDto> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public AmbalajPlanlamaProjeFiltreOzetiDto? FilteredSummary { get; init; }
}

public sealed record AmbalajPlanlamaPlanDto(
    int ProjeId,
    string ProjeNo,
    string? FbNo,
    string Musteri,
    int ProjeTipiId,
    string ProjeTipiMetni,
    string? FirinPartiNo,
    string? IlaveFirinPartiNo,
    string? IcSandikFirinPartiNo,
    int ProjeSandiklariDurumId,
    int IlaveSandiklarDurumId,
    int IcSandiklarDurumId,
    IReadOnlyList<AmbalajPlanlamaKalemDto> Kalemler,
    int SeciliSandikAdedi,
    decimal SeciliHacimM3);

public sealed record AmbalajPlanlamaKalemDto(
    int Id,
    int? KaynakSandikId,
    int? UstKalemId,
    int? IcSandikSablonId,
    int Tur,
    string TurMetni,
    bool UretimeAlindi,
    string SandikNo,
    string? Ad,
    string SandikTipi,
    int Adet,
    decimal Boy,
    decimal En,
    decimal Yukseklik,
    string? KullanimAmaci,
    string? TalimatVeren,
    string? Aciklama,
    decimal HacimM3,
    bool? AmbalajaDahilMi = true,
    bool AmbalajKarariOneriliyor = false);

public sealed record AmbalajIcSandikSablonDto(
    int Id,
    string Ad,
    string SandikTipi,
    decimal Boy,
    decimal En,
    decimal Yukseklik);

public sealed record AmbalajTalepEdenDto(int Id, string Ad);

public sealed record AmbalajKullaniciSecenegiDto(int Id, string AdSoyad);

public sealed record AmbalajIlaveSandikAdayDto(
    int Id,
    string SandikNo,
    string? Ad,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik);

public sealed record AmbalajSandikSecenegiDto(
    int Id,
    string SandikNo,
    string? Ad,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik);

public sealed record AmbalajBagimsizSandikDto(
    int Id,
    int Tur,
    string TurMetni,
    int? ProjeId,
    string? ProjeNo,
    string? Musteri,
    int? KaynakSandikId,
    string? KaynakSandikNo,
    string? KaynakSandikAdi,
    int? UstKaynakSandikId,
    int? IcSandikSablonId,
    string? UstSandikNo,
    string? UstSandikAdi,
    bool UretimeAlindi,
    string SandikNo,
    string Ad,
    string SandikTipi,
    int Adet,
    decimal Boy,
    decimal En,
    decimal Yukseklik,
    string? KullanimAmaci,
    string? TalimatVeren,
    string? Aciklama,
    decimal HacimM3);

public sealed class AmbalajBagimsizSandikTurOzetiDto
{
    public int Tur { get; init; }
    public int KayitSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public decimal ToplamHacimM3 { get; init; }
}

public sealed class AmbalajBagimsizSandikFiltreOzetiDto
{
    public int KayitSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public int UretimeAlinanSandikAdedi { get; init; }
    public decimal ToplamHacimM3 { get; init; }
    public IReadOnlyList<AmbalajBagimsizSandikTurOzetiDto> TurOzetleri { get; init; } = [];
}

public sealed class AmbalajBagimsizSandiklarSayfasiDto
{
    public IReadOnlyList<AmbalajBagimsizSandikDto> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public AmbalajBagimsizSandikFiltreOzetiDto? FilteredSummary { get; init; }
}
