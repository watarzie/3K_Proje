using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

/// <summary>
/// Grid Sahası hareket okumaları: canlı ve arşiv tabloları aynı süzgeçle okunur, iki projeksiyon UNION ALL ile
/// birleştirilir. Mal kabul tanımı (<see cref="GridHareketTuru.MalKabul"/>): Grid durumu Tam Geldi, Eksik Geldi, Trafo
/// Sevk ya da Grid Kapandı'ya geçen (<see cref="GridSahaBekleyenKurali.SahaDurumlari"/>; 24.10.2026) çeki satırı hareketi
/// (eski değer yeni değerden farklı; aynı durumdaki sevk güncellemesi sayılmaz) ve Grid manuel ürün eklemesi. Durum
/// değiştirmez.
/// </summary>
internal sealed class GridHareketOkuyucu(AppDbContext context) : IGridHareketOkuyucu
{
    private const string TamGeldi = "8";
    private const string EksikGeldi = "9";
    private const string TrafoSevk = "11";
    private const string GridKapandi = "14";

    private static readonly Expression<Func<HareketGecmisi, bool>> CanliMalKabul = h =>
        (h.IslemTipiId == (int)IslemTipi.GridDurumGuncellendi &&
         h.ReferansTipi == GridHareketSabitleri.CekiSatiriReferansi &&
         (h.YeniDeger == TamGeldi || h.YeniDeger == EksikGeldi || h.YeniDeger == TrafoSevk || h.YeniDeger == GridKapandi) &&
         (h.EskiDeger == null || h.EskiDeger != h.YeniDeger)) ||
        (h.IslemTipiId == (int)IslemTipi.ManuelUrunEklendi && h.Islem == GridHareketSabitleri.ManuelUrunIslemi);

    private static readonly Expression<Func<HareketGecmisiArsiv, bool>> ArsivMalKabul = h =>
        (h.IslemTipiId == (int)IslemTipi.GridDurumGuncellendi &&
         h.ReferansTipi == GridHareketSabitleri.CekiSatiriReferansi &&
         (h.YeniDeger == TamGeldi || h.YeniDeger == EksikGeldi || h.YeniDeger == TrafoSevk || h.YeniDeger == GridKapandi) &&
         (h.EskiDeger == null || h.EskiDeger != h.YeniDeger)) ||
        (h.IslemTipiId == (int)IslemTipi.ManuelUrunEklendi && h.Islem == GridHareketSabitleri.ManuelUrunIslemi);

    private static readonly Expression<Func<HareketGecmisi, bool>> CanliTopluSevk = h =>
        h.IslemTipiId == (int)IslemTipi.GridTopluSevkEdildi && h.ReferansTipi == GridHareketSabitleri.TopluSevkReferansi;

    private static readonly Expression<Func<HareketGecmisiArsiv, bool>> ArsivTopluSevk = h =>
        h.IslemTipiId == (int)IslemTipi.GridTopluSevkEdildi && h.ReferansTipi == GridHareketSabitleri.TopluSevkReferansi;

    public async Task<GridHareketSayfasi> GridHareketleriAsync(GridHareketFiltresi filtre, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtre);
        var baslangic = filtre.BaslangicDahil;
        var bitis = filtre.BitisHaric;

        var canliTaban = context.HareketGecmisleri.AsNoTracking().Where(h => h.Tarih >= baslangic && h.Tarih < bitis);
        var arsivTaban = context.HareketGecmisleriArsiv.AsNoTracking().Where(h => h.Tarih >= baslangic && h.Tarih < bitis);
        canliTaban = TureGore(canliTaban, filtre.Turler, CanliMalKabul, CanliTopluSevk);
        arsivTaban = TureGore(arsivTaban, filtre.Turler, ArsivMalKabul, ArsivTopluSevk);

        // İki projeksiyon aynı biçimde olmalıdır ki UNION ALL çevrilsin.
        var birlesik = canliTaban
            .Select(h => new GridHareketKaydi
            {
                Id = h.Id,
                ArsivdenMi = false,
                ProjeId = h.ProjeId,
                ProjeNo = h.Proje.ProjeNo,
                ReferansTipi = h.ReferansTipi,
                ReferansId = h.ReferansId,
                ReferansMetni = h.ReferansMetni,
                Islem = h.Islem,
                IslemTipiId = h.IslemTipiId,
                KullaniciId = h.KullaniciId,
                KullaniciAdi = h.Kullanici.AdSoyad,
                Tarih = h.Tarih,
                EskiDeger = h.EskiDeger,
                YeniDeger = h.YeniDeger,
                Aciklama = h.Aciklama
            })
            .Concat(arsivTaban.Select(h => new GridHareketKaydi
            {
                Id = h.Id,
                ArsivdenMi = true,
                ProjeId = h.ProjeId,
                ProjeNo = h.ProjeNo ?? string.Empty,
                ReferansTipi = h.ReferansTipi,
                ReferansId = h.ReferansId,
                ReferansMetni = h.ReferansMetni,
                Islem = h.Islem,
                IslemTipiId = h.IslemTipiId,
                KullaniciId = h.KullaniciId,
                KullaniciAdi = h.KullaniciAdi ?? string.Empty,
                Tarih = h.Tarih,
                EskiDeger = h.EskiDeger,
                YeniDeger = h.YeniDeger,
                Aciklama = h.Aciklama
            }));

        if (filtre.ProjeId is > 0)
        {
            var projeId = filtre.ProjeId.Value;
            birlesik = birlesik.Where(x => x.ProjeId == projeId);
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var terim = filtre.Arama.Trim().ToLower();
            birlesik = birlesik.Where(x =>
                x.ProjeNo.ToLower().Contains(terim) ||
                (x.ReferansMetni != null && x.ReferansMetni.ToLower().Contains(terim)) ||
                x.KullaniciAdi.ToLower().Contains(terim) ||
                (x.Aciklama != null && x.Aciklama.ToLower().Contains(terim)));
        }

        // Personel özeti personel süzgecinden önce alınır: ekrandaki personel seçimi ve özet, seçili kişi dışındakileri de göstermeye devam eder.
        // Kullanıcı kimliğine göre gruplanır: arşiv kaydı o günkü adı taşır, adı değişmiş kişi iki satır olmaz.
        var personeller = await birlesik
            .GroupBy(x => x.KullaniciId)
            .Select(g => new { KullaniciId = g.Key, KullaniciAdi = g.Max(x => x.KullaniciAdi), Sayi = g.Count() })
            .ToListAsync(cancellationToken);
        if (filtre.KullaniciId is > 0)
        {
            var kullaniciId = filtre.KullaniciId.Value;
            birlesik = birlesik.Where(x => x.KullaniciId == kullaniciId);
        }

        var sayfa = Math.Max(filtre.SayfaNo, 1);
        var boyut = Math.Max(filtre.SayfaBoyutu, 1);
        var toplam = await birlesik.CountAsync(cancellationToken);
        var kayitlar = await birlesik
            .OrderByDescending(x => x.Tarih)
            .ThenByDescending(x => x.ArsivdenMi)
            .ThenByDescending(x => x.Id)
            .Skip((sayfa - 1) * boyut)
            .Take(boyut)
            .ToListAsync(cancellationToken);

        return new GridHareketSayfasi
        {
            Kayitlar = kayitlar,
            ToplamKayit = toplam,
            PersonelOzeti = personeller
                .Select(x => new GridHareketPersonelOzeti(x.KullaniciId, x.KullaniciAdi ?? string.Empty, x.Sayi))
                .OrderByDescending(x => x.IslemSayisi)
                .ThenBy(x => x.KullaniciAdi, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
        };
    }

    public async Task<IReadOnlyDictionary<int, GridSonMalKabul>> SonMalKabulleriAsync(
        IReadOnlyCollection<int> cekiSatiriIds,
        CancellationToken cancellationToken = default)
    {
        var idMetinleri = cekiSatiriIds.Distinct().Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray();
        if (idMetinleri.Length == 0)
            return new Dictionary<int, GridSonMalKabul>();

        var canli = await context.HareketGecmisleri.AsNoTracking()
            .Where(CanliMalKabul)
            .Where(h => h.ReferansId != null && idMetinleri.Contains(h.ReferansId))
            .Select(h => new { h.ReferansId, h.Tarih, h.KullaniciId, KullaniciAdi = h.Kullanici.AdSoyad })
            .ToListAsync(cancellationToken);
        var arsiv = await context.HareketGecmisleriArsiv.AsNoTracking()
            .Where(ArsivMalKabul)
            .Where(h => h.ReferansId != null && idMetinleri.Contains(h.ReferansId))
            .Select(h => new { h.ReferansId, h.Tarih, h.KullaniciId, KullaniciAdi = h.KullaniciAdi ?? string.Empty })
            .ToListAsync(cancellationToken);

        return canli.Concat(arsiv)
            .GroupBy(x => int.Parse(x.ReferansId!, CultureInfo.InvariantCulture))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var son = g.OrderByDescending(x => x.Tarih).First();
                    return new GridSonMalKabul(g.Key, son.Tarih, son.KullaniciId, son.KullaniciAdi);
                });
    }

    public IQueryable<GridSonMalKabulTarihi> SonMalKabulTarihleri() =>
        // ReferansId'de indeks yoktur: satır başına ilişkili alt sorgu yerine mal kabulleri bir kez gruplayıp birleştiririz.
        context.HareketGecmisleri.AsNoTracking()
            .Where(CanliMalKabul)
            .Where(h => h.ReferansId != null)
            .Select(h => new { ReferansId = h.ReferansId!, h.Tarih })
            .Concat(context.HareketGecmisleriArsiv.AsNoTracking()
                .Where(ArsivMalKabul)
                .Where(h => h.ReferansId != null)
                .Select(h => new { ReferansId = h.ReferansId!, h.Tarih }))
            .GroupBy(x => x.ReferansId)
            .Select(g => new GridSonMalKabulTarihi { ReferansId = g.Key, Tarih = g.Max(x => x.Tarih) });

    public async Task<int> MalKabulSayisiAsync(DateTime baslangicDahil, DateTime bitisHaric, CancellationToken cancellationToken = default)
    {
        var canli = await context.HareketGecmisleri.AsNoTracking()
            .Where(h => h.Tarih >= baslangicDahil && h.Tarih < bitisHaric)
            .CountAsync(CanliMalKabul, cancellationToken);
        var arsiv = await context.HareketGecmisleriArsiv.AsNoTracking()
            .Where(h => h.Tarih >= baslangicDahil && h.Tarih < bitisHaric)
            .CountAsync(ArsivMalKabul, cancellationToken);
        return canli + arsiv;
    }

    private static IQueryable<T> TureGore<T>(
        IQueryable<T> sorgu,
        GridHareketTuru turler,
        Expression<Func<T, bool>> malKabul,
        Expression<Func<T, bool>> topluSevk)
    {
        var malKabulDahil = turler.HasFlag(GridHareketTuru.MalKabul);
        var topluSevkDahil = turler.HasFlag(GridHareketTuru.TopluSevk);
        if (malKabulDahil && topluSevkDahil)
        {
            var parametre = malKabul.Parameters[0];
            var ikinci = new ParametreDegistirici(topluSevk.Parameters[0], parametre).Visit(topluSevk.Body);
            return sorgu.Where(Expression.Lambda<Func<T, bool>>(Expression.OrElse(malKabul.Body, ikinci), parametre));
        }
        if (topluSevkDahil)
            return sorgu.Where(topluSevk);
        return sorgu.Where(malKabul);
    }

    private sealed class ParametreDegistirici(ParameterExpression eski, Expression yeni) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == eski ? yeni : base.VisitParameter(node);
    }
}
