using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>Planın hedef saha projesindeki karşılığı: mevcut sandık (<see cref="HedefSandikId"/>) ya da açılacak numara.</summary>
internal sealed record OtomatikSahaHedefSandigi(int? HedefSandikId, string SandikNo, bool NumaraDegisti);

/// <summary>Önizleme ve uygulamanın ortak girdisi: plan, hedef saha projesi ve sandık eşleşmeleri.</summary>
internal sealed class OtomatikSahaAktarimHazirligi
{
    public required Proje Proje { get; init; }
    public required OtomatikSahaAktarimPlani Plan { get; init; }
    public required IReadOnlyList<Proje> HedefSecenekleri { get; init; }
    public required IReadOnlyDictionary<int, int> HedefSandikSayilari { get; init; }
    public Proje? HedefSahaProje { get; init; }
    public bool YeniSahaProjesi { get; init; }
    public bool HedefSecimiGerekli { get; init; }
    public required string YeniSahaProjeNoOnerisi { get; init; }
    public required IReadOnlyDictionary<int, OtomatikSahaHedefSandigi> SandikHedefleri { get; init; }
    public OtomatikSahaHedefSandigi? EksiklerHedefi { get; init; }

    /// <summary>
    /// Önizlemenin değişmez özeti: plan özeti + hedef proje + hedef sandık eşleşmeleri. Uygulama bunu karşılaştırır;
    /// önizlemeden sonra hedefteki sandık değişirse (ör. "1" sevk edildi) kullanıcıya söylenmeden başka sandık açılmaz.
    /// </summary>
    public required string OnizlemeOzeti { get; init; }
}

/// <summary>
/// Otomatik sahaya aktarımın veri okuma adımı: projeyi, sandıkları, kök satırları ve saha durumunu yükler, planı
/// kurar ve hedef saha projesini seçer. Veri değiştirmez; önizleme sorgusu ile uygulama komutu aynı sonucu görür.
/// </summary>
internal sealed class OtomatikSahaAktarimHazirlayici(
    IUnitOfWork unitOfWork,
    ISandikService sandikService,
    ISahaTamamlamaService sahaTamamlamaService)
{
    public async Task<Result<OtomatikSahaAktarimHazirligi>> HazirlaAsync(
        int projeId,
        int? hedefSahaProjeId,
        bool yeniProje,
        CancellationToken cancellationToken)
    {
        var projeRepo = unitOfWork.GetRepository<Proje>();
        var proje = await projeRepo.GetByIdAsync(projeId);
        if (proje == null)
            return Result<OtomatikSahaAktarimHazirligi>.Failure("Proje bulunamadı.", 404);

        var sandiklar = (await unitOfWork.GetRepository<Sandik>().FindAsync(s => s.ProjeId == projeId)).ToList();
        var sandikIds = sandiklar.Select(s => s.Id).ToList();
        var etkinIcerikMap = await sandikService.GetEtkinSandikIcerikleriAsync(sandikIds, cancellationToken);

        var cekiIds = (await unitOfWork.GetRepository<Ceki>().FindAsync(c => c.ProjeId == projeId))
            .Select(c => c.Id)
            .ToList();
        var cekiSatiriRepo = unitOfWork.GetRepository<CekiSatiri>();
        var kokSatirlar = (await cekiSatiriRepo.FindAsync(cs =>
                cekiIds.Contains(cs.CekiId) &&
                !cs.KaynakCekiSatiriId.HasValue))
            .ToDictionary(cs => cs.Id);
        var kokSatirIds = kokSatirlar.Keys.ToList();

        var planlanmisTamamlamaMap = kokSatirIds.Count == 0
            ? new Dictionary<int, decimal>()
            : (await cekiSatiriRepo.FindAsync(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kokSatirIds.Contains(cs.KaynakCekiSatiriId.Value)))
                .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(cs => cs.IstenenAdet));

        var tumFizikselIcerikler = kokSatirIds.Count == 0
            ? new List<SandikIcerik>()
            : (await unitOfWork.GetRepository<SandikIcerik>().FindAsync(i =>
                    i.CekiSatiriId.HasValue &&
                    kokSatirIds.Contains(i.CekiSatiriId.Value)))
                .ToList();

        var aktifTamamlamaMap = kokSatirIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await sahaTamamlamaService.GetAktifTamamlamaMapAsync(kokSatirIds, cancellationToken);
        var sahaDurumu = proje.ProjeTipiId == (int)ProjeTipi.Normal
            ? await sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(sandikIds, cancellationToken)
            : new KaynakSandikSahaAktarimDurumu();

        var planSonucu = OtomatikSahaAktarimPlanlayici.Planla(new OtomatikSahaAktarimGirdisi(
            proje,
            sandiklar,
            etkinIcerikMap,
            kokSatirlar,
            tumFizikselIcerikler,
            planlanmisTamamlamaMap,
            aktifTamamlamaMap,
            sahaDurumu.AktifAktarimaBagliSandikIds));

        if (!planSonucu.IsSuccess)
            return SahaAktarimSonucEslemesi.Basarisiz<OtomatikSahaAktarimHazirligi>(planSonucu);

        var plan = planSonucu.Value!;
        var hedefSecenekleri = await HedefSecenekleriniBulAsync(proje, cancellationToken);
        var hedefSecenekIds = hedefSecenekleri.Select(p => p.Id).ToList();
        var hedefSandikSayilari = hedefSecenekIds.Count == 0
            ? new Dictionary<int, int>()
            : (await unitOfWork.GetRepository<Sandik>().FindAsync(s => hedefSecenekIds.Contains(s.ProjeId)))
                .GroupBy(s => s.ProjeId)
                .ToDictionary(g => g.Key, g => g.Count());

        Proje? hedef = null;
        var yeni = false;
        var secimGerekli = false;
        if (yeniProje)
        {
            yeni = true;
        }
        else if (hedefSahaProjeId.HasValue)
        {
            hedef = await projeRepo.GetByIdAsync(hedefSahaProjeId.Value);
            if (hedef == null)
                return Result<OtomatikSahaAktarimHazirligi>.Failure("Hedef saha projesi bulunamadı.", 404);
            if (hedef.ProjeTipiId != (int)ProjeTipi.Saha)
                return Result<OtomatikSahaAktarimHazirligi>.Failure("Aktarım sadece saha projelerine eklenebilir.");
            if (SevkEdilmisMi(hedef))
                return Result<OtomatikSahaAktarimHazirligi>.Failure("Sevk edilmiş veya kısmi sevk edilmiş saha projesine yeni aktarım eklenemez.");
        }
        else if (hedefSecenekleri.Count == 1)
        {
            hedef = await projeRepo.GetByIdAsync(hedefSecenekleri[0].Id);
        }
        else if (hedefSecenekleri.Count == 0)
        {
            yeni = true;
        }
        else
        {
            secimGerekli = true;
        }

        var (sandikHedefleri, eksiklerHedefi) = await HedefSandiklariniEslestirAsync(plan, hedef);
        var hedefMetni = hedef != null ? $"H{hedef.Id}" : yeni ? "Y" : "-";
        var eksikMetni = eksiklerHedefi == null ? "-" : $"{eksiklerHedefi.HedefSandikId?.ToString() ?? "+"}:{eksiklerHedefi.SandikNo}";
        var eslesmeMetni = string.Join(";", sandikHedefleri
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}>{p.Value.HedefSandikId?.ToString() ?? "+"}:{p.Value.SandikNo}"));

        return Result<OtomatikSahaAktarimHazirligi>.Success(new OtomatikSahaAktarimHazirligi
        {
            Proje = proje,
            Plan = plan,
            HedefSecenekleri = hedefSecenekleri,
            HedefSandikSayilari = hedefSandikSayilari,
            HedefSahaProje = hedef,
            YeniSahaProjesi = yeni,
            HedefSecimiGerekli = secimGerekli,
            YeniSahaProjeNoOnerisi = await SahaAktarimIslemi.SahaProjeNoUretAsync(unitOfWork, $"{proje.ProjeNo}-SAHA"),
            SandikHedefleri = sandikHedefleri,
            EksiklerHedefi = eksiklerHedefi,
            OnizlemeOzeti = OtomatikSahaAktarimPlanlayici.Ozet($"{plan.PlanOzeti}|{hedefMetni}|{eksikMetni}|{eslesmeMetni}")
        });
    }

    /// <summary>
    /// Ana projeye aktarım defteriyle bağlı ya da "&lt;ProjeNo&gt;-SAHA" adlı, sevk edilmemiş saha projeleri.
    /// </summary>
    private async Task<List<Proje>> HedefSecenekleriniBulAsync(Proje proje, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bagliSahaIds = (await unitOfWork.GetRepository<SahaAktarimKalemi>().FindAsync(k => k.KaynakProjeId == proje.Id))
            .Select(k => k.SahaProjeId)
            .Concat((await unitOfWork.GetRepository<SahaAktarim>().FindAsync(a => a.KaynakProjeId == proje.Id))
                .Select(a => a.SahaProjeId))
            .Distinct()
            .ToList();
        var adOnEki = $"{proje.ProjeNo}-SAHA";

        return (await unitOfWork.GetRepository<Proje>().FindAsync(p =>
                p.ProjeTipiId == (int)ProjeTipi.Saha &&
                (bagliSahaIds.Contains(p.Id) || p.ProjeNo == adOnEki || p.ProjeNo.StartsWith(adOnEki + "-"))))
            .Where(p => !SevkEdilmisMi(p))
            .OrderBy(p => p.ProjeNo)
            .ToList();
    }

    /// <summary>
    /// Eksikler, hedefteki sevk edilmemiş "1" sandığına eklenir (yoksa "1" açılır). Sevk edilmemiş sandık, hedefte aynı
    /// numaralı boş sandık varsa ona (geri alınmış aktarımın bıraktığı), yoksa yeni numarayla açılır; numara doluysa "-2".
    /// Sıra, uygulamanın sandık taslaklarını gönderdiği sırayla aynıdır.
    /// </summary>
    private async Task<(Dictionary<int, OtomatikSahaHedefSandigi> SandikHedefleri, OtomatikSahaHedefSandigi? EksiklerHedefi)>
        HedefSandiklariniEslestirAsync(OtomatikSahaAktarimPlani plan, Proje? hedef)
    {
        var hedefSandiklar = hedef == null
            ? new List<Sandik>()
            : (await unitOfWork.GetRepository<Sandik>().FindAsync(s => s.ProjeId == hedef.Id)).ToList();
        var hedefSandikIds = hedefSandiklar.Select(s => s.Id).ToList();
        var doluSandikIds = hedefSandikIds.Count == 0
            ? new HashSet<int>()
            : (await unitOfWork.GetRepository<SandikIcerik>().FindAsync(i => hedefSandikIds.Contains(i.SandikId)))
                .Select(i => i.SandikId)
                .ToHashSet();
        // Numara çakışması çeki biçimiyle denetlenir: hedefte "5" varken kaynak "05" yanına ikinci bir "5" açmaz.
        var kullanilanNolar = hedefSandiklar
            .Select(s => KoliNoKurali.Normalize(s.SandikNo))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eklenebilirSandiklar = hedefSandiklar
            .Where(s => s.DurumId != (int)SandikDurum.Sevkedildi && !s.SevkiyatDuzeltmeAcikMi)
            .OrderBy(s => s.Id)
            .ToList();

        OtomatikSahaHedefSandigi? eksiklerHedefi = null;
        if (plan.EksikKalemleri.Count > 0)
        {
            var mevcutBir = eklenebilirSandiklar.FirstOrDefault(s => SahaAktarimSandikKurali.BirNumaraliSandikMi(s.SandikNo));
            eksiklerHedefi = mevcutBir != null
                ? new OtomatikSahaHedefSandigi(mevcutBir.Id, mevcutBir.SandikNo, false)
                : YeniNumara(SahaAktarimConstants.EksiklerSandikNo, kullanilanNolar);
        }

        var kullanilanHedefIds = new HashSet<int>();
        var sandikHedefleri = new Dictionary<int, OtomatikSahaHedefSandigi>();
        foreach (var aktarim in plan.SandikAktarimlari)
        {
            var kaynakNo = KoliNoKurali.Normalize(aktarim.KaynakSandik.SandikNo);
            var bosAyniNumara = eklenebilirSandiklar.FirstOrDefault(s =>
                !kullanilanHedefIds.Contains(s.Id) &&
                !doluSandikIds.Contains(s.Id) &&
                KoliNoKurali.Normalize(s.SandikNo) == kaynakNo);

            if (bosAyniNumara != null)
            {
                kullanilanHedefIds.Add(bosAyniNumara.Id);
                sandikHedefleri[aktarim.KaynakSandik.Id] = new OtomatikSahaHedefSandigi(bosAyniNumara.Id, bosAyniNumara.SandikNo, false);
            }
            else
            {
                sandikHedefleri[aktarim.KaynakSandik.Id] = YeniNumara(aktarim.KaynakSandik.SandikNo, kullanilanNolar);
            }
        }

        return (sandikHedefleri, eksiklerHedefi);
    }

    private static OtomatikSahaHedefSandigi YeniNumara(string istenenNo, HashSet<string> kullanilanNolar)
    {
        var istenen = KoliNoKurali.Normalize(istenenNo);
        var no = SahaAktarimIslemi.GenerateUniqueSandikNo(istenen, kullanilanNolar);
        return new OtomatikSahaHedefSandigi(null, no, !string.Equals(no, istenen, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SevkEdilmisMi(Proje proje) =>
        proje.DurumId is (int)ProjeDurum.SevkEdildi or (int)ProjeDurum.EksikSevkEdildi;
}
