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

    /// <summary>
    /// <see cref="FinansTutarKurallari.IsNet"/> kuralının SQL'e çevrilen karşılığı. PostgreSQL <c>round</c>
    /// yarımları sıfırdan uzağa yuvarlar; kuralın para yuvarlamasıyla aynıdır.
    /// </summary>
    internal static readonly Expression<Func<FinansIsKaydi, decimal>> IsNetIfadesi = x =>
        x.ManuelNetTutar ?? decimal.Round(x.BirimFiyatSnapshot *
            (x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet ? x.Adet
                : x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Metrekup ? x.ToplamM3 : 1m), 2);

    /// <summary>Projeksiyona iş net tutarını ekler: seçici işi ve <see cref="IsNetIfadesi"/> değerini alır.</summary>
    internal static Expression<Func<FinansIsKaydi, T>> IsNetIle<T>(Expression<Func<FinansIsKaydi, decimal, T>> secici)
    {
        var isKaydi = secici.Parameters[0];
        var net = new ParametreDegistirici(IsNetIfadesi.Parameters[0], isKaydi).Visit(IsNetIfadesi.Body);
        var govde = new ParametreDegistirici(secici.Parameters[1], net).Visit(secici.Body);
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
        {
            var arama = LiteralSearchPattern(filtre.Arama);
            query = query.Where(x =>
                EF.Functions.ILike(x.ProjeNo, arama, "\\") ||
                EF.Functions.ILike(x.Musteri, arama, "\\") ||
                EF.Functions.ILike(x.IsAdi, arama, "\\") ||
                (x.SandikNo != null && EF.Functions.ILike(x.SandikNo, arama, "\\")) ||
                (x.Aciklama != null && EF.Functions.ILike(x.Aciklama, arama, "\\")) ||
                (x.TalepEdenKisi != null && EF.Functions.ILike(x.TalepEdenKisi, arama, "\\")) ||
                x.SiparisKalemleri.Any(y => EF.Functions.ILike(y.FinansSiparis.PoNumarasi, arama, "\\") ||
                    y.FaturaKalemleri.Any(z => EF.Functions.ILike(z.FinansFatura.FaturaNumarasi, arama, "\\"))));
        }
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

    // Harf eşlemesini tek yerde (PostgreSQL) yap; tr-TR uygulama kültürü ile DB
    // kültürünün farklı küçültmesi aranan I/İ karakterlerini kaybettirmesin.
    // Contains sözleşmesindeki %, _ ve \ karakterleri joker değil, gerçek metindir.
    private static string LiteralSearchPattern(string value)
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
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = LiteralSearchPattern(filtre.Arama);
            query = query.Where(x => EF.Functions.ILike(x.Aciklama, search, "\\") || (x.FirmaVeyaKisi != null && EF.Functions.ILike(x.FirmaVeyaKisi, search, "\\")) ||
                (x.BelgeNo != null && EF.Functions.ILike(x.BelgeNo, search, "\\")) || EF.Functions.ILike(x.Kategori.Ad, search, "\\"));
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

    private sealed class ParametreDegistirici(ParameterExpression eski, Expression yeni) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == eski ? yeni : base.VisitParameter(node);
    }
}
