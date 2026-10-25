using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Etiketler;

/// <summary>
/// Barkod etiketi (<see cref="BarkodEtiketKurali"/>): projenin sandık/ürün listeleri veritabanında süzülüp sıralanır ve
/// yalnız istenen sayfa okunur; etiketler <see cref="GodexEtiketSablonu"/> düzeninde PDF veya EZPL olarak üretilir.
/// Ürünler çeki satırları ile saha/yedek sandıklarına elle eklenen malzemelerdir; barkod satır kimliği olmadığı için
/// seçim (kaynak, kimlik) ile yapılır. Hiçbir kaydı değiştirmez.
/// </summary>
internal sealed class BarkodEtiketService(AppDbContext context) : IBarkodEtiketService
{
    public async Task<SayfaliSonuc<BarkodEtiketProjesi>> ProjeleriListeleAsync(
        string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        var aranan = Aranan(arama);
        var sorgu = context.Projeler.AsNoTracking()
            .Where(p => aranan == null || p.ProjeNo.ToUpper().Contains(aranan) || p.Musteri.ToUpper().Contains(aranan)
                || (p.FBNo != null && p.FBNo.ToUpper().Contains(aranan)));
        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var projeler = await sorgu
            .OrderByDescending(p => p.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(p => new { p.Id, p.ProjeNo, p.ProjeTipiId, p.FBNo, p.Musteri, p.Lokasyon })
            .ToListAsync(cancellationToken);
        return new SayfaliSonuc<BarkodEtiketProjesi>
        {
            Items = projeler.Select(p => new BarkodEtiketProjesi(p.Id, p.ProjeNo, p.ProjeTipiId,
                BarkodEtiketKurali.SeriNo(p.FBNo, p.ProjeNo), p.Musteri, p.Lokasyon ?? "")).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<BarkodEtiketProjesi> ProjeAsync(int projeId, CancellationToken cancellationToken)
    {
        var proje = await context.Projeler.AsNoTracking()
            .Where(p => p.Id == projeId)
            .Select(p => new { p.Id, p.ProjeNo, p.ProjeTipiId, p.FBNo, p.Musteri, p.Lokasyon })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KayitBulunamadiException("Proje bulunamadı.");
        return new BarkodEtiketProjesi(proje.Id, proje.ProjeNo, proje.ProjeTipiId,
            BarkodEtiketKurali.SeriNo(proje.FBNo, proje.ProjeNo), proje.Musteri, proje.Lokasyon ?? "");
    }

    public async Task<SayfaliSonuc<BarkodEtiketSandikSatiri>> SandiklariListeleAsync(
        int projeId, string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        await ProjeVarMiAsync(projeId, cancellationToken);
        var aranan = Aranan(arama);
        var sorgu = context.Sandiklar.AsNoTracking()
            .Where(s => s.ProjeId == projeId)
            .Where(s => aranan == null || s.SandikNo.ToUpper().Contains(aranan) || (s.Ad != null && s.Ad.ToUpper().Contains(aranan)));

        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        // Sayısal sandık numaralarında doğal sıra (2 < 10): önce uzunluk, sonra metin.
        var satirlar = await sorgu
            .OrderBy(s => s.SandikNo.Length).ThenBy(s => s.SandikNo).ThenBy(s => s.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(s => new
            {
                s.Id, s.SandikNo, s.Ad, s.AdIngilizce,
                Durum = s.DurumLookup != null ? s.DurumLookup.Deger : "",
                UrunSayisi = s.SandikIcerikleri.Count
            })
            .ToListAsync(cancellationToken);

        return new SayfaliSonuc<BarkodEtiketSandikSatiri>
        {
            Items = satirlar.Select(s => new BarkodEtiketSandikSatiri(s.Id, s.SandikNo, s.Ad, s.AdIngilizce, s.Durum,
                BarkodEtiketKurali.SandikBarkodu(s.Id), s.UrunSayisi)).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<SayfaliSonuc<BarkodEtiketUrunSatiri>> UrunleriListeleAsync(
        int projeId, int? sandikId, string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        await ProjeVarMiAsync(projeId, cancellationToken);
        if (sandikId is not null && !await context.Sandiklar.AnyAsync(s => s.Id == sandikId && s.ProjeId == projeId, cancellationToken))
            throw new KayitBulunamadiException("Sandık bu projede bulunamadı.");
        var aranan = Aranan(arama);

        // Sayfa anahtarları tek sorguda (UNION ALL): önce çeki satırları çeki ve sıra no ile, sonra elle eklenenler.
        var cekiAnahtarlari = context.CekiSatirlari.AsNoTracking()
            .Where(cs => cs.Ceki.ProjeId == projeId)
            .Where(cs => sandikId == null || cs.SandikIcerikleri.Any(si => si.SandikId == sandikId))
            .Where(cs => aranan == null || cs.BarkodNo.ToUpper().Contains(aranan) || cs.Aciklama.ToUpper().Contains(aranan))
            .Select(cs => new UrunAnahtari { Kaynak = (int)BarkodEtiketUrunKaynagi.CekiSatiri, Id = cs.Id, Grup = cs.CekiId, Sira = cs.SiraNo });
        var elleAnahtarlari = context.SandikIcerikleri.AsNoTracking()
            .Where(si => si.CekiSatiriId == null && si.Sandik.ProjeId == projeId)
            .Where(si => sandikId == null || si.SandikId == sandikId)
            .Where(si => aranan == null || (si.BarkodNo != null && si.BarkodNo.ToUpper().Contains(aranan))
                || (si.Isim != null && si.Isim.ToUpper().Contains(aranan)))
            .Select(si => new UrunAnahtari { Kaynak = (int)BarkodEtiketUrunKaynagi.SandikIcerik, Id = si.Id, Grup = si.SandikId, Sira = 0 });
        var anahtarlar = cekiAnahtarlari.Concat(elleAnahtarlari);

        var toplam = await anahtarlar.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var sayfaAnahtarlari = await anahtarlar
            .OrderBy(a => a.Kaynak).ThenBy(a => a.Grup).ThenBy(a => a.Sira).ThenBy(a => a.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .ToListAsync(cancellationToken);

        var cekiIdleri = sayfaAnahtarlari.Where(a => a.Kaynak == (int)BarkodEtiketUrunKaynagi.CekiSatiri).Select(a => a.Id).ToList();
        var elleIdleri = sayfaAnahtarlari.Where(a => a.Kaynak == (int)BarkodEtiketUrunKaynagi.SandikIcerik).Select(a => a.Id).ToList();
        var cekiSatirlari = (await context.CekiSatirlari.AsNoTracking()
                .Where(cs => cekiIdleri.Contains(cs.Id))
                .Select(cs => new
                {
                    cs.Id, cs.BarkodNo, cs.Aciklama, cs.Remarks, cs.IstenenAdet, cs.CekideGecenSandikNo,
                    SandiktakiMiktar = sandikId == null ? null
                        : cs.SandikIcerikleri.Where(si => si.SandikId == sandikId).Sum(si => (decimal?)si.TahsisMiktari),
                    Birim = cs.BirimLookup != null ? cs.BirimLookup.Deger : "",
                    Sandiklar = cs.SandikIcerikleri.Select(si => si.Sandik.SandikNo).ToList()
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(cs => cs.Id, cs =>
            {
                var (tanim, aciklama) = BarkodEtiketKurali.AciklamaAyir(cs.Aciklama);
                var sandiklar = cs.Sandiklar.Count > 0
                    ? string.Join(", ", cs.Sandiklar.Distinct().Order(SandikNumarasiComparer.Instance))
                    : string.IsNullOrWhiteSpace(cs.CekideGecenSandikNo) ? "-" : cs.CekideGecenSandikNo;
                return new BarkodEtiketUrunSatiri(BarkodEtiketUrunKaynagi.CekiSatiri, cs.Id, cs.BarkodNo,
                    BarkodEtiketKurali.UrunBarkodu(cs.BarkodNo), tanim, aciklama, cs.Remarks, sandiklar, cs.IstenenAdet,
                    cs.SandiktakiMiktar, cs.Birim);
            });
        var elleEklenenler = (await context.SandikIcerikleri.AsNoTracking()
                .Where(si => elleIdleri.Contains(si.Id))
                .Select(si => new
                {
                    si.Id, si.BarkodNo, si.Isim, si.Miktar, si.Sandik.SandikNo,
                    Birim = si.BirimLookup != null ? si.BirimLookup.Deger : ""
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(si => si.Id, si => new BarkodEtiketUrunSatiri(BarkodEtiketUrunKaynagi.SandikIcerik, si.Id,
                si.BarkodNo ?? "", BarkodEtiketKurali.UrunBarkodu(si.BarkodNo), si.Isim?.Trim() ?? "", "", null, si.SandikNo,
                si.Miktar, sandikId == null ? null : si.Miktar, si.Birim));

        // Anahtar ile ayrıntı okuması arasında silinen satır sayfada atlanır.
        return new SayfaliSonuc<BarkodEtiketUrunSatiri>
        {
            Items = sayfaAnahtarlari
                .Select(a => (a.Kaynak == (int)BarkodEtiketUrunKaynagi.CekiSatiri ? cekiSatirlari : elleEklenenler).GetValueOrDefault(a.Id))
                .OfType<BarkodEtiketUrunSatiri>()
                .ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<DosyaIcerigi> SandikEtiketleriAsync(
        int projeId, IReadOnlyList<BarkodEtiketSandikSecimi> secimler, int yil, int ay, BarkodEtiketBicimi bicim,
        CancellationToken cancellationToken)
    {
        var proje = await ProjeAsync(projeId, cancellationToken);
        var idler = secimler.Select(s => s.SandikId).ToList();
        var sandiklar = await context.Sandiklar.AsNoTracking()
            .Where(s => s.ProjeId == projeId && idler.Contains(s.Id))
            .Select(s => new { s.Id, s.SandikNo })
            .ToDictionaryAsync(s => s.Id, s => s.SandikNo, cancellationToken);
        if (idler.Any(id => !sandiklar.ContainsKey(id)))
            throw new KayitBulunamadiException("Seçilen sandıklardan biri bu projede bulunamadı; listeyi yenileyin.");

        var tarih = BarkodEtiketKurali.Tarih(yil, ay);
        var etiketler = secimler.Select(s => GodexEtiketSablonu.Sandik(new SandikEtiketi(
            BarkodEtiketKurali.SandikBarkodu(s.SandikId), Metin(proje.SeriNo), Metin(sandiklar[s.SandikId]),
            Metin(proje.Musteri), Metin(proje.Lokasyon), tarih, s.Adet))).ToList();
        return Dosya(etiketler, bicim, "Sandik_Etiketleri", proje.ProjeNo);
    }

    public async Task<DosyaIcerigi> UrunEtiketleriAsync(
        int projeId, IReadOnlyList<BarkodEtiketUrunSecimi> secimler, BarkodEtiketBicimi bicim, CancellationToken cancellationToken)
    {
        var proje = await ProjeAsync(projeId, cancellationToken);
        var cekiIdleri = secimler.Where(s => s.Kaynak == BarkodEtiketUrunKaynagi.CekiSatiri).Select(s => s.Id).ToList();
        var elleIdleri = secimler.Where(s => s.Kaynak == BarkodEtiketUrunKaynagi.SandikIcerik).Select(s => s.Id).ToList();
        var cekiSatirlari = await context.CekiSatirlari.AsNoTracking()
            .Where(cs => cs.Ceki.ProjeId == projeId && cekiIdleri.Contains(cs.Id))
            .Select(cs => new { cs.Id, cs.BarkodNo, cs.Aciklama })
            .ToDictionaryAsync(cs => cs.Id, cs => (cs.BarkodNo, cs.Aciklama), cancellationToken);
        var elleEklenenler = await context.SandikIcerikleri.AsNoTracking()
            .Where(si => si.CekiSatiriId == null && si.Sandik.ProjeId == projeId && elleIdleri.Contains(si.Id))
            .Select(si => new { si.Id, si.BarkodNo, si.Isim })
            .ToDictionaryAsync(si => si.Id, si => (BarkodNo: si.BarkodNo ?? "", Aciklama: si.Isim ?? ""), cancellationToken);

        var etiketler = new List<DoluEtiket>(secimler.Count);
        foreach (var secim in secimler)
        {
            var kaynak = secim.Kaynak == BarkodEtiketUrunKaynagi.CekiSatiri ? cekiSatirlari : elleEklenenler;
            if (!kaynak.TryGetValue(secim.Id, out var urun))
                throw new KayitBulunamadiException("Seçilen ürünlerden biri bu projede bulunamadı; listeyi yenileyin.");
            var barkod = BarkodEtiketKurali.UrunBarkodu(urun.BarkodNo)
                ?? throw new IsKuraliException(
                    $"'{urun.BarkodNo}' barkodu etikete basılamaz: boş, {BarkodEtiketKurali.UrunBarkodHanesi} karakterden uzun ya da " +
                    "basılamayan karakter (Türkçe harf, ^, ~) içeriyor.");
            var (tanim, aciklama) = BarkodEtiketKurali.AciklamaAyir(urun.Aciklama);
            etiketler.Add(GodexEtiketSablonu.Urun(new UrunEtiketi(barkod, Metin(proje.SeriNo), Metin(proje.Musteri),
                Metin(proje.Lokasyon), Metin(tanim), Metin(aciklama), BarkodEtiketKurali.Remarks(secim.Remarks), secim.Adet)));
        }
        return Dosya(etiketler, bicim, "Urun_Etiketleri", proje.ProjeNo);
    }

    private static DosyaIcerigi Dosya(IReadOnlyList<DoluEtiket> etiketler, BarkodEtiketBicimi bicim, string onEk, string projeNo)
    {
        var ad = $"{onEk}_{DosyaAdiParcasi(projeNo)}_{TurkeyTime.Now:yyyyMMdd_HHmmss}";
        return bicim == BarkodEtiketBicimi.Ezpl
            ? DosyaIcerigi.YaziciKomutu(GodexEtiketSablonu.Ezpl(etiketler), ad + ".prn")
            : DosyaIcerigi.Pdf(BarkodEtiketPdf.Olustur(etiketler), ad + ".pdf");
    }

    private async Task ProjeVarMiAsync(int projeId, CancellationToken cancellationToken)
    {
        if (!await context.Projeler.AnyAsync(p => p.Id == projeId, cancellationToken))
            throw new KayitBulunamadiException("Proje bulunamadı.");
    }

    private static string? Aranan(string? arama) =>
        string.IsNullOrWhiteSpace(arama) ? null : arama.Trim().ToUpperInvariant();

    private static string Metin(string? deger) => BarkodEtiketKurali.Metin(deger);

    /// <summary>Proje no dosya adına yalnız harf, rakam ve tire olarak girer.</summary>
    private static string DosyaAdiParcasi(string projeNo)
    {
        var temiz = new string(projeNo.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
        return temiz.Length > 40 ? temiz[..40] : temiz;
    }

    /// <summary>Ürün listesinin sayfa anahtarı; iki kaynağın UNION ALL'ı aynı tipte olmalıdır.</summary>
    private sealed class UrunAnahtari
    {
        public int Kaynak { get; init; }
        public int Id { get; init; }
        public int Grup { get; init; }
        public int Sira { get; init; }
    }
}
