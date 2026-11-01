using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>Planlayıcının okuduğu, önceden yüklenmiş proje verisi; planlayıcı veritabanına gitmez.</summary>
internal sealed record OtomatikSahaAktarimGirdisi(
    Proje Proje,
    IReadOnlyCollection<Sandik> Sandiklar,
    IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>> EtkinIcerikMap,
    IReadOnlyDictionary<int, CekiSatiri> KokSatirlar,
    IReadOnlyCollection<SandikIcerik> TumFizikselIcerikler,
    IReadOnlyDictionary<int, decimal> PlanlanmisTamamlamaMap,
    IReadOnlyDictionary<int, decimal> AktifTamamlamaMap,
    IReadOnlySet<int> AktifAktarimaBagliSandikIds);

/// <summary>Sahaya aktarılacak bir ürün miktarı; <see cref="KaynakSandikId"/> ürünün ana projedeki sandığıdır.</summary>
internal sealed record OtomatikAktarimUrunu(CekiSatiri Satir, int? KaynakSandikId, string? KaynakSandikNo, decimal Miktar);

/// <summary>Sevk edilmemiş ana sandığın elle "Sandıkları Sahaya Aktar" ile aynı kurallarla aktarılacak içeriği.</summary>
internal sealed record OtomatikSandikAktarimi(Sandik KaynakSandik, IReadOnlyList<OtomatikAktarimUrunu> Urunler);

/// <summary>
/// Otomatik aktarıma girmeyen sandık ya da ürün: kısa <see cref="Neden"/> ve ürün ürün <see cref="Ayrintilar"/>
/// (elle sandık aktarımının ret gerekçeleri); önizlemede gösterilir, elle yollar açık kalır.
/// </summary>
internal sealed record OtomatikAktarimAtlanan(string? SandikNo, CekiSatiri? Satir, string Neden, IReadOnlyList<string>? Ayrintilar = null);

internal sealed record OtomatikSahaAktarimPlani(
    IReadOnlyList<OtomatikSandikAktarimi> SandikAktarimlari,
    IReadOnlyList<OtomatikAktarimUrunu> EksikKalemleri,
    IReadOnlyList<OtomatikAktarimAtlanan> Atlananlar,
    string PlanOzeti)
{
    public bool BosMu => SandikAktarimlari.Count == 0 && EksikKalemleri.Count == 0;
}

/// <summary>
/// Sevk sonrası kalanları kullanıcının iki elle yoluyla ("Sandıkları sahaya aktar" ve "Eksiklerden oluştur") aynı
/// kurallarla otomatik seçer:
/// <list type="bullet">
/// <item>Sevk edilmemiş sandık (1 numaralı hariç) elle sandık aktarımının güvenlik kuralını
/// (<see cref="SandikBazliSahaAktarimGuvenlikKural"/>) geçiyorsa sandığın kendisi aynı numarayla sahaya aktarılır.</item>
/// <item>Ürünlerin kalanı ("Eksiklerden oluştur"daki planlanabilir kalan) saha projesinin 1 numaralı eksikler sandığına
/// gider: sevk edilmiş sandıklardakiler ve sandık olarak gidemeyen (işlem görmüş içerikli) sevk edilmemiş
/// sandıklardakiler; sandığın kendisi ana projede kalır. Elle "Eksiklerden oluştur" da bu ürünleri gösterir.</item>
/// <item>1 numaralı ana sandık hiçbir koşulda sandık olarak aktarılmaz; sevk edilmemişse ürünleri de ana projede kalır,
/// sevk edilmişse eksikleri diğer sevkli sandıklar gibi gider.</item>
/// <item>Sevkiyat düzeltmesi açık sandık ve ürünleri plana girmez (içerik düzeltme bitene kadar değişebilir).</item>
/// </list>
/// Plan veri değiştirmez; <see cref="OtomatikSahaAktarimPlani.PlanOzeti"/> önizleme ile uygulama arasındaki değişikliği yakalar.
/// </summary>
internal static class OtomatikSahaAktarimPlanlayici
{
    public static Result<OtomatikSahaAktarimPlani> Planla(OtomatikSahaAktarimGirdisi girdi)
    {
        if (girdi.Proje.ProjeTipiId != (int)ProjeTipi.Normal)
            return Result<OtomatikSahaAktarimPlani>.Failure("Otomatik sahaya aktarım yalnız normal projelerde yapılır.");

        // Sevkiyat düzeltmesi açık sandık ne sevkli ne sevksiz sayılır: içeriği düzeltme bitene kadar değişebilir.
        var sevkliSandikIds = girdi.Sandiklar
            .Where(s => s.DurumId == (int)SandikDurum.Sevkedildi && !s.SevkiyatDuzeltmeAcikMi)
            .Select(s => s.Id)
            .ToHashSet();
        var projeSevkDurumunda = girdi.Proje.DurumId is (int)ProjeDurum.SevkEdildi or (int)ProjeDurum.EksikSevkEdildi;

        if (sevkliSandikIds.Count == 0 && !projeSevkDurumunda)
        {
            return Result<OtomatikSahaAktarimPlani>.Failure(
                "Otomatik sahaya aktarım yalnız sevk edilmiş (tam veya kısmi) projede yapılır.",
                409);
        }

        var sandikAktarimlari = new List<OtomatikSandikAktarimi>();
        var atlananlar = new List<OtomatikAktarimAtlanan>();
        var sandiklaAktarilanSatirIds = new HashSet<int>();
        // Eksikleri alınabilen sandıklar: sevk edilmişler ve sandık olarak gidemeyen sevk edilmemişler. Bu ikincilerin
        // önizleme satırı eksik sayısı belli olunca (aşağıda) yazılır.
        var eksikAlinabilirSandikIds = new HashSet<int>(sevkliSandikIds);
        var kalanSandiklar = new List<(Sandik Sandik, string Neden, IReadOnlyList<string> Ayrintilar)>();

        foreach (var sandik in girdi.Sandiklar
                     .Where(s => !sevkliSandikIds.Contains(s.Id))
                     .OrderBy(s => NumaraSirasi(s.SandikNo))
                     .ThenBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase))
        {
            var icerikler = girdi.EtkinIcerikMap
                .GetValueOrDefault(sandik.Id, Array.Empty<SandikIcerik>())
                .Where(SandikBazliSahaAktarimGuvenlikKural.IncelenecekIcerikMi)
                .ToList();

            if (sandik.SevkiyatDuzeltmeAcikMi)
            {
                atlananlar.Add(new(sandik.SandikNo, null, "Sevkiyat düzeltmesi açık; düzeltme bitince tekrar deneyin."));
                continue;
            }

            // Sevk edilmemiş ana "1" sandığının ürünleri de ana projede kalır (eksikleri plana girmez); sevk edilmiş "1"in
            // eksikleri diğer sevkli sandıklar gibi gider.
            if (SahaAktarimSandikKurali.BirNumaraliSandikMi(sandik.SandikNo))
            {
                atlananlar.Add(new(sandik.SandikNo, null, "1 numaralı sandık ana projede kalır; sandık ve ürünleri sahaya aktarılmaz."));
                continue;
            }

            if (girdi.AktifAktarimaBagliSandikIds.Contains(sandik.Id))
            {
                atlananlar.Add(new(sandik.SandikNo, null, "Sandık zaten sahaya aktarılmış."));
                continue;
            }

            if (icerikler.Count == 0)
            {
                atlananlar.Add(new(sandik.SandikNo, null, "Sandıkta aktarılacak ürün yok."));
                continue;
            }

            var dogrulama = SandikBazliSahaAktarimGuvenlikKural.Dogrula(
                [sandik],
                girdi.EtkinIcerikMap,
                girdi.KokSatirlar,
                girdi.TumFizikselIcerikler,
                girdi.AktifTamamlamaMap);

            if (!dogrulama.Basarili)
            {
                // Elle "Sandıkları sahaya aktar" da bu sandığı kabul etmez; ürün ürün gerekçe ayrıntıda gösterilir.
                eksikAlinabilirSandikIds.Add(sandik.Id);
                kalanSandiklar.Add((
                    sandik,
                    "Sandıkta işlem görmüş ürün var (gelmiş, konulmuş ya da karşılanmış); sandık ana projede kalır.",
                    dogrulama.Engeller.ToList()));
                continue;
            }

            var urunler = dogrulama.Adaylar
                .Select(aday => new OtomatikAktarimUrunu(
                    girdi.KokSatirlar[aday.CekiSatiriId],
                    sandik.Id,
                    sandik.SandikNo,
                    aday.Miktar))
                .OrderBy(u => u.Satir.SiraNo)
                .ToList();

            sandikAktarimlari.Add(new OtomatikSandikAktarimi(sandik, urunler));
            foreach (var urun in urunler)
                sandiklaAktarilanSatirIds.Add(urun.Satir.Id);
        }

        // Ürünün yeri, miktarları sıfır olsa da gerçek tahsis kaydından okunur (elle sandık aktarımındaki inceleme kuralı);
        // yoksa sevk edilmemiş sandıkta duran ürün tahsissiz sanılıp eksiklere gönderilirdi.
        var sandikNoMap = girdi.Sandiklar.ToDictionary(s => s.Id, s => s.SandikNo);
        var satirIcerikleri = girdi.EtkinIcerikMap
            .SelectMany(pair => pair.Value
                .Where(i => i.CekiSatiriId.HasValue && SandikBazliSahaAktarimGuvenlikKural.IncelenecekIcerikMi(i))
                .Select(i => (SandikId: pair.Key, Icerik: i)))
            .GroupBy(x => x.Icerik.CekiSatiriId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var eksikKalemleri = new List<OtomatikAktarimUrunu>();
        foreach (var satir in girdi.KokSatirlar.Values.OrderBy(s => s.SiraNo).ThenBy(s => s.Id))
        {
            if (sandiklaAktarilanSatirIds.Contains(satir.Id))
                continue;

            // "Eksiklerden oluştur" listesiyle aynı kalan: satırın kalanından daha önce sahaya planlanmış miktar düşülür.
            var planlanmis = girdi.PlanlanmisTamamlamaMap.GetValueOrDefault(satir.Id);
            var planlanabilirKalan = Math.Max(satir.KalanMiktar - planlanmis, 0);
            if (planlanabilirKalan <= 0)
                continue;

            var icerikler = satirIcerikleri.GetValueOrDefault(satir.Id) ?? [];

            // Ürün eksikleri alınamayan bir sandıkta da duruyorsa (sevkiyat düzeltmesi açık, zaten sahaya aktarılmış)
            // o sandığın kararı geçerlidir; satır plana girmez.
            if (icerikler.Any(x => !eksikAlinabilirSandikIds.Contains(x.SandikId)))
                continue;

            var kaynakSandikIds = icerikler.Select(x => x.SandikId).Distinct().ToList();
            var kaynakSandikNo = kaynakSandikIds.Count == 0
                ? null
                : string.Join(", ", kaynakSandikIds.Select(id => sandikNoMap[id]).OrderBy(NumaraSirasi));

            if (satir.HataliMiktar > 0 || satir.DurumId == (int)UrunDurum.HataliUyumsuzGonderim)
            {
                atlananlar.Add(new(kaynakSandikNo, satir, "Hatalı veya uyumsuz ürün; iade süreci elle yürütülür."));
                continue;
            }

            // "Eksiklerden oluştur" ile aynı miktar: planlanabilir kalanın tamamı.
            eksikKalemleri.Add(new OtomatikAktarimUrunu(
                satir,
                kaynakSandikIds.Count == 1 ? kaynakSandikIds[0] : null,
                kaynakSandikNo,
                planlanabilirKalan));
        }

        // Ana projede kalan sevk edilmemiş sandıklar: kısa neden + eksiklerinden kaçının sahaya gideceği; ürün ürün ret
        // gerekçeleri ayrıntıdadır.
        foreach (var (sandik, neden, ayrintilar) in kalanSandiklar)
        {
            var eksikSayisi = eksikKalemleri.Count(e =>
                (satirIcerikleri.GetValueOrDefault(e.Satir.Id) ?? []).Any(x => x.SandikId == sandik.Id));
            var sonuc = eksikSayisi > 0
                ? $" Eksik {eksikSayisi} ürünü sahaya eksik olarak aktarılır."
                : " Sahaya gidecek eksik ürünü yok.";
            atlananlar.Add(new(sandik.SandikNo, null, neden + sonuc, ayrintilar));
        }
        // Önce sandıklar (numara sırasıyla), sonra ürün satırları; sıralama kararlıdır.
        var siraliAtlananlar = atlananlar
            .OrderBy(a => a.Satir is null ? 0 : 1)
            .ThenBy(a => a.Satir is null ? NumaraSirasi(a.SandikNo) : 0)
            .ThenBy(a => a.Satir is null ? a.SandikNo : null, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<OtomatikSahaAktarimPlani>.Success(new OtomatikSahaAktarimPlani(
            sandikAktarimlari,
            eksikKalemleri,
            siraliAtlananlar,
            PlanOzetiHesapla(sandikAktarimlari, eksikKalemleri)));
    }

    /// <summary>
    /// Planın kanonik metninin SHA-256 özeti. Sıra bağımsızdır; yalnız aktarılacak sandık, satır ve miktarlar girer
    /// (hedef saha seçimi uygulamada ayrıca doğrulanır).
    /// </summary>
    public static string PlanOzetiHesapla(
        IEnumerable<OtomatikSandikAktarimi> sandikAktarimlari,
        IEnumerable<OtomatikAktarimUrunu> eksikKalemleri)
    {
        var satirlar = sandikAktarimlari
            .Select(s => $"S|{s.KaynakSandik.Id}|" + string.Join(",", s.Urunler
                .OrderBy(u => u.Satir.Id)
                .Select(u => $"{u.Satir.Id}:{MiktarMetni(u.Miktar)}")))
            .Concat(eksikKalemleri.Select(e =>
                $"E|{e.Satir.Id}|{e.KaynakSandikId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{MiktarMetni(e.Miktar)}"))
            .OrderBy(x => x, StringComparer.Ordinal);

        return Ozet(string.Join("\n", satirlar));
    }

    /// <summary>Metnin SHA-256 özeti (küçük harf onaltılık, 64 karakter).</summary>
    public static string Ozet(string metin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metin))).ToLowerInvariant();

    private static string MiktarMetni(decimal miktar) =>
        miktar.ToString("0.############", CultureInfo.InvariantCulture);

    private static int NumaraSirasi(string? sandikNo)
    {
        var rakamlar = new string((sandikNo ?? string.Empty).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(rakamlar, out var sayi) ? sayi : int.MaxValue;
    }
}
