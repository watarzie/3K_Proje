using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Sandık açılış/kapanış olayları (<see cref="SandikDurumOlayi"/>): sandığın zaman çizelgesi ve süzgece göre geniş
/// istatistik. Kırılımlar veritabanında gruplanır ve sıralanır; ekran tabloları sayfa sayfa okur (<see cref="SayfaliSonuc{T}"/>),
/// Excel aynı sorguların bütün satırlarını ve süzgeçteki olayları yazar.
/// </summary>
internal sealed class SandikDurumIstatistikService(AppDbContext context) : ISandikDurumIstatistikService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    /// <summary>Excel'e yazılan en fazla olay sayısı; aşılırsa bilgi satırında belirtilir.</summary>
    private const int ExcelOlaySiniri = 50_000;

    public async Task<IReadOnlyList<SandikDurumOlayModeli>> SandikGecmisiAsync(int sandikId, CancellationToken cancellationToken) =>
        await ModellerAsync(context.SandikDurumOlaylari.AsNoTracking().Where(o => o.SandikId == sandikId), 0, null, cancellationToken);

    public async Task<SandikDurumIstatistigi> IstatistikAsync(SandikDurumIstatistikKriteri kriter, CancellationToken cancellationToken) =>
        await IstatistikAsync(kriter, await SuzAsync(kriter, cancellationToken), cancellationToken);

    public async Task<SayfaliSonuc<SandikDurumProjeSatiri>> ProjelerAsync(
        SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken) =>
        await ProjelerAsync(await SuzAsync(kriter, cancellationToken), new SayfaIstegi(sayfa, boyut), cancellationToken);

    public async Task<SayfaliSonuc<SandikDurumMusteriSatiri>> MusterilerAsync(
        SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken) =>
        await MusterilerAsync(await SuzAsync(kriter, cancellationToken), new SayfaIstegi(sayfa, boyut), cancellationToken);

    public async Task<SayfaliSonuc<SandikDurumKisiSatiri>> KisilerAsync(
        SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken) =>
        await KisilerAsync(await SuzAsync(kriter, cancellationToken), new SayfaIstegi(sayfa, boyut), cancellationToken);

    public async Task<SayfaliSonuc<SandikDurumOlayModeli>> OlaylarAsync(
        SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        var sorgu = await SuzAsync(kriter, cancellationToken);
        var toplam = await sorgu.CountAsync(cancellationToken);
        var (gecerliSayfa, sayfaBoyutu) = new SayfaIstegi(sayfa, boyut).Coz(toplam);
        return new SayfaliSonuc<SandikDurumOlayModeli>
        {
            Items = await ModellerAsync(sorgu, SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu), sayfaBoyutu, cancellationToken),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    private async Task<SandikDurumIstatistigi> IstatistikAsync(
        SandikDurumIstatistikKriteri kriter, IQueryable<SandikDurumOlayi> sorgu, CancellationToken cancellationToken)
    {
        var acilmalar = sorgu.Where(o => o.Tur == SandikDurumOlayTuru.Acildi);

        var turKaynak = await sorgu
            .GroupBy(o => new { o.Tur, o.Kaynak })
            .Select(g => new { g.Key.Tur, g.Key.Kaynak, Sayi = g.Count() })
            .ToListAsync(cancellationToken);
        int Say(SandikDurumOlayTuru tur, SandikDurumOlayKaynagi? kaynak = null) =>
            turKaynak.Where(x => x.Tur == tur && (kaynak == null || x.Kaynak == kaynak)).Sum(x => x.Sayi);

        var ozet = new SandikDurumIstatistikOzeti
        {
            KapanmaSayisi = Say(SandikDurumOlayTuru.Kapandi),
            TopluKapanmaSayisi = Say(SandikDurumOlayTuru.Kapandi, SandikDurumOlayKaynagi.Toplu),
            OtomatikKapanmaSayisi = Say(SandikDurumOlayTuru.Kapandi, SandikDurumOlayKaynagi.Otomatik),
            AcilmaSayisi = Say(SandikDurumOlayTuru.Acildi),
            ElleAcilmaSayisi = Say(SandikDurumOlayTuru.Acildi, SandikDurumOlayKaynagi.Elle),
            OtomatikAcilmaSayisi = Say(SandikDurumOlayTuru.Acildi, SandikDurumOlayKaynagi.Otomatik),
            GecmistenAktarilanAcilmaSayisi = Say(SandikDurumOlayTuru.Acildi, SandikDurumOlayKaynagi.GecmistenAktarildi),
            AcilanSandikSayisi = await acilmalar.Select(o => new { o.ProjeId, o.SandikNo }).Distinct().CountAsync(cancellationToken),
            ProjeSayisi = await sorgu.Select(o => o.ProjeId).Distinct().CountAsync(cancellationToken),
            MusteriSayisi = await sorgu.Select(o => o.Proje.Musteri.Trim()).Distinct().CountAsync(cancellationToken),
            KisiSayisi = await sorgu.Select(o => o.KullaniciId).Distinct().CountAsync(cancellationToken),
            OlaySayisi = turKaynak.Sum(x => x.Sayi)
        };

        return new SandikDurumIstatistigi
        {
            Kriter = kriter,
            OlusturmaTarihi = TurkeyTime.Now,
            Ozet = ozet,
            Sebepler = await SebeplerAsync(acilmalar, cancellationToken),
            EnCokAcilanlar = await EnCokAcilanlarAsync(sorgu, acilmalar, cancellationToken)
        };
    }

    public async Task<byte[]> ExcelOlusturAsync(SandikDurumIstatistikKriteri kriter, CancellationToken cancellationToken)
    {
        var sorgu = await SuzAsync(kriter, cancellationToken);
        var istatistik = await IstatistikAsync(kriter, sorgu, cancellationToken);
        var hepsi = SayfaIstegi.Hepsi;
        var projeler = (await ProjelerAsync(sorgu, hepsi, cancellationToken)).Items;
        var musteriler = (await MusterilerAsync(sorgu, hepsi, cancellationToken)).Items;
        var kisiler = (await KisilerAsync(sorgu, hepsi, cancellationToken)).Items;
        var olaylar = await ModellerAsync(sorgu, 0, ExcelOlaySiniri + 1, cancellationToken);
        return ExcelOlustur(istatistik, projeler, musteriler, kisiler, olaylar);
    }

    private async Task<IQueryable<SandikDurumOlayi>> SuzAsync(SandikDurumIstatistikKriteri kriter, CancellationToken cancellationToken)
    {
        var sorgu = context.SandikDurumOlaylari.AsNoTracking();
        if (kriter.Baslangic is { } baslangic)
        {
            var ilk = baslangic.Date;
            sorgu = sorgu.Where(o => o.Tarih >= ilk);
        }
        if (kriter.Bitis is { } bitis)
        {
            var sonrakiGun = bitis.Date.AddDays(1);
            sorgu = sorgu.Where(o => o.Tarih < sonrakiGun);
        }
        var proje = SandikDurumOlayKurali.AramaNormalize(kriter.ProjeArama);
        var musteri = SandikDurumOlayKurali.AramaNormalize(kriter.Musteri);
        if (proje is null && musteri is null)
            return sorgu;

        // Türkçe harf duyarsız eşleme bellekte (yalnız kimlik, no ve müşteri okunur); olaylar eşleşen proje kimlikleriyle süzülür.
        var projeler = await context.Projeler.AsNoTracking()
            .Select(p => new { p.Id, p.ProjeNo, p.Musteri })
            .ToListAsync(cancellationToken);
        var projeIdleri = projeler
            .Where(p => (proje is null || SandikDurumOlayKurali.AramaEslesir(p.ProjeNo, proje)) &&
                        (musteri is null || SandikDurumOlayKurali.AramaEslesir(p.Musteri, musteri)))
            .Select(p => p.Id)
            .ToList();
        return sorgu.Where(o => projeIdleri.Contains(o.ProjeId));
    }

    private async Task<List<SandikDurumSebepSatiri>> SebeplerAsync(
        IQueryable<SandikDurumOlayi> acilmalar, CancellationToken cancellationToken)
    {
        var gruplar = await acilmalar
            .GroupBy(o => new { o.SebepId, o.Kaynak })
            .Select(g => new { g.Key.SebepId, g.Key.Kaynak, Sayi = g.Count() })
            .ToListAsync(cancellationToken);
        var sebepAdlari = await context.LookupSandikAcilmaSebepleri.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Deger, cancellationToken);
        // Sebepsiz açılma kaynağına göre ayrı satırdır: otomatik açılmanın ve eski kayıtların sebebi yoktur.
        return gruplar
            .GroupBy(g => g.SebepId is { } id
                ? (SebepId: (int?)id, Ad: sebepAdlari.GetValueOrDefault(id, "?"))
                : (SebepId: null, Ad: $"Sebepsiz – {SandikDurumOlayKurali.KaynakEtiketi(g.Kaynak)}"))
            .Select(g => new SandikDurumSebepSatiri { SebepId = g.Key.SebepId, Sebep = g.Key.Ad, Sayi = g.Sum(x => x.Sayi) })
            .OrderByDescending(s => s.Sayi)
            .ThenBy(s => s.Sebep, StringComparer.Create(Tr, false))
            .ToList();
    }

    private async Task<SayfaliSonuc<SandikDurumKisiSatiri>> KisilerAsync(
        IQueryable<SandikDurumOlayi> sorgu, SayfaIstegi istek, CancellationToken cancellationToken)
    {
        var gruplar = sorgu
            .GroupBy(o => o.KullaniciId)
            .Select(g => new
            {
                KullaniciId = g.Key,
                Kapatma = g.Count(o => o.Tur == SandikDurumOlayTuru.Kapandi),
                GeriAcma = g.Count(o => o.Tur == SandikDurumOlayTuru.Acildi)
            });
        var toplam = await gruplar.CountAsync(cancellationToken);
        var (sayfa, boyut) = istek.Coz(toplam);
        var satirlar = await gruplar
            .OrderByDescending(x => x.GeriAcma)
            .ThenByDescending(x => x.Kapatma)
            .ThenBy(x => x.KullaniciId)
            .Skip(SayfaKurali.Atlanan(sayfa, boyut))
            .Take(boyut)
            .ToListAsync(cancellationToken);
        var adlar = await KullaniciAdlariAsync(satirlar.Select(x => x.KullaniciId), cancellationToken);
        return new SayfaliSonuc<SandikDurumKisiSatiri>
        {
            Items = satirlar.Select(x => new SandikDurumKisiSatiri
            {
                KullaniciId = x.KullaniciId,
                KullaniciAdi = x.KullaniciId is { } id ? adlar.GetValueOrDefault(id, "?") : "Bilinmiyor / sistem",
                KapatmaSayisi = x.Kapatma,
                GeriAcmaSayisi = x.GeriAcma
            }).ToList(),
            PageNumber = sayfa,
            PageSize = boyut,
            TotalCount = toplam
        };
    }

    private static async Task<SayfaliSonuc<SandikDurumProjeSatiri>> ProjelerAsync(
        IQueryable<SandikDurumOlayi> sorgu, SayfaIstegi istek, CancellationToken cancellationToken)
    {
        var gruplar = sorgu
            .GroupBy(o => new { o.ProjeId, o.Proje.ProjeNo, o.Proje.Musteri })
            .Select(g => new
            {
                g.Key.ProjeId,
                g.Key.ProjeNo,
                g.Key.Musteri,
                Kapanma = g.Count(o => o.Tur == SandikDurumOlayTuru.Kapandi),
                Acilma = g.Count(o => o.Tur == SandikDurumOlayTuru.Acildi),
                Elle = g.Count(o => o.Tur == SandikDurumOlayTuru.Acildi && o.Kaynak == SandikDurumOlayKaynagi.Elle)
            });
        var toplam = await gruplar.CountAsync(cancellationToken);
        var (sayfa, boyut) = istek.Coz(toplam);
        var satirlar = await gruplar
            .OrderByDescending(x => x.Acilma)
            .ThenByDescending(x => x.Kapanma)
            .ThenBy(x => x.ProjeNo)
            .Skip(SayfaKurali.Atlanan(sayfa, boyut))
            .Take(boyut)
            .ToListAsync(cancellationToken);
        // Geri açılan farklı sandık sayısı yalnız bu sayfanın projeleri için okunur.
        var projeIdleri = satirlar.Select(x => x.ProjeId).ToList();
        var acilanSandiklar = (await sorgu
                .Where(o => o.Tur == SandikDurumOlayTuru.Acildi && projeIdleri.Contains(o.ProjeId))
                .Select(o => new { o.ProjeId, o.SandikNo })
                .Distinct()
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.ProjeId)
            .ToDictionary(g => g.Key, g => g.Count());
        return new SayfaliSonuc<SandikDurumProjeSatiri>
        {
            Items = satirlar.Select(x => new SandikDurumProjeSatiri
            {
                ProjeId = x.ProjeId,
                ProjeNo = x.ProjeNo,
                Musteri = x.Musteri.Trim(),
                KapanmaSayisi = x.Kapanma,
                AcilmaSayisi = x.Acilma,
                ElleAcilmaSayisi = x.Elle,
                AcilanSandikSayisi = acilanSandiklar.GetValueOrDefault(x.ProjeId)
            }).ToList(),
            PageNumber = sayfa,
            PageSize = boyut,
            TotalCount = toplam
        };
    }

    private static async Task<SayfaliSonuc<SandikDurumMusteriSatiri>> MusterilerAsync(
        IQueryable<SandikDurumOlayi> sorgu, SayfaIstegi istek, CancellationToken cancellationToken)
    {
        var gruplar = sorgu
            .GroupBy(o => o.Proje.Musteri.Trim())
            .Select(g => new
            {
                Musteri = g.Key,
                ProjeSayisi = g.Select(o => o.ProjeId).Distinct().Count(),
                Kapanma = g.Count(o => o.Tur == SandikDurumOlayTuru.Kapandi),
                Acilma = g.Count(o => o.Tur == SandikDurumOlayTuru.Acildi),
                Elle = g.Count(o => o.Tur == SandikDurumOlayTuru.Acildi && o.Kaynak == SandikDurumOlayKaynagi.Elle)
            });
        var toplam = await gruplar.CountAsync(cancellationToken);
        var (sayfa, boyut) = istek.Coz(toplam);
        var satirlar = await gruplar
            .OrderByDescending(x => x.Acilma)
            .ThenByDescending(x => x.Kapanma)
            .ThenBy(x => x.Musteri)
            .Skip(SayfaKurali.Atlanan(sayfa, boyut))
            .Take(boyut)
            .ToListAsync(cancellationToken);
        return new SayfaliSonuc<SandikDurumMusteriSatiri>
        {
            Items = satirlar.Select(x => new SandikDurumMusteriSatiri
            {
                Musteri = string.IsNullOrEmpty(x.Musteri) ? "Müşterisiz" : x.Musteri,
                ProjeSayisi = x.ProjeSayisi,
                KapanmaSayisi = x.Kapanma,
                AcilmaSayisi = x.Acilma,
                ElleAcilmaSayisi = x.Elle
            }).ToList(),
            PageNumber = sayfa,
            PageSize = boyut,
            TotalCount = toplam
        };
    }

    private async Task<List<SandikDurumSandikSatiri>> EnCokAcilanlarAsync(
        IQueryable<SandikDurumOlayi> sorgu, IQueryable<SandikDurumOlayi> acilmalar, CancellationToken cancellationToken)
    {
        var enCok = await acilmalar
            .GroupBy(o => new { o.ProjeId, o.SandikNo })
            .Select(g => new
            {
                g.Key.ProjeId,
                g.Key.SandikNo,
                SandikId = g.Max(o => o.SandikId),
                Acilma = g.Count(),
                Elle = g.Count(o => o.Kaynak == SandikDurumOlayKaynagi.Elle),
                SonAcilma = g.Max(o => o.Tarih)
            })
            .OrderByDescending(x => x.Acilma)
            .ThenByDescending(x => x.SonAcilma)
            .Take(SandikDurumIstatistigi.EnCokAcilanSiniri)
            .ToListAsync(cancellationToken);
        if (enCok.Count == 0)
            return [];

        var projeIdleri = enCok.Select(x => x.ProjeId).Distinct().ToList();
        var kapanmalar = (await sorgu
                .Where(o => o.Tur == SandikDurumOlayTuru.Kapandi && projeIdleri.Contains(o.ProjeId))
                .GroupBy(o => new { o.ProjeId, o.SandikNo })
                .Select(g => new { g.Key.ProjeId, g.Key.SandikNo, Sayi = g.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => (x.ProjeId, x.SandikNo), x => x.Sayi);
        var projeMap = await context.Projeler.AsNoTracking()
            .Where(p => projeIdleri.Contains(p.Id))
            .Select(p => new { p.Id, p.ProjeNo, p.Musteri })
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        return enCok.Select(x => new SandikDurumSandikSatiri
        {
            SandikId = x.SandikId,
            ProjeId = x.ProjeId,
            ProjeNo = projeMap.GetValueOrDefault(x.ProjeId)?.ProjeNo ?? "?",
            Musteri = projeMap.GetValueOrDefault(x.ProjeId)?.Musteri?.Trim() ?? string.Empty,
            SandikNo = x.SandikNo,
            AcilmaSayisi = x.Acilma,
            ElleAcilmaSayisi = x.Elle,
            KapanmaSayisi = kapanmalar.GetValueOrDefault((x.ProjeId, x.SandikNo)),
            SonAcilma = x.SonAcilma
        }).ToList();
    }

    /// <summary>Ekrandan gelen sayfa isteği ya da Excel için bütün satırlar (<see cref="Hepsi"/>).</summary>
    private readonly record struct SayfaIstegi(int? Sayfa, int? Boyut, bool TumSatirlar = false)
    {
        public static SayfaIstegi Hepsi => new(null, null, true);

        public (int Sayfa, int Boyut) Coz(int toplam)
        {
            if (TumSatirlar)
                return (1, Math.Max(toplam, 1));
            var boyut = SayfaKurali.Boyut(Boyut);
            return (SayfaKurali.Sayfa(Sayfa, boyut, toplam), boyut);
        }
    }

    /// <summary>Olayları en yeniden eskiye, proje/kullanıcı/sebep adlarıyla okur (yalnız istenen aralık).</summary>
    private async Task<List<SandikDurumOlayModeli>> ModellerAsync(
        IQueryable<SandikDurumOlayi> sorgu, int atlanan, int? alinan, CancellationToken cancellationToken)
    {
        IQueryable<SandikDurumOlayi> sirali = sorgu.OrderByDescending(o => o.Tarih).ThenByDescending(o => o.Id);
        if (atlanan > 0)
            sirali = sirali.Skip(atlanan);
        if (alinan is { } n)
            sirali = sirali.Take(n);
        var satirlar = await sirali
            .Select(o => new
            {
                o.Id,
                o.ProjeId,
                o.Proje.ProjeNo,
                o.Proje.Musteri,
                o.SandikId,
                o.SandikNo,
                o.Tur,
                o.Kaynak,
                o.EskiDurumId,
                o.YeniDurumId,
                o.SebepId,
                Sebep = o.Sebep != null ? o.Sebep.Deger : null,
                o.Not,
                o.KullaniciId,
                o.Tarih
            })
            .ToListAsync(cancellationToken);
        if (satirlar.Count == 0)
            return [];

        var kullanicilar = await KullaniciAdlariAsync(satirlar.Select(s => s.KullaniciId), cancellationToken);
        var durumlar = await context.LookupSandikDurumlari.AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.Deger, cancellationToken);
        return satirlar.Select(s => new SandikDurumOlayModeli
        {
            Id = s.Id,
            ProjeId = s.ProjeId,
            ProjeNo = s.ProjeNo,
            Musteri = s.Musteri,
            SandikId = s.SandikId,
            SandikNo = s.SandikNo,
            Tur = s.Tur,
            TurMetni = SandikDurumOlayKurali.TurEtiketi(s.Tur),
            Kaynak = s.Kaynak,
            KaynakMetni = SandikDurumOlayKurali.KaynakEtiketi(s.Kaynak),
            EskiDurumMetni = s.EskiDurumId is { } eski ? durumlar.GetValueOrDefault(eski) : null,
            YeniDurumMetni = durumlar.GetValueOrDefault(s.YeniDurumId, "?"),
            SebepId = s.SebepId,
            SebepMetni = s.Sebep,
            Not = s.Not,
            KullaniciId = s.KullaniciId,
            KullaniciAdi = s.KullaniciId is { } id ? kullanicilar.GetValueOrDefault(id) : null,
            Tarih = s.Tarih
        }).ToList();
    }

    private async Task<Dictionary<int, string>> KullaniciAdlariAsync(IEnumerable<int?> idler, CancellationToken cancellationToken)
    {
        var liste = idler.Where(id => id is > 0).Select(id => id!.Value).Distinct().ToList();
        return liste.Count == 0
            ? new Dictionary<int, string>()
            : await context.Kullanicilar.AsNoTracking()
                .Where(k => liste.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.AdSoyad, cancellationToken);
    }

    private static byte[] ExcelOlustur(
        SandikDurumIstatistigi istatistik,
        IReadOnlyList<SandikDurumProjeSatiri> projeler,
        IReadOnlyList<SandikDurumMusteriSatiri> musteriler,
        IReadOnlyList<SandikDurumKisiSatiri> kisiler,
        IReadOnlyList<SandikDurumOlayModeli> olaylar)
    {
        using var wb = new XLWorkbook();
        var o = istatistik.Ozet;
        Tablo(wb, "Özet", istatistik, ["Gösterge", "Sayı"],
        [
            ["Kapanma", o.KapanmaSayisi],
            ["– toplu işlemle", o.TopluKapanmaSayisi],
            ["– otomatik", o.OtomatikKapanmaSayisi],
            ["Geri açılma", o.AcilmaSayisi],
            ["– elle (sebepli)", o.ElleAcilmaSayisi],
            ["– otomatik", o.OtomatikAcilmaSayisi],
            ["– geçmişten aktarılan", o.GecmistenAktarilanAcilmaSayisi],
            ["Geri açılan farklı sandık", o.AcilanSandikSayisi],
            ["Proje", o.ProjeSayisi],
            ["Müşteri", o.MusteriSayisi],
            ["Kişi", o.KisiSayisi],
            ["Olay", o.OlaySayisi]
        ]);
        Tablo(wb, "Sebepler", istatistik, ["Sebep", "Geri açılma"],
            istatistik.Sebepler.Select(s => new object?[] { s.Sebep, s.Sayi }));
        Tablo(wb, "Kişiler", istatistik, ["Kullanıcı", "Kapatma", "Geri açma"],
            kisiler.Select(k => new object?[] { k.KullaniciAdi, k.KapatmaSayisi, k.GeriAcmaSayisi }));
        Tablo(wb, "Projeler", istatistik, ["Proje No", "Müşteri", "Kapanma", "Geri açılma", "Elle geri açılma", "Geri açılan sandık"],
            projeler.Select(p => new object?[]
                { p.ProjeNo, p.Musteri, p.KapanmaSayisi, p.AcilmaSayisi, p.ElleAcilmaSayisi, p.AcilanSandikSayisi }));
        Tablo(wb, "Müşteriler", istatistik, ["Müşteri", "Proje", "Kapanma", "Geri açılma", "Elle geri açılma"],
            musteriler.Select(m => new object?[]
                { m.Musteri, m.ProjeSayisi, m.KapanmaSayisi, m.AcilmaSayisi, m.ElleAcilmaSayisi }));
        Tablo(wb, "En Çok Açılanlar", istatistik,
            ["Proje No", "Müşteri", "Sandık No", "Geri açılma", "Elle geri açılma", "Kapanma", "Son geri açılma"],
            istatistik.EnCokAcilanlar.Select(s => new object?[]
                { s.ProjeNo, s.Musteri, s.SandikNo, s.AcilmaSayisi, s.ElleAcilmaSayisi, s.KapanmaSayisi, s.SonAcilma }));
        var tasti = olaylar.Count > ExcelOlaySiniri;
        Tablo(wb, "Olaylar", istatistik,
            ["Tarih", "Proje No", "Müşteri", "Sandık No", "Olay", "Kaynak", "Önceki durum", "Yeni durum", "Sebep", "Not", "Kullanıcı"],
            olaylar.Take(ExcelOlaySiniri).Select(x => new object?[]
            {
                x.Tarih, x.ProjeNo, x.Musteri, x.SandikNo, x.TurMetni, x.KaynakMetni, x.EskiDurumMetni, x.YeniDurumMetni,
                x.SebepMetni, x.Not, x.KullaniciAdi
            }),
            tasti ? $"İlk {ExcelOlaySiniri:N0} olay yazıldı; daha dar bir tarih aralığı seçin." : null);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static void Tablo(
        XLWorkbook wb, string ad, SandikDurumIstatistigi istatistik, string[] basliklar,
        IEnumerable<object?[]> satirlar, string? ekBilgi = null)
    {
        const int baslikSatiri = 5;
        var ws = wb.Worksheets.Add(ad);
        var sonSutun = basliklar.Length;
        ws.Cell(1, 1).Value = $"Sandık Açılış/Kapanış İstatistiği – {ad}";
        RaporExcelStilleri.StyleExcelTitle(ws, sonSutun);
        ws.Cell(2, 1).Value = SuzgecMetni(istatistik.Kriter);
        ws.Cell(3, 1).Value = $"Rapor tarihi: {istatistik.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm", Tr)}" +
                              (ekBilgi is null ? string.Empty : $" · {ekBilgi}");
        RaporExcelStilleri.StyleExcelInfoRows(ws, sonSutun);
        for (var i = 0; i < basliklar.Length; i++)
            ws.Cell(baslikSatiri, i + 1).Value = basliklar[i];
        RaporExcelStilleri.StyleExcelHeader(ws, baslikSatiri, sonSutun);

        var satir = baslikSatiri + 1;
        foreach (var degerler in satirlar)
        {
            for (var i = 0; i < degerler.Length; i++)
            {
                var hucre = ws.Cell(satir, i + 1);
                switch (degerler[i])
                {
                    case null: break;
                    case int n: hucre.SetValue(n); break;
                    case DateTime t:
                        hucre.SetValue(t);
                        hucre.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                        break;
                    default: hucre.SetValue(Convert.ToString(degerler[i], Tr)); break;
                }
            }
            satir++;
        }
        var sonSatir = satir - 1;
        if (sonSatir > baslikSatiri)
            ws.Range(baslikSatiri, 1, sonSatir, sonSutun).SetAutoFilter();
        RaporExcelStilleri.FinalizeExcelWorksheet(ws, baslikSatiri, sonSatir, sonSutun);
    }

    private static string SuzgecMetni(SandikDurumIstatistikKriteri kriter)
    {
        var parcalar = new List<string>
        {
            kriter.Baslangic is null && kriter.Bitis is null
                ? "Tarih: tümü"
                : $"Tarih: {kriter.Baslangic?.ToString("dd.MM.yyyy", Tr) ?? "…"} – {kriter.Bitis?.ToString("dd.MM.yyyy", Tr) ?? "…"}"
        };
        if (!string.IsNullOrWhiteSpace(kriter.ProjeArama))
            parcalar.Add($"Proje: {kriter.ProjeArama.Trim()}");
        if (!string.IsNullOrWhiteSpace(kriter.Musteri))
            parcalar.Add($"Müşteri: {kriter.Musteri.Trim()}");
        return "Süzgeç: " + string.Join(" · ", parcalar);
    }
}
