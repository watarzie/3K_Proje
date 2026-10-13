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
    decimal? ToplamHacimM3,
    string? FirinPartiNo,
    int UretimeAlinanSandikAdedi,
    int IlaveSandikSayisi,
    int IcSandikSayisi,
    decimal? UretimHacimM3,
    int ProjeSandiklariDurumId,
    int IlaveSandiklarDurumId,
    int IcSandiklarDurumId,
    string? IlaveFirinPartiNo,
    string? IcSandikFirinPartiNo,
    int ProjeSandikSayisi,
    decimal? ProjeSandiklariHacimM3,
    decimal? IlaveSandiklarHacimM3,
    decimal? IcSandiklarHacimM3,
    int GenelUretimDurumu = 1,
    int GerekliSandikAdedi = 0,
    int TamamlananSandikAdedi = 0,
    IReadOnlyList<AmbalajGrupSandikAdediDto>? GerekliSandikKirilimi = null);

/// <summary>
/// Tamamlama için gerekli sandık adedinin grup kırılımı (Normal, İlave, İç, özel sandık); "X / Y gerekli sandık"
/// sayacındaki Y'nin proje sandık sayısından neden büyük olabileceğini açıklar.
/// </summary>
public sealed record AmbalajGrupSandikAdediDto(string Grup, int Adet, int TamamlananAdet);

public sealed record AmbalajPlanlamaProjeFiltreOzetiDto
{
    public int ProjeSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public decimal? ToplamHacimM3 { get; init; }
    public int EksikOlculuProjeSayisi { get; init; }
}

public sealed record AmbalajPlanlamaProjeleriSayfasiDto
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
    decimal? SeciliHacimM3,
    int GenelUretimDurumu = 1,
    int GerekliSandikAdedi = 0,
    int TamamlananSandikAdedi = 0,
    IReadOnlyList<AmbalajGrupSandikAdediDto>? GerekliSandikKirilimi = null);

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
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    string? KullanimAmaci,
    string? TalimatVeren,
    string? Aciklama,
    decimal? HacimM3,
    bool? AmbalajaDahilMi = true,
    bool AmbalajKarariOneriliyor = false,
    bool M3HesaplanabilirMi = true,
    int UretimDurumu = 1,
    bool OlcuHazir = false);

public sealed record AmbalajIcSandikSablonDto(
    int Id,
    string Ad,
    string SandikTipi,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik);

public sealed record AmbalajKullaniciSecenegiDto(int Id, string AdSoyad);

/// <summary>İlave sandığa dönüştürülebilecek proje sandığı; <see cref="SandikTipi"/> kaynak sandığın tipidir.</summary>
public sealed record AmbalajIlaveSandikAdayDto(
    int Id,
    string SandikNo,
    string? Ad,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    string? SandikTipi = null);

/// <summary>Özel sandık formunda seçilebilen proje sandığı ve tipi.</summary>
public sealed record AmbalajSandikSecenegiDto(
    int Id,
    string SandikNo,
    string? Ad,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    string? SandikTipi = null);

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
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    string? KullanimAmaci,
    string? TalimatVeren,
    string? Aciklama,
    decimal? HacimM3,
    bool M3HesaplanabilirMi = true,
    int UretimDurumu = 1,
    bool AmbalajaDahilMi = true,
    DateTime? OlusturmaTarihi = null,
    string? OlusturanAdi = null,
    DateTime? FinansaAktarimTarihi = null,
    string? FinansaAktaranAdi = null);

/// <summary>Ambalaj kaydının bir değişiklik olayı (aynı işlemde değişen alanlar birlikte).</summary>
public sealed record AmbalajKayitGecmisiDto(
    DateTime Tarih,
    string Kullanici,
    string Islem,
    string? Aciklama,
    IReadOnlyList<AmbalajAlanDegisikligiDto> Degisiklikler);

public sealed record AmbalajAlanDegisikligiDto(string Alan, string? EskiDeger, string? YeniDeger);

public sealed record AmbalajBagimsizSandikTurOzetiDto
{
    public int Tur { get; init; }
    public int KayitSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public decimal? ToplamHacimM3 { get; init; }
}

public sealed record AmbalajBagimsizSandikFiltreOzetiDto
{
    public int KayitSayisi { get; init; }
    public int ToplamSandikAdedi { get; init; }
    public int UretimeAlinanSandikAdedi { get; init; }
    public decimal? ToplamHacimM3 { get; init; }
    public IReadOnlyList<AmbalajBagimsizSandikTurOzetiDto> TurOzetleri { get; init; } = [];
    /// <summary>Arama ve tür filtresindeki finansa aktarılmamış / aktarılmış kayıt sayıları (finans durumu sekmeleri).</summary>
    public int FinansaAktarilmayanKayitSayisi { get; init; }
    public int FinansaAktarilanKayitSayisi { get; init; }
}

public sealed record AmbalajBagimsizSandiklarSayfasiDto
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
