using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// PO öncesi iş onayı: onay talebinin oluşturulması, onaylanan talebin kalemlere uygulanması ve onaycıya gösterilen
/// talep detayı. Talep kalemlerin onaya sunulduğu andaki değişmez görüntüsünü saklar; uygulama anında kalemler bu
/// görüntüyle aynı değilse hiçbir kalem onaylanmaz.
/// </summary>
internal sealed class FinansPoOnayService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    FinansPoOnayOkuyucu okuyucu,
    ICurrentUserService currentUser) : IFinansPoOnayService
{
    private static readonly JsonSerializerOptions JsonAyarlari = new(JsonSerializerDefaults.Web);
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public Task<FinansPoOnayGonderSonucu> TalepleriOlusturAsync(
        FinansPoOnayGonderModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => TalepleriOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansPoOnayGonderSonucu> TalepleriOlusturCoreAsync(
        FinansPoOnayGonderModel model,
        CancellationToken cancellationToken)
    {
        var kullaniciId = currentUser.UserId
            ?? throw new IsKuraliException("Kullanıcı bilgisi alınamadı.", 401);
        var ids = model.IsKaydiIds.Distinct().ToArray();
        var works = await context.IsKaydiDetayQuery(true).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (works.Count != ids.Length)
            throw new KayitBulunamadiException("Onaya gönderilecek işlerden en az biri bulunamadı.");

        var bilgiler = await okuyucu.BilgileriGetirAsync(works, cancellationToken);
        var uygun = works
            .Where(x => FinansPoOnayKurali.OnayaGonderilebilir(bilgiler[x.Id].Durum) && FinansTutarKurallari.IsNet(x) > 0)
            .ToList();
        if (uygun.Count == 0)
        {
            throw IsKuraliException.Cakisma(
                "Seçilen işlerde onaya gönderilecek kalem yok: kalemler onaylı, onay bekliyor, fiyatsız, iptal " +
                "veya onay kapsamı dışında (özel/düzenli iş).");
        }

        var talepler = new List<FinansPoOnayTalebiOzeti>();
        var gruplar = uygun
            .GroupBy(FinansPoOnayKurali.GrupAnahtari)
            .Select(x => x.OrderBy(y => y.FinansTarihi).ThenBy(y => y.Id).ToList())
            .OrderBy(x => x[0].ProjeNo, StringComparer.Ordinal)
            .ThenBy(x => x[0].IsTuru)
            .ThenBy(x => x[0].ParaBirimiSnapshot, StringComparer.Ordinal);
        foreach (var kalemler in gruplar)
        {
            var onizleme = Onizleme(kalemler);
            var talep = new FinansPoOnayTalebi
            {
                ProjeId = onizleme.ProjeId,
                ProjeNo = onizleme.ProjeNo,
                Musteri = onizleme.Musteri,
                IsTuru = onizleme.IsTuru,
                ParaBirimi = onizleme.ParaBirimi,
                KalemSayisi = onizleme.KalemSayisi,
                NetTutar = onizleme.NetTutar,
                OnizlemeJson = JsonSerializer.Serialize(onizleme, JsonAyarlari),
                OnizlemeHash = FinansPoOnayOnizlemeButunlugu.HashOlustur(onizleme),
                TalepEdenKullaniciId = kullaniciId
            };
            context.Set<FinansPoOnayTalebi>().Add(talep);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var work in kalemler)
            {
                var dusenOnay = work.PoOnayNetTutar;
                var oncekiTalep = work.PoOnayTalebiId;
                // Düşmüş onay yeni kararla değişir; karar gelene kadar kalem onaylı sayılmaz.
                work.PoOnayTalebiId = talep.Id;
                work.PoOnayNetTutar = null;
                work.PoOnayParaBirimi = null;
                work.PoOnayTarihi = null;
                work.PoOnaylayanKullaniciId = null;
                var net = FinansTutarKurallari.IsNet(work);
                denetim.Ekle(nameof(FinansIsKaydi), work.Id, "Onaya Gönderme", "PoOnayTalebiId", oncekiTalep, talep.Id,
                    $"PO öncesi onay talebi; net {Tutar(net)} {work.ParaBirimiSnapshot}" +
                    (dusenOnay.HasValue ? $"; düşen onay {Tutar(dusenOnay.Value)}" : string.Empty));
            }

            talepler.Add(new FinansPoOnayTalebiOzeti(
                talep.Id,
                talep.ProjeId,
                talep.ProjeNo,
                onizleme.KategoriAdi,
                talep.KalemSayisi,
                talep.NetTutar,
                talep.ParaBirimi,
                talep.OnizlemeHash));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new FinansPoOnayGonderSonucu
        {
            Talepler = talepler,
            KalemSayisi = uygun.Count,
            AtlananKalemSayisi = works.Count - uygun.Count
        };
    }

    public Task<FinansPoOnayUygulamaSonucu> OnayiUygulaAsync(
        int talepId,
        string onizlemeHash,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => OnayiUygulaCoreAsync(talepId, onizlemeHash, cancellationToken), cancellationToken);

    private async Task<FinansPoOnayUygulamaSonucu> OnayiUygulaCoreAsync(
        int talepId,
        string onizlemeHash,
        CancellationToken cancellationToken)
    {
        var talep = await context.Set<FinansPoOnayTalebi>().FirstOrDefaultAsync(x => x.Id == talepId, cancellationToken)
            ?? throw new KayitBulunamadiException("PO onay talebi bulunamadı.");
        if (talep.UygulamaTarihi.HasValue)
            throw IsKuraliException.Cakisma("Bu onay talebi daha önce uygulandı.");

        var onizleme = GuvenliOnizleme(talep);
        if (onizleme is null ||
            !string.Equals(talep.OnizlemeHash, onizlemeHash?.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw IsKuraliException.Cakisma("Onay talebinin kayıtlı görüntüsü doğrulanamadı; onay uygulanmadı.");
        }

        var ids = onizleme.Kalemler.Select(x => x.IsKaydiId).ToArray();
        var works = await context.IsKaydiDetayQuery(true)
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var degisenler = KalemDurumlari(onizleme, works, talep.Id).Where(x => x.Degisti).ToList();
        if (degisenler.Count > 0)
        {
            var ornekler = string.Join("; ", degisenler.Take(3).Select(x =>
                $"{onizleme.Kalemler.First(y => y.IsKaydiId == x.IsKaydiId).IsAdi}: {x.Degisiklik}"));
            throw IsKuraliException.Cakisma(
                $"Talep onaya sunulduktan sonra {degisenler.Count} kalem değişti ({ornekler}); onay uygulanmadı. " +
                "Kalemleri yeniden onaya gönderin.");
        }

        var simdi = TurkeyTime.Now;
        foreach (var kalem in onizleme.Kalemler)
        {
            var work = works[kalem.IsKaydiId];
            work.PoOnayNetTutar = kalem.NetTutar;
            work.PoOnayParaBirimi = kalem.ParaBirimi;
            work.PoOnayTarihi = simdi;
            work.PoOnaylayanKullaniciId = currentUser.UserId;
            denetim.Ekle(nameof(FinansIsKaydi), work.Id, "PO Onayı", "PoOnayNetTutar", null, kalem.NetTutar,
                $"Talep #{talep.Id} onaylandı ({kalem.ParaBirimi}).");
        }

        talep.UygulamaTarihi = simdi;
        await context.SaveChangesAsync(cancellationToken);
        return new FinansPoOnayUygulamaSonucu(talep.Id, onizleme.KalemSayisi, onizleme.NetTutar, onizleme.ParaBirimi);
    }

    public async Task<FinansPoOnayDetayModel?> TalepDetayiAsync(
        int talepId,
        int talepEdenKullaniciId,
        int? projeId,
        CancellationToken cancellationToken)
    {
        // Onay kaydının referansı yalnız sunucuda yazılır; talep yine de aynı kişinin ve projenin talebi olmalıdır.
        var talep = await context.Set<FinansPoOnayTalebi>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == talepId &&
                                      x.TalepEdenKullaniciId == talepEdenKullaniciId &&
                                      x.ProjeId == projeId, cancellationToken);
        var onizleme = talep is null ? null : GuvenliOnizleme(talep);
        if (talep is null || onizleme is null)
            return null;

        var ids = onizleme.Kalemler.Select(x => x.IsKaydiId).ToArray();
        var works = await context.IsKaydiDetayQuery()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var durumlar = KalemDurumlari(onizleme, works, talep.Id);
        return new FinansPoOnayDetayModel
        {
            TalepId = talep.Id,
            Onizleme = onizleme,
            KalemDurumlari = durumlar,
            DegisenKalemSayisi = durumlar.Count(x => x.Degisti),
            UygulamaTarihi = talep.UygulamaTarihi
        };
    }

    /// <summary>Görüntüdeki her kalemin şu anki değerleri; onaydan önce değişen kalem onayı engeller.</summary>
    private static IReadOnlyList<FinansPoOnayKalemDurumu> KalemDurumlari(
        FinansPoOnayOnizleme onizleme,
        IReadOnlyDictionary<int, FinansIsKaydi> works,
        int talepId)
        => onizleme.Kalemler.Select(kalem =>
        {
            if (!works.TryGetValue(kalem.IsKaydiId, out var work))
                return new FinansPoOnayKalemDurumu(kalem.IsKaydiId, null, null, true, "Kalem silindi.");

            var net = FinansTutarKurallari.IsNet(work);
            string? degisiklik = null;
            if (work.IptalEdildi || !work.KaynakAktif)
                degisiklik = "Kalem iptal edildi.";
            else if (work.PoOnayTalebiId != talepId)
                degisiklik = "Kalem başka bir onay talebine geçti.";
            else if (work.IsTuru != onizleme.IsTuru)
                degisiklik = "Kalemin iş türü değişti.";
            else if (!string.Equals(work.ParaBirimiSnapshot, kalem.ParaBirimi, StringComparison.Ordinal))
                degisiklik = $"Para birimi {kalem.ParaBirimi} → {work.ParaBirimiSnapshot}.";
            else if (net != kalem.NetTutar)
                degisiklik = $"Net tutar {Tutar(kalem.NetTutar)} → {Tutar(net)} {work.ParaBirimiSnapshot}.";

            return new FinansPoOnayKalemDurumu(kalem.IsKaydiId, net, work.ParaBirimiSnapshot, degisiklik is not null, degisiklik);
        }).ToArray();

    private static FinansPoOnayOnizleme Onizleme(IReadOnlyList<FinansIsKaydi> kalemler)
    {
        var ilk = kalemler[0];
        var kategori = FinansKategoriKurali.Kategori(ilk.IsTuru, ilk.DuzenliIsId);
        var satirlar = kalemler.Select(work =>
        {
            var hacim = FinansModelEslemeleri.Hacim(work);
            return new FinansPoOnayKalemi
            {
                IsKaydiId = work.Id,
                IsAdi = work.IsAdi,
                SandikNo = work.SandikNo,
                SandikAdi = work.SandikAdi,
                SandikTipi = work.SandikTipi,
                Boy = work.Boy,
                En = work.En,
                Yukseklik = work.Yukseklik,
                Adet = work.Adet,
                Birim = work.Birim,
                ToplamM3 = work.ToplamM3,
                AhsapM3 = hacim.Ahsap,
                KontraM3 = hacim.Kontra,
                FiyatlandirmaBirimi = work.FiyatlandirmaBirimiSnapshot,
                BirimFiyat = work.BirimFiyatSnapshot,
                AhsapBirimFiyat = work.AhsapBirimFiyatSnapshot,
                KdvOrani = work.KdvOraniSnapshot,
                NetTutar = FinansTutarKurallari.IsNet(work),
                SiparisNetTutar = FinansTutarKurallari.SiparisNet(work),
                ParaBirimi = work.ParaBirimiSnapshot,
                UretimTarihi = work.UretimTarihi,
                FinansTarihi = work.FinansTarihi
            };
        }).ToArray();
        var net = satirlar.Sum(x => x.NetTutar);
        var kdv = satirlar.Sum(x => FinansTutarKurallari.Para(x.NetTutar * x.KdvOrani / 100m));
        return new FinansPoOnayOnizleme
        {
            Surum = FinansPoOnayOnizlemeButunlugu.Surum,
            ProjeId = ilk.ProjeId,
            ProjeNo = ilk.ProjeNo,
            Musteri = ilk.Musteri,
            IsTuru = ilk.IsTuru,
            Kategori = kategori,
            KategoriAdi = FinansKategoriKurali.Ad(kategori),
            ParaBirimi = ilk.ParaBirimiSnapshot,
            KalemSayisi = satirlar.Length,
            NetTutar = net,
            KdvTutari = kdv,
            ToplamTutar = net + kdv,
            ToplamM3 = satirlar.Sum(x => x.ToplamM3),
            AhsapM3 = satirlar.Sum(x => x.AhsapM3),
            KontraM3 = satirlar.Sum(x => x.KontraM3),
            IlkFinansTarihi = satirlar.Min(x => x.FinansTarihi),
            SonFinansTarihi = satirlar.Max(x => x.FinansTarihi),
            Kalemler = satirlar
        };
    }

    /// <summary>Saklanan görüntü; sürümü veya özeti tutmuyorsa kullanılmaz.</summary>
    private static FinansPoOnayOnizleme? GuvenliOnizleme(FinansPoOnayTalebi talep)
    {
        try
        {
            var onizleme = JsonSerializer.Deserialize<FinansPoOnayOnizleme>(talep.OnizlemeJson, JsonAyarlari);
            return onizleme is { Surum: FinansPoOnayOnizlemeButunlugu.Surum } &&
                   onizleme.Kalemler.Count > 0 &&
                   FinansPoOnayOnizlemeButunlugu.HashDogrula(onizleme, talep.OnizlemeHash)
                ? onizleme
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Tutar(decimal deger) => deger.ToString("N2", Tr);
}
