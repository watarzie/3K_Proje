using System.Globalization;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

/// <summary>Ambalaj kaydının (ör. özel sandık) bütün değişiklik geçmişi: kim, ne zaman, hangi işlemle neyi değiştirdi.</summary>
public sealed class GetAmbalajKayitGecmisiQuery : AmbalajPlanlamaQuery<IReadOnlyList<AmbalajKayitGecmisiDto>>
{
    public int KayitId { get; init; }
}

/// <summary>
/// Kaydın <see cref="AmbalajUretimHareketi"/> satırlarını işlem grubu başına tek olayda toplar (en yeni üstte); alanlar
/// Türkçe etiket ve okunur değerle döner. Ölçü, m³, sarf ve kaynak alanları kullanıcının görünüm izinleriyle maskelenir
/// (görülemeyen alan satırı listeye girmez); iç teknik alanlar gösterilmez. Kayıt, kullanıcının görebildiği üretim
/// durumunda değilse 404 döner.
/// </summary>
public sealed class GetAmbalajKayitGecmisiQueryHandler(
    IUnitOfWork unitOfWork,
    IRolService roller,
    ICurrentUserService kullanici,
    IReadQueryExecutor? readQueries = null)
    : IRequestHandler<GetAmbalajKayitGecmisiQuery, Result<IReadOnlyList<AmbalajKayitGecmisiDto>>>
{
    private readonly IReadQueryExecutor _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;

    public async Task<Result<IReadOnlyList<AmbalajKayitGecmisiDto>>> Handle(
        GetAmbalajKayitGecmisiQuery request,
        CancellationToken cancellationToken)
    {
        var kayitlar = AmbalajDurumErisimi.Filtrele(
            _readQueries.AsNoTracking(unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()).Where(k => k.Id == request.KayitId),
            await AmbalajYetkilendirmeYardimcisi.IzinliDurumlariGetirAsync(roller, kullanici, cancellationToken));
        if (await _readQueries.CountAsync(kayitlar, cancellationToken) == 0)
            return Result<IReadOnlyList<AmbalajKayitGecmisiDto>>.Failure("Ambalaj kaydı bulunamadı.", 404);

        var hareketler = await _readQueries.ToListAsync(
            _readQueries.AsNoTracking(unitOfWork.GetRepository<AmbalajUretimHareketi>().Queryable())
                .Where(h => h.AmbalajUretimKaydiId == request.KayitId), cancellationToken);
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roller, kullanici, cancellationToken);
        var kullaniciIds = hareketler.Select(h => h.KullaniciId).Distinct().ToList();
        var adlar = (await _readQueries.ToListAsync(
                _readQueries.AsNoTracking(unitOfWork.GetRepository<Kullanici>().Queryable()).Where(k => kullaniciIds.Contains(k.Id)),
                cancellationToken))
            .ToDictionary(k => k.Id, k => k.AdSoyad);
        var projeIds = hareketler
            .Where(h => h.AlanAdi == nameof(AmbalajUretimKaydi.ProjeId))
            .SelectMany(h => new[] { h.EskiDeger, h.YeniDeger })
            .Select(d => int.TryParse(d, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var projeler = (await _readQueries.ToListAsync(
                _readQueries.AsNoTracking(unitOfWork.GetRepository<Proje>().Queryable()).Where(p => projeIds.Contains(p.Id)),
                cancellationToken))
            .ToDictionary(p => p.Id, p => p.ProjeNo);

        var olaylar = hareketler
            .GroupBy(h => h.IslemGrubu)
            // Aynı anda yazılmış olaylarda sıra hareket kimliğinden gelir (sonraki işlem üstte).
            .OrderByDescending(g => g.Max(h => h.Tarih))
            .ThenByDescending(g => g.Max(h => h.Id))
            .Select(g =>
            {
                var ilk = g.OrderBy(h => h.Id).First();
                var degisiklikler = g.OrderBy(h => h.Id)
                    .Where(h => AmbalajKayitGecmisiBicimi.Gorunur(h.AlanAdi, izin))
                    .Where(h => h.EskiDeger != null || h.YeniDeger != null)
                    .Select(h => new AmbalajAlanDegisikligiDto(
                        AmbalajKayitGecmisiBicimi.Etiket(h.AlanAdi),
                        AmbalajKayitGecmisiBicimi.Deger(h.AlanAdi, h.EskiDeger, projeler),
                        AmbalajKayitGecmisiBicimi.Deger(h.AlanAdi, h.YeniDeger, projeler)))
                    .ToList();
                return new AmbalajKayitGecmisiDto(
                    g.Max(h => h.Tarih),
                    adlar.GetValueOrDefault(ilk.KullaniciId) ?? (ilk.KullaniciId == 0 ? "Sistem" : $"Kullanıcı #{ilk.KullaniciId}"),
                    ilk.Islem,
                    g.Select(h => h.Aciklama).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)),
                    degisiklikler);
            })
            .ToList();
        return Result<IReadOnlyList<AmbalajKayitGecmisiDto>>.Success(olaylar);
    }
}

/// <summary>Hareket alanlarının ekran etiketi, okunur değeri ve görünüm izni.</summary>
internal static class AmbalajKayitGecmisiBicimi
{
    private static readonly Dictionary<string, string> Etiketler = new(StringComparer.Ordinal)
    {
        [nameof(AmbalajUretimKaydi.ProjeId)] = "Proje",
        [nameof(AmbalajUretimKaydi.ManuelProjeNo)] = "Manuel proje no",
        [nameof(AmbalajUretimKaydi.ManuelProjeAdi)] = "Manuel proje adı",
        [nameof(AmbalajUretimKaydi.Tur)] = "Grup",
        [nameof(AmbalajUretimKaydi.KaynakKayitId)] = "Kaynak sandık",
        [nameof(AmbalajUretimKaydi.SandikNo)] = "Sandık no",
        [nameof(AmbalajUretimKaydi.Ad)] = "Sandık adı",
        [nameof(AmbalajUretimKaydi.SandikCinsi)] = "Yapı",
        [nameof(AmbalajUretimKaydi.DigerSandikCinsi)] = "Diğer yapı",
        [nameof(AmbalajUretimKaydi.Adet)] = "Adet",
        [nameof(AmbalajUretimKaydi.Boy)] = "Boy (mm)",
        [nameof(AmbalajUretimKaydi.En)] = "En (mm)",
        [nameof(AmbalajUretimKaydi.Yukseklik)] = "Yükseklik (mm)",
        [nameof(AmbalajUretimKaydi.AmbalajaDahil)] = "Ambalaja dahil",
        [nameof(AmbalajUretimKaydi.UretimeAlindi)] = "Üretime alındı / finansa aktarıldı",
        [nameof(AmbalajUretimKaydi.HesaplananBirimM3)] = "Birim m³",
        [nameof(AmbalajUretimKaydi.HesaplananToplamM3)] = "Hesaplanan m³",
        [nameof(AmbalajUretimKaydi.M3Override)] = "Elle m³",
        [nameof(AmbalajUretimKaydi.M3OverrideNedeni)] = "Elle m³ nedeni",
        [nameof(AmbalajUretimKaydi.SarfOrani)] = "Sarf oranı",
        [nameof(AmbalajUretimKaydi.SarfM3)] = "Sarf m³",
        [nameof(AmbalajUretimKaydi.ToplamM3)] = "Toplam m³",
        [nameof(AmbalajUretimKaydi.KullanimAmaci)] = "Kullanım amacı",
        [nameof(AmbalajUretimKaydi.TalepEdenKisi)] = "Talep eden",
        [nameof(AmbalajUretimKaydi.TalepEdenBolum)] = "Talep eden bölüm",
        [nameof(AmbalajUretimKaydi.TalimatVeren)] = "Talimat veren",
        [nameof(AmbalajUretimKaydi.FirinPartiNo)] = "Fırın parti no",
        [nameof(AmbalajUretimKaydi.Aciklama)] = "Açıklama",
        [nameof(AmbalajUretimKaydi.UretimDurumu)] = "Üretim durumu",
        [nameof(AmbalajUretimKaydi.UretimTarihi)] = "Üretim / aktarım tarihi",
        [nameof(AmbalajUretimKaydi.TamamlanmaTarihi)] = "Tamamlanma tarihi",
        [nameof(AmbalajUretimKaydi.FinansTarihi)] = "Finans tarihi",
        [nameof(AmbalajUretimKaydi.IptalMi)] = "İptal",
        [nameof(AmbalajUretimKaydi.IptalTarihi)] = "İptal tarihi",
        [nameof(AmbalajUretimKaydi.IptalNedeni)] = "İptal nedeni"
    };

    private static readonly HashSet<string> OlcuAlanlari = new(StringComparer.Ordinal)
        { nameof(AmbalajUretimKaydi.Boy), nameof(AmbalajUretimKaydi.En), nameof(AmbalajUretimKaydi.Yukseklik) };

    private static readonly HashSet<string> M3Alanlari = new(StringComparer.Ordinal)
    {
        nameof(AmbalajUretimKaydi.HesaplananBirimM3), nameof(AmbalajUretimKaydi.HesaplananToplamM3),
        nameof(AmbalajUretimKaydi.M3Override), nameof(AmbalajUretimKaydi.M3OverrideNedeni), nameof(AmbalajUretimKaydi.ToplamM3)
    };

    private static readonly HashSet<string> SarfAlanlari = new(StringComparer.Ordinal)
        { nameof(AmbalajUretimKaydi.SarfOrani), nameof(AmbalajUretimKaydi.SarfM3) };

    /// <summary>Alan ekranda gösterilir mi: etiketli (teknik olmayan) ve kullanıcının görünüm izni olan alanlar.</summary>
    public static bool Gorunur(string alan, AmbalajGorunumYetkileri izin) =>
        Etiketler.ContainsKey(alan) &&
        (izin.OlcuGorunur || !OlcuAlanlari.Contains(alan)) &&
        (izin.M3Gorunur || !M3Alanlari.Contains(alan)) &&
        (izin.SarfGorunur || !SarfAlanlari.Contains(alan)) &&
        (izin.KaynakGorunur || alan != nameof(AmbalajUretimKaydi.KaynakKayitId));

    public static string Etiket(string alan) => Etiketler.GetValueOrDefault(alan, alan);

    /// <summary>Saklanan değeri okunur hâle getirir (enum adı, Evet/Hayır, proje no, gg.aa.yyyy SS:dd).</summary>
    public static string? Deger(string alan, string? deger, IReadOnlyDictionary<int, string> projeler)
    {
        if (string.IsNullOrWhiteSpace(deger)) return null;
        var sayi = int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : (int?)null;
        return alan switch
        {
            nameof(AmbalajUretimKaydi.ProjeId) when sayi.HasValue => projeler.GetValueOrDefault(sayi.Value, deger),
            nameof(AmbalajUretimKaydi.Tur) when sayi.HasValue => AmbalajUretimYardimcilari.TurMetni((AmbalajSandikTuru)sayi.Value),
            nameof(AmbalajUretimKaydi.SandikCinsi) when sayi.HasValue =>
                AmbalajUretimYardimcilari.CinsMetni((AmbalajSandikCinsi)sayi.Value, null),
            nameof(AmbalajUretimKaydi.UretimDurumu) when sayi.HasValue => (AmbalajUretimDurumu)sayi.Value switch
            {
                AmbalajUretimDurumu.Planlandi => "Planlandı",
                AmbalajUretimDurumu.Uretimde => "Üretimde",
                AmbalajUretimDurumu.Tamamlandi => "Tamamlandı",
                _ => deger
            },
            _ when bool.TryParse(deger, out var mantiksal) => mantiksal ? "Evet" : "Hayır",
            _ when DateTime.TryParse(deger, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var tarih) &&
                   deger.Contains('T') => tarih.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            _ => deger
        };
    }
}
