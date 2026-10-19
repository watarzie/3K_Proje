using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Aylık iş özeti: proje birimleriyle sayfalanan ekran listesi, grup toplamları, dönem finans özeti ve raporlar için tam liste.</summary>
internal sealed class FinansAylikOzetService(AppDbContext context, FinansPoOnayOkuyucu poOnay) : IFinansAylikOzetService
{
    public async Task<IReadOnlyList<FinansAylikIsModel>> AylikOzetAsync(
        int yil,
        int ay,
        CancellationToken cancellationToken)
    {
        var (baslangic, bitis) = AylikDonem(yil, ay);
        if (await AylikKayitlariFiltrele(context.Set<FinansIsKaydi>(), baslangic, bitis).CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Aylık rapor 20.000 işi aşamaz; filtreli rapor kullanın.");
        var kayitlar = await AylikKayitlariFiltrele(context.IsKaydiDetayQuery(), baslangic, bitis)
            .OrderBy(x => x.UretimTarihi)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var sablonAdlari = await context.Set<AmbalajIcSandikSablonu>()
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ad, cancellationToken);

        return AylikModelleriOlustur(kayitlar, sablonAdlari);
    }

    public async Task<FinansAylikSayfaliSonuc> AylikOzetSayfaliAsync(
        int yil,
        int ay,
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var (baslangic, bitis) = AylikDonem(yil, ay);
        var filtreNoDate = filtre with { Baslangic = null, Bitis = null };
        var periodQuery = AylikKayitlariFiltrele(
            context.Set<FinansIsKaydi>().AsNoTracking(), baslangic, bitis);
        var filtered = FinansSorgulari.ApplyFilter(periodQuery, filtreNoDate);

        var units = BuildMonthlyPageUnitsQuery(filtered);
        var totalCount = await units.CountAsync(cancellationToken);
        var (page, size, skip) = FinansSorgulari.NormalizePagination(
            filtre.PageNumber, filtre.PageSize, totalCount, 100);
        var selectedUnits = await units
            .OrderBy(x => x.Sira)
            .ThenBy(x => x.IsGrubu)
            .ThenBy(x => x.ProjeNo)
            .ThenBy(x => x.ProjeId)
            .ThenBy(x => x.IsAdi)
            .ThenBy(x => x.OzelIsId)
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        var selectedSpecialIds = selectedUnits
            .Where(x => x.OzelIsId.HasValue)
            .Select(x => x.OzelIsId!.Value)
            .ToArray();
        var selectedProjectIds = selectedUnits
            .Where(x => !x.OzelIsId.HasValue && x.ProjeId.HasValue)
            .Select(x => x.ProjeId!.Value)
            .Distinct()
            .ToArray();
        var selectedManualKeys = selectedUnits
            .Where(x => !x.OzelIsId.HasValue && !x.ProjeId.HasValue)
            .Select(x => ManualProjectKey(x.ProjeNo, x.Musteri))
            .Distinct()
            .ToArray();

        IReadOnlyList<FinansAylikIsModel> items = Array.Empty<FinansAylikIsModel>();
        IReadOnlyList<FinansAylikProjeOzetiModel> projects = Array.Empty<FinansAylikProjeOzetiModel>();
        IReadOnlyList<FinansAylikIsModel> projectSpecialWorks = Array.Empty<FinansAylikIsModel>();
        if (selectedProjectIds.Length > 0 || selectedManualKeys.Length > 0)
        {
            var projectRecords = await AylikKayitlariFiltrele(context.IsKaydiDetayQuery(), baslangic, bitis)
                .Where(x => x.IsTuru != FinansIsTuru.OzelIs && x.KaynakAktif && !x.IptalEdildi &&
                            ((x.ProjeId.HasValue && selectedProjectIds.Contains(x.ProjeId.Value)) ||
                             (!x.ProjeId.HasValue && selectedManualKeys.Contains(x.ProjeNo + "\u001f" + x.Musteri))))
                .ToListAsync(cancellationToken);
            projects = ProjeOzetleriniOlustur(projectRecords);

            // Projeye bağlı özel/düzenli işler proje bloğunun altında da gösterilir; sayfalama birimleri
            // değişmediği için bu iş başka bir sayfada kendi satırıyla da yer alabilir.
            var projectSpecialRecords = await FinansSorgulari.ApplyFilter(
                    AylikKayitlariFiltrele(context.IsKaydiDetayQuery(), baslangic, bitis),
                    filtreNoDate with { Arama = null })
                .Where(x => x.IsTuru == FinansIsTuru.OzelIs &&
                            ((x.ProjeId.HasValue && selectedProjectIds.Contains(x.ProjeId.Value)) ||
                             (!x.ProjeId.HasValue && x.ProjeNo != string.Empty &&
                              selectedManualKeys.Contains(x.ProjeNo + "\u001f" + x.Musteri))))
                .OrderBy(x => x.UretimTarihi)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
            projectSpecialWorks = AylikModelleriOlustur(projectSpecialRecords, new Dictionary<int, string>());
        }
        if (selectedUnits.Count > 0)
        {
            // Arama proje birimini seçer; seçilen projenin aynı filtrelere uyan
            // ana/alt satırları birlikte döner ve farklı sayfalara bölünmez.
            var detailFilter = filtreNoDate with { Arama = null };
            var detailQuery = FinansSorgulari.ApplyFilter(
                    AylikKayitlariFiltrele(context.IsKaydiDetayQuery(), baslangic, bitis),
                    detailFilter)
                .Where(x =>
                    (x.IsTuru == FinansIsTuru.OzelIs && selectedSpecialIds.Contains(x.Id)) ||
                    (x.IsTuru != FinansIsTuru.OzelIs &&
                     ((x.ProjeId.HasValue && selectedProjectIds.Contains(x.ProjeId.Value)) ||
                      (!x.ProjeId.HasValue && selectedManualKeys.Contains(x.ProjeNo + "\u001f" + x.Musteri)))));
            var pageRecords = await detailQuery
                .OrderBy(x => x.UretimTarihi)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
            var templateNames = await TemplateNamesAsync(pageRecords, cancellationToken);
            items = await poOnay.AylikSatirlaraEkleAsync(
                AylikModelleriOlustur(pageRecords, templateNames), pageRecords, cancellationToken);
        }

        // Üst finans özeti arama ve "iptalleri göster" seçiminden bağımsızdır;
        // mevcut ekran semantiği gibi ayın tüm aktif kayıtlarını temsil eder.
        var activeMonthQuery = periodQuery.Where(x => x.KaynakAktif && !x.IptalEdildi);
        var activeGroupTotals = await AylikIsToplamlariAsync(activeMonthQuery, cancellationToken);
        var financeSummary = await AylikFinansOzetiAsync(
            activeMonthQuery, activeGroupTotals, baslangic, bitis, cancellationToken);

        // Grup toplamları sayfadaki satırlardan değil, aktif ve tam filtreli
        // kayıt kümesinden hesaplanır.
        var groupTotals = HasMonthlyGroupFilter(filtreNoDate)
            ? await AylikIsToplamlariAsync(
                BuildMonthlyExpandedFilterQuery(periodQuery, filtreNoDate),
                cancellationToken)
            : activeGroupTotals;

        return new FinansAylikSayfaliSonuc
        {
            Items = items,
            Projeler = projects,
            ProjeOzelIsleri = projectSpecialWorks,
            FinansOzeti = financeSummary,
            GrupToplamlari = groupTotals,
            PoOnayKuraliAcik = await poOnay.KuralAcikMiAsync(cancellationToken),
            PageNumber = page,
            PageSize = size,
            TotalCount = totalCount
        };
    }

    public async Task<FinansProjeSandikKirilimiModel> ProjeSandikKirilimiAsync(
        FinansProjeSandikKirilimiIstegi istek,
        CancellationToken cancellationToken)
    {
        var projeNo = istek.ProjeNo?.Trim() ?? string.Empty;
        var musteri = istek.Musteri?.Trim();
        if (istek.ProjeId is not > 0 && projeNo.Length == 0)
            throw IsKuraliException.Cakisma("Sandık kırılımı için proje seçilmelidir.");

        // Kırılım üretim kayıtlarıdır; projeye bağlı özel/düzenli işler sandık taşımaz.
        var query = context.Set<FinansIsKaydi>().AsNoTracking().Where(x => x.IsTuru != FinansIsTuru.OzelIs);
        query = istek.ProjeId is > 0
            ? query.Where(x => x.ProjeId == istek.ProjeId)
            : string.IsNullOrEmpty(musteri)
                ? query.Where(x => x.ProjeId == null && x.ProjeNo == projeNo)
                : query.Where(x => x.ProjeId == null && x.ProjeNo == projeNo && x.Musteri == musteri);
        if (istek.Yil.HasValue && istek.Ay.HasValue)
        {
            var (baslangic, bitis) = AylikDonem(istek.Yil.Value, istek.Ay.Value);
            query = query.Where(x => x.FinansDonemi >= baslangic && x.FinansDonemi < bitis);
        }
        query = FinansSorgulari.ApplyFilter(query, istek.Filtre with { ProjeId = null, ProjeNo = null });

        if (await query.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Sandık kırılımı 20.000 kaydı aşamaz; dönem veya filtre seçin.");

        // Tutarlar veritabanında hesaplanır; hacim payı fiyat satırlarına (JSON) bağlı olduğundan kayıt kuralıyla bulunur.
        var satirlar = await query
            .Select(FinansSorgulari.IsTutarlariIle((x, net, siparis, fatura) => new KirilimSorguSatiri
            {
                Id = x.Id,
                ProjeId = x.ProjeId,
                ProjeNo = x.ProjeNo,
                Musteri = x.Musteri,
                IsTuru = x.IsTuru,
                KaynakBileseni = x.KaynakBileseni,
                KaynakKayitId = x.KaynakKayitId,
                SandikNo = x.SandikNo,
                SandikAdi = x.SandikAdi,
                SandikTipi = x.SandikTipi,
                SandikCinsi = x.SandikCinsi,
                IsAdi = x.IsAdi,
                FinansTarihi = x.FinansTarihi,
                Adet = x.Adet,
                ToplamM3 = x.ToplamM3,
                AhsapM3 = x.AhsapM3,
                KontraM3 = x.KontraM3,
                FiyatBilesenleriJson = x.FiyatBilesenleriJson,
                ParaBirimi = x.ParaBirimiSnapshot,
                Net = net,
                Siparis = siparis,
                Fatura = fatura,
                Aktif = !x.IptalEdildi && x.KaynakAktif
            }))
            .ToListAsync(cancellationToken);

        var idler = query.Select(x => x.Id);
        var poNumaralari = (await context.Set<FinansSiparisKalemi>().AsNoTracking()
                .Where(x => !x.FinansSiparis.IptalEdildi && idler.Contains(x.FinansIsKaydiId))
                .Select(x => new { x.FinansIsKaydiId, x.FinansSiparis.PoNumarasi })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(x => x.FinansIsKaydiId, x => x.PoNumarasi);
        var faturaNumaralari = (await context.Set<FinansFaturaKalemi>().AsNoTracking()
                .Where(x => !x.FinansFatura.IptalEdildi &&
                            !x.FinansSiparisKalemi.FinansSiparis.IptalEdildi &&
                            idler.Contains(x.FinansSiparisKalemi.FinansIsKaydiId))
                .Select(x => new { x.FinansSiparisKalemi.FinansIsKaydiId, x.FinansFatura.FaturaNumarasi })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(x => x.FinansIsKaydiId, x => x.FaturaNumarasi);

        var kayitlar = satirlar.Select(x =>
        {
            var hacim = FinansHacimKurallari.Hacim(x.AhsapM3, x.KontraM3, x.SandikCinsi, x.SandikTipi, x.ToplamM3,
                FinansModelEslemeleri.Bilesenler(x.FiyatBilesenleriJson));
            return new FinansSandikKirilimKaydi(
                x.Id, x.IsTuru, x.KaynakBileseni, x.KaynakKayitId, x.SandikNo, x.SandikAdi, x.SandikTipi, x.SandikCinsi,
                x.IsAdi, x.FinansTarihi, x.Adet, x.ToplamM3, hacim.Ahsap, hacim.Kontra, x.ParaBirimi,
                x.Net, x.Siparis, x.Fatura, x.Aktif,
                poNumaralari[x.Id].Order(StringComparer.Ordinal).ToArray(),
                faturaNumaralari[x.Id].Order(StringComparer.Ordinal).ToArray());
        }).ToList();

        var ilk = satirlar.OrderBy(x => x.Id).FirstOrDefault();
        return new FinansProjeSandikKirilimiModel
        {
            ProjeId = istek.ProjeId is > 0 ? istek.ProjeId : null,
            ProjeNo = ilk?.ProjeNo ?? projeNo,
            Musteri = ilk?.Musteri ?? musteri ?? string.Empty,
            KayitSayisi = kayitlar.Count,
            Sandiklar = FinansSandikKirilimiKurali.Olustur(kayitlar)
        };
    }

    internal static IQueryable<FinansAylikSayfaBirimi> BuildMonthlyPageUnitsQuery(
        IQueryable<FinansIsKaydi> filtered) =>
        filtered
            .Select(x => new FinansAylikSayfaBirimi
            {
                OzelIsId = x.IsTuru == FinansIsTuru.OzelIs ? x.Id : null,
                ProjeId = x.ProjeId,
                ProjeNo = x.IsTuru != FinansIsTuru.OzelIs && x.ProjeId.HasValue
                    ? string.Empty
                    : x.ProjeNo,
                Musteri = x.IsTuru != FinansIsTuru.OzelIs && x.ProjeId.HasValue
                    ? string.Empty
                    : x.Musteri,
                IsGrubu = x.IsTuru == FinansIsTuru.OzelIs ? x.RaporGrubu ?? "Özel İş" : "Ana Ambalaj",
                IsAdi = x.IsTuru == FinansIsTuru.OzelIs
                    ? x.IsAdi
                    : x.ProjeId.HasValue ? string.Empty : x.ProjeNo,
                Sira = x.IsTuru != FinansIsTuru.OzelIs
                    ? 1
                    : x.RaporGrubu == "Kira" || x.RaporGrubu == "Sevkiyat" ? 0 : 2
            })
            .Distinct();

    internal static IQueryable<FinansIsKaydi> BuildMonthlyExpandedFilterQuery(
        IQueryable<FinansIsKaydi> periodQuery,
        FinansListeFiltre filter)
    {
        var activeStructured = FinansSorgulari.ApplyFilter(
            periodQuery,
            filter with { Arama = null, IptalEdilenleriDahilEt = false });
        if (string.IsNullOrWhiteSpace(filter.Arama))
            return activeStructured;

        var directMatches = FinansSorgulari.ApplyFilter(periodQuery, filter);
        return activeStructured.Where(record =>
            record.IsTuru == FinansIsTuru.OzelIs
                ? directMatches.Any(match =>
                    match.IsTuru == FinansIsTuru.OzelIs && match.Id == record.Id)
                : record.ProjeId.HasValue
                    ? directMatches.Any(match =>
                        match.IsTuru != FinansIsTuru.OzelIs &&
                        match.ProjeId == record.ProjeId)
                    : directMatches.Any(match =>
                        match.IsTuru != FinansIsTuru.OzelIs &&
                        !match.ProjeId.HasValue &&
                        match.ProjeNo == record.ProjeNo &&
                        match.Musteri == record.Musteri));
    }

    private static (DateTime Start, DateTime End) AylikDonem(int yil, int ay)
    {
        if (!FinansTarihKurallari.GecerliDonem(yil, ay))
            throw IsKuraliException.Cakisma("Geçerli bir yıl ve ay seçilmelidir.");
        var start = new DateTime(yil, ay, 1);
        return (start, start.AddMonths(1));
    }

    private async Task<IReadOnlyDictionary<int, string>> TemplateNamesAsync(
        IReadOnlyCollection<FinansIsKaydi> records,
        CancellationToken cancellationToken)
    {
        var ids = records
            .Where(x => x.IcSandikSablonId.HasValue)
            .Select(x => x.IcSandikSablonId!.Value)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, string>();

        return await context.Set<AmbalajIcSandikSablonu>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ad, cancellationToken);
    }

    private static string ManualProjectKey(string projectNo, string customer)
        => projectNo + "\u001f" + customer;

    internal static IReadOnlyList<FinansAylikIsModel> AylikModelleriOlustur(
        IReadOnlyCollection<FinansIsKaydi> kayitlar,
        IReadOnlyDictionary<int, string> sablonAdlari)
    {

        var sonuc = new List<FinansAylikIsModel>();
        var projeKayitlari = kayitlar.Where(x => x.IsTuru != FinansIsTuru.OzelIs).ToList();
        foreach (var grup in projeKayitlari.GroupBy(x => new
                 {
                     x.ProjeId,
                     ProjeNo = x.ProjeId.HasValue ? null : x.ProjeNo,
                     Musteri = x.ProjeId.HasValue ? null : x.Musteri,
                     x.IsTuru,
                     SandikTipi = OzelSandikTuru(x.IsTuru) ? x.SandikTipi : null,
                     // Katlanır sandık boyutla değil, kontrplak gibi ahşap/kontra m³ ile fiyatlanır; satır boyuta bölünmez.
                     IcSandikSablonId = x.IsTuru == FinansIsTuru.IcSandik ? x.IcSandikSablonId : null,
                     x.FinansUrunId,
                     x.FiyatlandirmaBirimiSnapshot,
                     x.BirimFiyatSnapshot,
                     x.AhsapBirimFiyatSnapshot,
                     x.ParaBirimiSnapshot,
                     x.KdvOraniSnapshot
                 }))
        {
            var satirlar = grup.ToList();
            var tarife = satirlar.OrderBy(x => x.Id).First();
            var projeNo = tarife.ProjeNo;
            // Miktar, net tutarın fiyatlandığı kayıt m³'lerinin yuvarlanmamış toplamıdır. Net kayıt başına hesaplanır
            // (PO kalemi de kayıt başınadır); miktarı ayrıca yuvarlamak satırı Ambalaj m³'ünden ve netten ayırır.
            var miktar = Miktar(tarife.FiyatlandirmaBirimiSnapshot, satirlar);

            var siparisler = satirlar.SelectMany(x => x.SiparisKalemleri)
                .Where(x => !x.FinansSiparis.IptalEdildi)
                .ToList();
            var faturalar = siparisler.SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi)
                .ToList();
            var siparisMiktari = Math.Min(miktar, siparisler.Sum(SiparisMiktari));
            var faturalananMiktar = Math.Min(siparisMiktari, faturalar.Sum(FaturaMiktari));

            var net = satirlar.Sum(FinansTutarKurallari.IsNet);
            var kdv = satirlar.Sum(x => FinansTutarKurallari.Para(FinansTutarKurallari.IsNet(x) * x.KdvOraniSnapshot / 100m));
            var ozet = SatirOzeti(satirlar);
            var isAdi = IsAdi(
                grup.Key.IsTuru,
                grup.Key.SandikTipi,
                grup.Key.IcSandikSablonId,
                sablonAdlari);
            sonuc.Add(new FinansAylikIsModel(
                "Proje", null, grup.Key.ProjeId,
                ProjectUnitKey(grup.Key.ProjeId, projeNo, tarife.Musteri), tarife.Musteri,
                grup.Key.IsTuru, IsGrubu(grup.Key.IsTuru),
                projeNo, isAdi, grup.Key.SandikTipi, null, null,
                null, satirlar.Min(x => x.UretimTarihi), satirlar.Max(x => x.UretimTarihi),
                grup.Key.IsTuru == FinansIsTuru.SarfKereste ? 0m : satirlar.Sum(x => x.Adet),
                miktar, Birim(tarife.FiyatlandirmaBirimiSnapshot), tarife.BirimFiyatSnapshot,
                tarife.KdvOraniSnapshot, net, kdv, net + kdv, tarife.ParaBirimiSnapshot,
                siparisMiktari, faturalananMiktar,
                FinansTutarKurallari.Para(siparisler.Sum(x => x.ToplamTutarSnapshot)),
                FinansTutarKurallari.Para(faturalar.Sum(x => x.ToplamTutarSnapshot)),
                satirlar.Select(x => x.Id).ToArray(),
                siparisler.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                faturalar.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                Durum(net, siparisler.Sum(x => x.NetTutarSnapshot), faturalar.Sum(x => x.NetTutarSnapshot)),
                false, false, satirlar.All(x => x.IptalEdildi),
                satirlar.Select(x => x.IptalAciklamasi).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                FinansKategoriKurali.Kategori(grup.Key.IsTuru, null), ozet.Durum.Siparis, ozet.Durum.Fatura,
                ozet.AhsapM3, ozet.KontraM3, ozet.KalanSiparis, ozet.KalanFatura, grup.Key.AhsapBirimFiyatSnapshot));
        }

        foreach (var kayit in kayitlar.Where(x => x.IsTuru == FinansIsTuru.OzelIs))
        {
            var siparisler = kayit.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
            var faturalar = siparisler.SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var miktar = kayit.FiyatlandirmaBirimiSnapshot switch
            {
                FinansFiyatlandirmaBirimi.Metrekup => kayit.ToplamM3,
                FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
                _ => kayit.Adet
            };
            var siparisMiktari = Math.Min(miktar, siparisler.Sum(SiparisMiktari));
            var faturalananMiktar = Math.Min(siparisMiktari, faturalar.Sum(FaturaMiktari));
            var net = FinansTutarKurallari.IsNet(kayit);
            var kdv = FinansTutarKurallari.Para(net * kayit.KdvOraniSnapshot / 100m);
            var ozelOzet = SatirOzeti([kayit]);
            sonuc.Add(new FinansAylikIsModel(
                "OzelIs", kayit.Id, kayit.ProjeId,
                ProjectUnitKey(kayit.ProjeId, kayit.ProjeNo, kayit.Musteri), kayit.Musteri,
                kayit.IsTuru,
                string.IsNullOrWhiteSpace(kayit.RaporGrubu) ? "Özel İş" : kayit.RaporGrubu,
                kayit.ProjeNo,
                kayit.IsAdi, kayit.SandikTipi, kayit.Boy, kayit.En, kayit.Yukseklik,
                kayit.UretimTarihi, kayit.UretimTarihi, 0m, miktar, kayit.Birim,
                kayit.BirimFiyatSnapshot, kayit.KdvOraniSnapshot, net, kdv, net + kdv,
                kayit.ParaBirimiSnapshot, siparisMiktari, faturalananMiktar,
                FinansTutarKurallari.Para(siparisler.Sum(x => x.ToplamTutarSnapshot)),
                FinansTutarKurallari.Para(faturalar.Sum(x => x.ToplamTutarSnapshot)), [kayit.Id],
                siparisler.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                faturalar.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                kayit.IptalEdildi ? "İptal" : net <= 0 ? "Fiyatlandırma Bekliyor" : Durum(net, siparisler.Sum(x => x.NetTutarSnapshot), faturalar.Sum(x => x.NetTutarSnapshot)),
                !kayit.IptalEdildi && siparisler.Count == 0 && FinansTutarKurallari.AylikMiktarDuzenlenebilir(kayit),
                !kayit.IptalEdildi && siparisler.Count == 0 && FinansTutarKurallari.AylikBirimFiyatDuzenlenebilir(kayit),
                kayit.IptalEdildi, kayit.IptalAciklamasi,
                FinansKategoriKurali.Kategori(kayit.IsTuru, kayit.DuzenliIsId), ozelOzet.Durum.Siparis, ozelOzet.Durum.Fatura,
                ozelOzet.AhsapM3, ozelOzet.KontraM3, ozelOzet.KalanSiparis, ozelOzet.KalanFatura, kayit.AhsapBirimFiyatSnapshot));
        }

        return sonuc.OrderBy(x => x.IsGrubu).ThenBy(x => x.ProjeNo).ThenBy(x => x.IsAdi).ToArray();
    }

    /// <summary>Satırın aktif kalemlerinden birleşik durum, ahşap/kontra m³ ve kalan sipariş/fatura tutarı.</summary>
    private static (FinansDurumOzeti Durum, decimal AhsapM3, decimal KontraM3, decimal KalanSiparis, decimal KalanFatura) SatirOzeti(
        IReadOnlyCollection<FinansIsKaydi> kayitlar)
    {
        var aktif = kayitlar.Where(x => !x.IptalEdildi && x.KaynakAktif).ToList();
        var tutarlar = aktif.Select(FinansTutarKurallari.KalemTutarlari).ToList();
        var hacimler = aktif.Select(FinansModelEslemeleri.Hacim).ToList();
        return (FinansTutarKurallari.BirlesikDurum(tutarlar), hacimler.Sum(x => x.Ahsap), hacimler.Sum(x => x.Kontra),
            tutarlar.Sum(x => Math.Max(0, x.Net - x.Siparis)), tutarlar.Sum(x => Math.Max(0, x.Siparis - x.Fatura)));
    }

    /// <summary>
    /// Sayfadaki projelerin ayın bütün aktif kalemlerinden özeti. Kategori/arama filtresi yalnız satırları
    /// daraltır; proje durumu projenin tamamından türetilir (F7).
    /// </summary>
    internal static IReadOnlyList<FinansAylikProjeOzetiModel> ProjeOzetleriniOlustur(IEnumerable<FinansIsKaydi> kayitlar)
        => kayitlar
            .Where(x => x.IsTuru != FinansIsTuru.OzelIs && !x.IptalEdildi && x.KaynakAktif)
            .GroupBy(x => ProjectUnitKey(x.ProjeId, x.ProjeNo, x.Musteri))
            .Select(grup =>
            {
                var items = grup.ToList();
                var first = items[0];
                var tutarlar = items.Select(x => (x.ParaBirimiSnapshot, Tutar: FinansTutarKurallari.KalemTutarlari(x))).ToList();
                var durum = FinansTutarKurallari.BirlesikDurum(tutarlar.Select(x => x.Tutar));
                var hacimler = items.Select(FinansModelEslemeleri.Hacim).ToList();
                return new FinansAylikProjeOzetiModel(grup.Key, first.ProjeId, first.ProjeNo, first.Musteri,
                    durum.Siparis, durum.Fatura, durum.Durum, durum.GerekliKalem, durum.FiyatBekleyenKalem,
                    hacimler.Sum(x => x.Ahsap), hacimler.Sum(x => x.Kontra),
                    tutarlar.GroupBy(x => x.ParaBirimiSnapshot).OrderBy(x => x.Key)
                        .Select(x => new FinansProjeTutarModel(x.Key, x.Sum(y => y.Tutar.Net), x.Sum(y => y.Tutar.Siparis), x.Sum(y => y.Tutar.Fatura)))
                        .ToArray());
            })
            .OrderBy(x => x.ProjeNo, StringComparer.Ordinal)
            .ToArray();

    private async Task<IReadOnlyList<FinansAylikGrupToplamiModel>> AylikIsToplamlariAsync(IQueryable<FinansIsKaydi> query, CancellationToken cancellationToken)
    {
        var values = await query.Select(FinansSorgulari.IsNetIle((x, net) => new
        {
            x.IsTuru, x.RaporGrubu, x.ParaBirimiSnapshot, x.KdvOraniSnapshot, Net = net
        })).GroupBy(x => new { x.IsTuru, x.RaporGrubu, x.ParaBirimiSnapshot })
          .Select(g => new { g.Key.IsTuru, g.Key.RaporGrubu, g.Key.ParaBirimiSnapshot,
              Net = g.Sum(x => x.Net), Vat = g.Sum(x => decimal.Round(x.Net * x.KdvOraniSnapshot / 100m, 2)) })
          .ToListAsync(cancellationToken);
        return values.Select(x => new FinansAylikGrupToplamiModel(
            x.IsTuru == FinansIsTuru.OzelIs ? x.RaporGrubu is "Kira" or "Sevkiyat" ? "Sabit İşler" : "Ekstra İşler" : IsGrubu(x.IsTuru),
            x.ParaBirimiSnapshot, x.Net, x.Vat, x.Net + x.Vat)).ToArray();
    }

    private async Task<IReadOnlyList<FinansAylikFinansOzetiModel>> AylikFinansOzetiAsync(
        IQueryable<FinansIsKaydi> activeMonthQuery,
        IReadOnlyList<FinansAylikGrupToplamiModel> workTotals,
        DateTime baslangic,
        DateTime bitis,
        CancellationToken cancellationToken)
    {
        var orderTotals = await activeMonthQuery
            .SelectMany(x => x.SiparisKalemleri.Where(y => !y.FinansSiparis.IptalEdildi))
            .GroupBy(x => x.ParaBirimiSnapshot)
            .Select(x => new FinansAylikTutarProjection
            {
                ParaBirimi = x.Key,
                Toplam = x.Sum(y => y.NetTutarSnapshot)
            })
            .ToListAsync(cancellationToken);
        var invoiceTotals = await activeMonthQuery
            .SelectMany(x => x.SiparisKalemleri.Where(y => !y.FinansSiparis.IptalEdildi))
            .SelectMany(
                x => x.FaturaKalemleri.Where(y => !y.FinansFatura.IptalEdildi),
                (orderLine, invoiceLine) => new
                {
                    orderLine.ParaBirimiSnapshot,
                    invoiceLine.NetTutarSnapshot
                })
            .GroupBy(x => x.ParaBirimiSnapshot)
            .Select(x => new FinansAylikTutarProjection
            {
                ParaBirimi = x.Key,
                Toplam = x.Sum(y => y.NetTutarSnapshot)
            })
            .ToListAsync(cancellationToken);
        var expenseTotals = await context.Set<FinansGider>()
            .AsNoTracking()
            .Where(x => !x.IptalEdildi && !x.AvansMi && x.FinansDonemi >= baslangic && x.FinansDonemi < bitis)
            .GroupBy(x => x.ParaBirimi)
            .Select(x => new FinansAylikTutarProjection
            {
                ParaBirimi = x.Key,
                Toplam = x.Sum(y => y.Matrah)
            })
            .ToListAsync(cancellationToken);

        var workByCurrency = workTotals
            .GroupBy(x => x.ParaBirimi)
            .ToDictionary(x => x.Key, x => FinansTutarKurallari.Para(x.Sum(y => y.NetTutar)), StringComparer.OrdinalIgnoreCase);
        var orderByCurrency = orderTotals.ToDictionary(x => x.ParaBirimi, x => FinansTutarKurallari.Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
        var invoiceByCurrency = invoiceTotals.ToDictionary(x => x.ParaBirimi, x => FinansTutarKurallari.Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
        var expenseByCurrency = expenseTotals.ToDictionary(x => x.ParaBirimi, x => FinansTutarKurallari.Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
        var currencies = await FinansSorgulari.OzetParaBirimleriAsync(context,
            workByCurrency.Keys.Concat(orderByCurrency.Keys).Concat(invoiceByCurrency.Keys).Concat(expenseByCurrency.Keys),
            cancellationToken);

        return currencies.Select(currency =>
        {
            var total = workByCurrency.GetValueOrDefault(currency);
            var ordered = orderByCurrency.GetValueOrDefault(currency);
            var invoiced = invoiceByCurrency.GetValueOrDefault(currency);
            var expense = expenseByCurrency.GetValueOrDefault(currency);
            return new FinansAylikFinansOzetiModel(
                currency,
                total,
                ordered,
                Math.Max(0m, total - ordered),
                invoiced,
                Math.Max(0m, ordered - invoiced),
                expense,
                FinansTutarKurallari.Para(invoiced - expense));
        }).ToArray();
    }

    private static bool HasMonthlyGroupFilter(FinansListeFiltre filter) =>
        !string.IsNullOrWhiteSpace(filter.Arama) ||
        filter.ProjeId.HasValue ||
        !string.IsNullOrWhiteSpace(filter.ProjeNo) ||
        filter.IsTuru.HasValue ||
        filter.Durum.HasValue ||
        !string.IsNullOrWhiteSpace(filter.ParaBirimi) ||
        !string.IsNullOrWhiteSpace(filter.PoNumarasi) ||
        !string.IsNullOrWhiteSpace(filter.TalepEden) ||
        filter.SiparisDurumu.HasValue ||
        filter.FaturaDurumu.HasValue ||
        filter.FaturaBekleyen;

    internal static string ProjectUnitKey(int? projectId, string projectNo, string customer)
        => projectId.HasValue
            ? $"P:{projectId.Value}"
            : $"M:{projectNo}\u001f{customer}";

    internal static IQueryable<FinansIsKaydi> AylikKayitlariFiltrele(
        IQueryable<FinansIsKaydi> query,
        DateTime baslangic,
        DateTime bitis) =>
        query.Where(x =>
            x.FinansDonemi >= baslangic &&
            x.FinansDonemi < bitis &&
            (x.IsTuru == FinansIsTuru.OzelIs || (x.KaynakAktif && !x.IptalEdildi)));

    private static bool OzelSandikTuru(FinansIsTuru tur) => tur is
        FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or
        FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik;

    private static decimal Miktar(FinansFiyatlandirmaBirimi birim, IReadOnlyCollection<FinansIsKaydi> kayitlar) => birim switch
    {
        FinansFiyatlandirmaBirimi.Adet => kayitlar.Sum(x => x.Adet),
        FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
        _ => kayitlar.Sum(x => x.ToplamM3)
    };

    private static decimal SiparisMiktari(FinansSiparisKalemi satir) => satir.FiyatlandirmaBirimiSnapshot switch
    {
        FinansFiyatlandirmaBirimi.Adet => satir.Adet,
        FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
        _ => satir.M3
    };

    private static decimal FaturaMiktari(FinansFaturaKalemi satir) => satir.FinansSiparisKalemi.FiyatlandirmaBirimiSnapshot switch
    {
        FinansFiyatlandirmaBirimi.Adet => satir.Adet,
        FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
        _ => satir.M3
    };

    private static string Birim(FinansFiyatlandirmaBirimi birim) => birim switch
    {
        FinansFiyatlandirmaBirimi.Adet => "Adet",
        FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => "Hizmet",
        _ => "m³"
    };

    private static string Durum(decimal miktar, decimal siparisMiktari, decimal faturalananMiktar)
    {
        if (miktar <= FinansMiktarKurallari.Tolerance) return "Miktar Bekliyor";
        if (siparisMiktari <= FinansMiktarKurallari.Tolerance) return "Sipariş Bekliyor";
        if (miktar - siparisMiktari > FinansMiktarKurallari.Tolerance) return "Kısmi Sipariş";
        return siparisMiktari - faturalananMiktar > FinansMiktarKurallari.Tolerance ? "Fatura Bekliyor" : "Tamamlandı";
    }

    private static string IsGrubu(FinansIsTuru tur) => tur switch
    {
        FinansIsTuru.AnaAmbalaj => "Ana Ambalaj",
        FinansIsTuru.SarfKereste => "Sarf Kereste",
        FinansIsTuru.IlaveSandik => "İlave Sandık",
        FinansIsTuru.IcSandik => "İç Sandık",
        FinansIsTuru.SahaSandigi => "Saha Sandığı",
        FinansIsTuru.YedekSandik => "Yedek Sandık",
        FinansIsTuru.Tadilat => "Tadilat",
        FinansIsTuru.DigerAmbalaj => "Diğer Ambalaj İşi",
        _ => "Özel İş"
    };

    private static string IsAdi(
        FinansIsTuru tur,
        string? sandikTipi,
        int? sablonId,
        IReadOnlyDictionary<int, string> sablonlar)
        => tur == FinansIsTuru.IcSandik && sablonId.HasValue
            ? sablonlar.GetValueOrDefault(sablonId.Value, "Silinmiş İç Sandık Tipi")
            : tur is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik && !string.IsNullOrWhiteSpace(sandikTipi)
                ? $"{IsGrubu(tur)} · {sandikTipi}"
                : IsGrubu(tur);

    internal sealed record FinansAylikSayfaBirimi
    {
        public int? OzelIsId { get; init; }
        public int? ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public string IsGrubu { get; init; } = string.Empty;
        public string IsAdi { get; init; } = string.Empty;
        public int Sira { get; init; }
    }

    private sealed class KirilimSorguSatiri
    {
        public int Id { get; init; }
        public int? ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public FinansIsTuru IsTuru { get; init; }
        public string KaynakBileseni { get; init; } = string.Empty;
        public string? KaynakKayitId { get; init; }
        public string? SandikNo { get; init; }
        public string? SandikAdi { get; init; }
        public string? SandikTipi { get; init; }
        public AmbalajSandikCinsi? SandikCinsi { get; init; }
        public string IsAdi { get; init; } = string.Empty;
        public DateTime FinansTarihi { get; init; }
        public decimal Adet { get; init; }
        public decimal ToplamM3 { get; init; }
        public decimal? AhsapM3 { get; init; }
        public decimal? KontraM3 { get; init; }
        public string? FiyatBilesenleriJson { get; init; }
        public string ParaBirimi { get; init; } = string.Empty;
        public decimal Net { get; init; }
        public decimal Siparis { get; init; }
        public decimal Fatura { get; init; }
        public bool Aktif { get; init; }
    }

    private sealed class FinansAylikTutarProjection
    {
        public string ParaBirimi { get; set; } = string.Empty;
        public decimal Toplam { get; set; }
    }
}
