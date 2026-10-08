using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Finans servislerinin ortak sorgu yapı taşları: iş kaydı detay yüklemesi, liste filtreleri,
/// sayfalama, para birimi toplamları, proje ve belge hedefi çözümü.
/// </summary>
internal static class FinansSorgulari
{
    /// <summary>Panel, rapor ve yaşlandırma sorgularının tek seferde tarayacağı en fazla kayıt.</summary>
    internal const int RaporKayitSiniri = 20000;

    /// <summary>Aktif para birimi kodları (F1), tanım sırasıyla; kartlar ve özetler bu listeyi her zaman gösterir.</summary>
    internal static Task<string[]> AktifParaBirimleriAsync(AppDbContext context, CancellationToken cancellationToken)
        => context.Set<FinansParaBirimi>().AsNoTracking().Where(x => x.Aktif)
            .OrderBy(x => x.Sira).ThenBy(x => x.Kod).Select(x => x.Kod).ToArrayAsync(cancellationToken);

    /// <summary>
    /// Özet ve kartlarda gösterilecek para birimleri: aktif tanımlar (tanım sırasıyla, verisi olmasa da) ve veride
    /// geçen diğer kodlar (pasifleştirilmiş para birimindeki geçmiş kayıtlar kaybolmasın diye, alfabetik).
    /// </summary>
    internal static async Task<string[]> OzetParaBirimleriAsync(
        AppDbContext context,
        IEnumerable<string?> veridekiler,
        CancellationToken cancellationToken)
    {
        var aktif = await AktifParaBirimleriAsync(context, cancellationToken);
        var diger = veridekiler.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim().ToUpperInvariant())
            .Where(x => !aktif.Contains(x, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return aktif.Concat(diger).ToArray();
    }

    /// <summary>Kullanıcının seçtiği para birimini normalize eder; tanımlı ve aktif değilse iş kuralı hatası verir.</summary>
    internal static async Task<string> AktifParaBirimiAsync(AppDbContext context, string? paraBirimi, CancellationToken cancellationToken)
    {
        var kod = FinansParaBirimiKurali.Normalize(paraBirimi ?? string.Empty);
        if (!await context.Set<FinansParaBirimi>().AnyAsync(x => x.Kod == kod && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma($"{kod} tanımlı ve aktif bir para birimi değil; Para Birimleri tanımından ekleyin.");
        return kod;
    }

    /// <summary>
    /// Kayıttaki para birimi seçimi (F1): yeni seçim tanımlı ve aktif olmalıdır. Kayıt zaten bu para birimindeyse
    /// (para birimi sonradan pasifleştirilmiş olsa da) seçim korunur; geçmiş kayıt düzenlenebilir kalır.
    /// </summary>
    internal static async Task<string> ParaBirimiSecimiAsync(
        AppDbContext context,
        string? paraBirimi,
        IEnumerable<string?> mevcutlar,
        CancellationToken cancellationToken)
    {
        var kod = FinansParaBirimiKurali.Normalize(paraBirimi ?? string.Empty);
        return mevcutlar.Any(x => string.Equals(x, kod, StringComparison.Ordinal))
            ? kod
            : await AktifParaBirimiAsync(context, kod, cancellationToken);
    }

    /// <summary>Tam sayı finans parametresi (D3); kayıt yoksa veya sayı değilse varsayılan döner.</summary>
    internal static async Task<int> ParametreAsync(AppDbContext context, string kod, int varsayilan, CancellationToken cancellationToken)
    {
        var deger = await context.Set<FinansParametre>().AsNoTracking().Where(x => x.Kod == kod)
            .Select(x => x.Deger).FirstOrDefaultAsync(cancellationToken);
        return int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sayi) ? sayi : varsayilan;
    }

    internal static Task<int> VarsayilanVadeGunuAsync(AppDbContext context, CancellationToken cancellationToken)
        => ParametreAsync(context, FinansParametreKodlari.VarsayilanVadeGunu, FinansVadeKurallari.VarsayilanVadeGunuDegeri, cancellationToken);

    internal static Task<int> VadeUyariGunuAsync(AppDbContext context, CancellationToken cancellationToken)
        => ParametreAsync(context, FinansParametreKodlari.VadeUyariGunu, FinansVadeKurallari.VarsayilanUyariGunu, cancellationToken);

    /// <summary>
    /// Fatura ödeme filtresi (D3); <see cref="FinansVadeKurallari.Durum"/> kuralının SQL karşılığıdır. "Yaklaşan",
    /// uyarı penceresinde vadesi gelen ödenmemiş faturadır; vadesi bugün olan fatura henüz geçmiş sayılmaz.
    /// </summary>
    internal static IQueryable<FinansFatura> OdemeFiltresi(
        IQueryable<FinansFatura> query,
        FinansOdemeFiltresi? filtre,
        DateTime bugun,
        int uyariGunu)
    {
        var gun = bugun.Date;
        var sinir = gun.AddDays(uyariGunu + 1);
        return filtre switch
        {
            FinansOdemeFiltresi.Odenmedi => query.Where(x => !x.OdendiMi),
            FinansOdemeFiltresi.VadesiGecen => query.Where(x => !x.OdendiMi && x.VadeTarihi < gun),
            FinansOdemeFiltresi.VadesiYaklasan => query.Where(x => !x.OdendiMi && x.VadeTarihi >= gun && x.VadeTarihi < sinir),
            FinansOdemeFiltresi.Odendi => query.Where(x => x.OdendiMi),
            _ => query
        };
    }

    /// <summary>
    /// <see cref="FinansTutarKurallari.IsNet"/> kuralının SQL'e çevrilen karşılığı. PostgreSQL <c>round</c>
    /// yarımları sıfırdan uzağa yuvarlar; kuralın para yuvarlamasıyla aynıdır.
    /// </summary>
    internal static readonly Expression<Func<FinansIsKaydi, decimal>> IsNetIfadesi = x =>
        x.ManuelNetTutar ?? decimal.Round(x.BirimFiyatSnapshot *
            (x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet ? x.Adet
                : x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Metrekup ? x.ToplamM3 : 1m), 2);

    /// <summary><see cref="FinansTutarKurallari.SiparisNet(FinansIsKaydi)"/> kuralının SQL karşılığı.</summary>
    internal static readonly Expression<Func<FinansIsKaydi, decimal>> SiparisNetIfadesi = x =>
        x.SiparisKalemleri.Where(y => !y.FinansSiparis.IptalEdildi).Sum(y => y.NetTutarSnapshot);

    /// <summary><see cref="FinansTutarKurallari.FaturaNet(FinansIsKaydi)"/> kuralının SQL karşılığı.</summary>
    internal static readonly Expression<Func<FinansIsKaydi, decimal>> FaturaNetIfadesi = x =>
        x.SiparisKalemleri.Where(y => !y.FinansSiparis.IptalEdildi)
            .SelectMany(y => y.FaturaKalemleri.Where(z => !z.FinansFatura.IptalEdildi))
            .Sum(z => z.NetTutarSnapshot);

    /// <summary>
    /// <see cref="FinansTutarKurallari.KalemKarsilanma"/> kuralının SQL karşılığı: karşılanan tutar (sipariş
    /// veya fatura) iş net bedeline göre yok/kısmi/tam.
    /// </summary>
    internal static Expression<Func<FinansIsKaydi, bool>> KarsilanmaIfadesi(
        Expression<Func<FinansIsKaydi, decimal>> karsilanan,
        FinansKarsilanma durum)
    {
        var isKaydi = Expression.Parameter(typeof(FinansIsKaydi), "x");
        var net = new ParametreDegistirici(IsNetIfadesi.Parameters[0], isKaydi).Visit(IsNetIfadesi.Body);
        var deger = new ParametreDegistirici(karsilanan.Parameters[0], isKaydi).Visit(karsilanan.Body);
        var sifir = Expression.Constant(0m);
        Expression govde = durum switch
        {
            FinansKarsilanma.Yok => Expression.LessThanOrEqual(deger, sifir),
            FinansKarsilanma.Tam => Expression.AndAlso(Expression.GreaterThan(net, sifir), Expression.GreaterThanOrEqual(deger, net)),
            _ => Expression.AndAlso(Expression.GreaterThan(deger, sifir),
                Expression.OrElse(Expression.LessThanOrEqual(net, sifir), Expression.LessThan(deger, net)))
        };
        return Expression.Lambda<Func<FinansIsKaydi, bool>>(govde, isKaydi);
    }

    /// <summary>Henüz PO'ya bağlanmamış net bakiyesi olan işler (fiyatlı ve kısmen/hiç siparişe bağlanmamış).</summary>
    internal static Expression<Func<FinansIsKaydi, bool>> SiparisBekleyenIfadesi()
    {
        var isKaydi = Expression.Parameter(typeof(FinansIsKaydi), "x");
        var net = new ParametreDegistirici(IsNetIfadesi.Parameters[0], isKaydi).Visit(IsNetIfadesi.Body);
        var siparis = new ParametreDegistirici(SiparisNetIfadesi.Parameters[0], isKaydi).Visit(SiparisNetIfadesi.Body);
        return Expression.Lambda<Func<FinansIsKaydi, bool>>(Expression.GreaterThan(net, siparis), isKaydi);
    }

    /// <summary>
    /// Çoklu kategori filtresi (<see cref="FinansKategoriKurali"/>): üretim kategorileri iş türüyle, özel ve
    /// düzenli işler kaynak düzenli işle ayrılır.
    /// </summary>
    internal static IQueryable<FinansIsKaydi> KategoriFiltresi(
        IQueryable<FinansIsKaydi> query,
        IReadOnlyCollection<FinansIsKategorisi>? kategoriler)
    {
        if (kategoriler is not { Count: > 0 }) return query;
        var uretimTurleri = kategoriler.Where(FinansKategoriKurali.UretimKategorisi)
            .SelectMany(FinansKategoriKurali.IsTurleri).Distinct().ToArray();
        var ozel = kategoriler.Contains(FinansIsKategorisi.OzelIs);
        var duzenli = kategoriler.Contains(FinansIsKategorisi.DuzenliIs);
        return query.Where(x => uretimTurleri.Contains(x.IsTuru) ||
                                (ozel && x.IsTuru == FinansIsTuru.OzelIs && x.DuzenliIsId == null) ||
                                (duzenli && x.IsTuru == FinansIsTuru.OzelIs && x.DuzenliIsId != null));
    }

    /// <summary>Projeksiyona iş net tutarını ekler: seçici işi ve <see cref="IsNetIfadesi"/> değerini alır.</summary>
    internal static Expression<Func<FinansIsKaydi, T>> IsNetIle<T>(Expression<Func<FinansIsKaydi, decimal, T>> secici)
    {
        var isKaydi = secici.Parameters[0];
        var net = new ParametreDegistirici(IsNetIfadesi.Parameters[0], isKaydi).Visit(IsNetIfadesi.Body);
        var govde = new ParametreDegistirici(secici.Parameters[1], net).Visit(secici.Body);
        return Expression.Lambda<Func<FinansIsKaydi, T>>(govde, isKaydi);
    }

    /// <summary>
    /// Projeksiyona işin net, sipariş ve fatura tutarlarını ekler (<see cref="IsNetIfadesi"/>,
    /// <see cref="SiparisNetIfadesi"/>, <see cref="FaturaNetIfadesi"/>); üçü de veritabanında hesaplanır.
    /// </summary>
    internal static Expression<Func<FinansIsKaydi, T>> IsTutarlariIle<T>(
        Expression<Func<FinansIsKaydi, decimal, decimal, decimal, T>> secici)
    {
        var isKaydi = secici.Parameters[0];
        Expression Ifade(Expression<Func<FinansIsKaydi, decimal>> kural) =>
            new ParametreDegistirici(kural.Parameters[0], isKaydi).Visit(kural.Body);
        var govde = new ParametreDegistirici(secici.Parameters[1], Ifade(IsNetIfadesi)).Visit(secici.Body);
        govde = new ParametreDegistirici(secici.Parameters[2], Ifade(SiparisNetIfadesi)).Visit(govde);
        govde = new ParametreDegistirici(secici.Parameters[3], Ifade(FaturaNetIfadesi)).Visit(govde);
        return Expression.Lambda<Func<FinansIsKaydi, T>>(govde, isKaydi);
    }

    internal static (int PageNumber, int PageSize, int Skip) NormalizePagination(
        int requestedPageNumber,
        int requestedPageSize,
        int totalCount,
        int maxPageSize)
    {
        var pageSize = Math.Clamp(requestedPageSize, 1, maxPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var pageNumber = Math.Clamp(requestedPageNumber, 1, totalPages);
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        return (pageNumber, pageSize, skip);
    }

    /// <summary>İşi sipariş ve fatura kalemleriyle birlikte yükler; durum ve kalan tutarlar bunlardan hesaplanır.</summary>
    internal static IQueryable<FinansIsKaydi> IsKaydiDetayQuery(this AppDbContext context, bool tracking = false)
    {
        var query = context.Set<FinansIsKaydi>()
            .Include(x => x.FinansUrun)
            .Include(x => x.SiparisKalemleri)
                .ThenInclude(x => x.FinansSiparis)
            .Include(x => x.SiparisKalemleri)
                .ThenInclude(x => x.FaturaKalemleri)
                    .ThenInclude(x => x.FinansFatura)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    internal static IQueryable<FinansIsKaydi> ApplyFilter(IQueryable<FinansIsKaydi> query, FinansListeFiltre filtre)
    {
        if (!filtre.IptalEdilenleriDahilEt)
            query = query.Where(x => !x.IptalEdildi && x.KaynakAktif);
        if (filtre.ProjeId.HasValue)
            query = query.Where(x => x.ProjeId == filtre.ProjeId);
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo))
            query = query.Where(x => x.ProjeNo == filtre.ProjeNo.Trim());
        if (filtre.IsTuru.HasValue)
            query = query.Where(x => x.IsTuru == filtre.IsTuru);
        query = KategoriFiltresi(query, filtre.Kategoriler);
        if (filtre.SiparisKarsilanma.HasValue)
            query = query.Where(KarsilanmaIfadesi(SiparisNetIfadesi, filtre.SiparisKarsilanma.Value));
        if (filtre.FaturaKarsilanma.HasValue)
            query = query.Where(KarsilanmaIfadesi(FaturaNetIfadesi, filtre.FaturaKarsilanma.Value));
        if (filtre.Durum.HasValue)
            query = query.Where(x => x.Durum == filtre.Durum);
        if (filtre.FaturaBekleyen)
            query = query.Where(x =>
                x.Durum == FinansIsDurumu.SiparisAcildi ||
                x.Durum == FinansIsDurumu.KismiFaturalandi);
        if (filtre.SiparisDurumu.HasValue)
            query = query.Where(x => x.SiparisKalemleri.Any(y =>
                !y.FinansSiparis.IptalEdildi && y.FinansSiparis.Durum == filtre.SiparisDurumu));
        if (filtre.FaturaDurumu.HasValue)
        {
            var faturaDurumu = filtre.FaturaDurumu.Value;
            query = faturaDurumu == FinansFaturaDurumu.IptalEdildi
                ? query.Where(x => x.SiparisKalemleri.Any(y => y.FaturaKalemleri.Any(z =>
                    z.FinansFatura.IptalEdildi &&
                    z.FinansFatura.Durum == FinansFaturaDurumu.IptalEdildi)))
                : query.Where(x => x.SiparisKalemleri.Any(y => y.FaturaKalemleri.Any(z =>
                    !z.FinansFatura.IptalEdildi && z.FinansFatura.Durum == faturaDurumu)));
        }
        if (filtre.Baslangic.HasValue)
            query = query.Where(x => x.FinansTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.SandikCinsi == filtre.SandikCinsi);
        if (!string.IsNullOrWhiteSpace(filtre.SandikNo))
        {
            var sandikNo = LiteralSearchPattern(filtre.SandikNo);
            query = query.Where(x => x.SandikNo != null && EF.Functions.ILike(x.SandikNo, sandikNo, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Firma))
        {
            var firma = LiteralSearchPattern(filtre.Firma);
            query = query.Where(x => EF.Functions.ILike(x.Musteri, firma, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi))
        {
            var fatura = LiteralSearchPattern(filtre.FaturaNumarasi);
            query = query.Where(x => x.SiparisKalemleri.Any(p => p.FaturaKalemleri.Any(f =>
                EF.Functions.ILike(f.FinansFatura.FaturaNumarasi, fatura, "\\"))));
        }
        if (filtre.Bitis.HasValue)
        {
            var bitisExclusive = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.FinansTarihi < bitisExclusive);
        }
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
            query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
            query = AramaFiltresi(query, filtre.Arama);
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = LiteralSearchPattern(filtre.PoNumarasi);
            query = query.Where(x => x.SiparisKalemleri.Any(y => !y.FinansSiparis.IptalEdildi && EF.Functions.ILike(y.FinansSiparis.PoNumarasi, po, "\\")));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var talepEden = LiteralSearchPattern(filtre.TalepEden);
            query = query.Where(x =>
                (x.TalepEdenKisi != null && EF.Functions.ILike(x.TalepEdenKisi, talepEden, "\\")) ||
                (x.TalepEdenBolum != null && EF.Functions.ILike(x.TalepEdenBolum, talepEden, "\\")));
        }

        return query;
    }

    /// <summary>
    /// Genel arama: proje no, müşteri, iş adı, sandık no, açıklama, talep eden, özel iş türü, PO ve fatura
    /// numarası; büyük/küçük harf duyarsız ve joker karaktersiz.
    /// </summary>
    internal static IQueryable<FinansIsKaydi> AramaFiltresi(IQueryable<FinansIsKaydi> query, string aramaMetni)
    {
        var arama = LiteralSearchPattern(aramaMetni);
        return query.Where(x =>
            EF.Functions.ILike(x.ProjeNo, arama, "\\") ||
            EF.Functions.ILike(x.Musteri, arama, "\\") ||
            EF.Functions.ILike(x.IsAdi, arama, "\\") ||
            (x.SandikNo != null && EF.Functions.ILike(x.SandikNo, arama, "\\")) ||
            (x.Aciklama != null && EF.Functions.ILike(x.Aciklama, arama, "\\")) ||
            (x.TalepEdenKisi != null && EF.Functions.ILike(x.TalepEdenKisi, arama, "\\")) ||
            (x.OzelIsTuru != null && EF.Functions.ILike(x.OzelIsTuru, arama, "\\")) ||
            x.SiparisKalemleri.Any(y => EF.Functions.ILike(y.FinansSiparis.PoNumarasi, arama, "\\") ||
                y.FaturaKalemleri.Any(z => EF.Functions.ILike(z.FinansFatura.FaturaNumarasi, arama, "\\"))));
    }

    // Harf eşlemesini tek yerde (PostgreSQL) yap; tr-TR uygulama kültürü ile DB
    // kültürünün farklı küçültmesi aranan I/İ karakterlerini kaybettirmesin.
    // Contains sözleşmesindeki %, _ ve \ karakterleri joker değil, gerçek metindir.
    internal static string LiteralSearchPattern(string value)
        => "%" + value.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal) + "%";

    /// <summary>
    /// Gider listesi, rapor verisi ve hareketler aynı filtre anlamını kullanır: arama açıklama,
    /// firma, belge no ve kategori adında büyük/küçük harf duyarsız ve joker karaktersiz eşleşir.
    /// </summary>
    internal static IQueryable<FinansGider> ExpenseFilter(IQueryable<FinansGider> query, FinansListeFiltre filtre)
    {
        if (filtre.GiderKategoriId.HasValue) query = query.Where(x => x.FinansGiderKategoriId == filtre.GiderKategoriId);
        if (!string.IsNullOrWhiteSpace(filtre.Firma))
        {
            var firma = LiteralSearchPattern(filtre.Firma);
            query = query.Where(x => x.FirmaVeyaKisi != null && EF.Functions.ILike(x.FirmaVeyaKisi, firma, "\\"));
        }
        if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue) { var end = filtre.Bitis.Value.Date.AddDays(1); query = query.Where(x => x.FinansTarihi < end); }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.ProjeId == filtre.ProjeId);
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo))
        {
            var projeNo = filtre.ProjeNo.Trim();
            query = query.Where(x => x.ManuelProjeNo == projeNo || (x.Proje != null && x.Proje.ProjeNo == projeNo));
        }
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var paraBirimi = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.ParaBirimi == paraBirimi);
        }
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.IsTuru == filtre.IsTuru);
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = LiteralSearchPattern(filtre.PoNumarasi);
            query = query.Where(x => x.FinansSiparis != null && EF.Functions.ILike(x.FinansSiparis.PoNumarasi, po, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = LiteralSearchPattern(filtre.Arama);
            query = query.Where(x => EF.Functions.ILike(x.Aciklama, search, "\\") || (x.FirmaVeyaKisi != null && EF.Functions.ILike(x.FirmaVeyaKisi, search, "\\")) ||
                (x.BelgeNo != null && EF.Functions.ILike(x.BelgeNo, search, "\\")) || EF.Functions.ILike(x.Kategori.Ad, search, "\\") ||
                (x.FinansSiparis != null && EF.Functions.ILike(x.FinansSiparis.PoNumarasi, search, "\\")));
        }
        return query;
    }

    /// <summary>Fatura kalemlerini para birimine göre SQL tarafında toplar.</summary>
    internal static IQueryable<FinansParaToplamiModel> BuildInvoiceTotalsQuery(
        IQueryable<FinansFaturaKalemi> invoiceLines)
        => invoiceLines
            .GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
            .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.NetTutarSnapshot), KdvTutari = group.Sum(x => x.KdvTutariSnapshot), ToplamTutar = group.Sum(x => x.ToplamTutarSnapshot) });

    /// <summary>Avans dışındaki giderleri para birimine göre SQL tarafında toplar.</summary>
    internal static IQueryable<FinansParaToplamiModel> BuildExpenseTotalsQuery(
        IQueryable<FinansGider> expenses)
        => expenses.Where(x => !x.AvansMi)
            .GroupBy(x => x.ParaBirimi)
            .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.Matrah), KdvTutari = group.Sum(x => x.KdvTutari), ToplamTutar = group.Sum(x => x.ToplamTutar) });

    /// <summary>Sistem projesi ya da manuel proje bilgisini doğrular ve kayda yazılacak proje alanlarını döndürür.</summary>
    internal static async Task<(int? ProjeId, string ProjeNo, string Musteri, bool Manuel)> ResolveProjectAsync(
        this AppDbContext context,
        int? projeId,
        string? manuelProjeNo,
        string? manuelProjeAdi,
        string? musteri,
        CancellationToken cancellationToken)
    {
        if (projeId.HasValue && !string.IsNullOrWhiteSpace(manuelProjeNo))
            throw IsKuraliException.Cakisma("Sistem projesi ile manuel proje aynı anda seçilemez.");
        if (projeId.HasValue)
        {
            var proje = await context.Set<Proje>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == projeId.Value, cancellationToken)
                ?? throw IsKuraliException.Cakisma("Proje bulunamadı.");
            return (proje.Id, proje.ProjeNo, proje.Musteri, false);
        }

        if (string.IsNullOrWhiteSpace(manuelProjeNo))
            return (null, "BAĞIMSIZ", string.IsNullOrWhiteSpace(musteri) ? manuelProjeAdi?.Trim() ?? string.Empty : musteri.Trim(), true);

        return (null, manuelProjeNo.Trim(), string.IsNullOrWhiteSpace(musteri) ? manuelProjeAdi?.Trim() ?? string.Empty : musteri.Trim(), true);
    }

    /// <summary>Belge ve kalıcı silme hedefi: iş kaydı, sipariş, fatura veya gider.</summary>
    internal static async Task<BaseEntity?> FinansHedefKaydiAsync(
        this AppDbContext context, string type, int id, CancellationToken cancellationToken) => type switch
    {
        "IsKaydi" => await context.Set<FinansIsKaydi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Siparis" => await context.Set<FinansSiparis>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Fatura" => await context.Set<FinansFatura>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Gider" => await context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        _ => throw IsKuraliException.Cakisma("Varlık türü IsKaydi, Siparis, Fatura veya Gider olmalıdır.")
    };

    /// <summary>
    /// Alacak satırları (D3): faturanın tahsil edilecek brüt tutarı ve para birimi. Belge toplamı girildiyse o,
    /// yoksa kalemlerin brüt toplamı kullanılır; bir PO tek para birimindedir (PO-2).
    /// </summary>
    internal static Task<List<FinansAlacakSatiri>> AlacakSatirlariAsync(IQueryable<FinansFatura> query, CancellationToken cancellationToken)
        => query.Select(x => new FinansAlacakSatiri
        {
            Id = x.Id,
            FaturaNumarasi = x.FaturaNumarasi,
            PoNumarasi = x.FinansSiparis.PoNumarasi,
            FaturaTarihi = x.FaturaTarihi,
            VadeTarihi = x.VadeTarihi,
            OdendiMi = x.OdendiMi,
            OdemeTarihi = x.OdemeTarihi,
            OdemeAciklamasi = x.OdemeAciklamasi,
            ParaBirimi = x.BelgeParaBirimiSnapshot ?? x.FinansSiparis.ParaBirimi
                ?? x.Kalemler.Select(k => k.FinansSiparisKalemi.ParaBirimiSnapshot).FirstOrDefault() ?? string.Empty,
            Toplam = x.BelgeToplamTutarSnapshot ?? x.Kalemler.Sum(k => k.ToplamTutarSnapshot),
            ProjeNolar = x.Kalemler.Select(k => k.FinansSiparisKalemi.FinansIsKaydi.ProjeNo).Distinct().ToList()
        }).ToListAsync(cancellationToken);

    private sealed class ParametreDegistirici(ParameterExpression eski, Expression yeni) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == eski ? yeni : base.VisitParameter(node);
    }
}

/// <summary>Alacak (vade/tahsilat) satırı; bkz. <see cref="FinansSorgulari.AlacakSatirlariAsync"/>.</summary>
internal sealed class FinansAlacakSatiri
{
    public int Id { get; init; }
    public string FaturaNumarasi { get; init; } = string.Empty;
    public string PoNumarasi { get; init; } = string.Empty;
    public DateTime FaturaTarihi { get; init; }
    public DateTime VadeTarihi { get; init; }
    public bool OdendiMi { get; init; }
    public DateTime? OdemeTarihi { get; init; }
    public string? OdemeAciklamasi { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal Toplam { get; init; }
    public List<string> ProjeNolar { get; init; } = [];
    public string ProjeNo => string.Join(", ", ProjeNolar.Order(StringComparer.Ordinal));
}
