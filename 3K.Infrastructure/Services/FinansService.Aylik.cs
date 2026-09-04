using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<IReadOnlyList<FinansAylikIsModel>> AylikOzetAsync(
            int yil,
            int ay,
            CancellationToken cancellationToken)
        {
            var (baslangic, bitis) = AylikDonem(yil, ay);
            var kayitlar = await AylikKayitlariFiltrele(IsKaydiDetayQuery(), baslangic, bitis)
                .OrderBy(x => x.UretimTarihi)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
            var sablonAdlari = await _context.Set<AmbalajIcSandikSablonu>()
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
                _context.Set<FinansIsKaydi>().AsNoTracking(), baslangic, bitis);
            var filtered = ApplyFilter(periodQuery, filtreNoDate);

            var units = BuildMonthlyPageUnitsQuery(filtered);
            var totalCount = await units.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(
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
            if (selectedUnits.Count > 0)
            {
                // Arama proje birimini seçer; seçilen projenin aynı filtrelere uyan
                // ana/alt satırları birlikte döner ve farklı sayfalara bölünmez.
                var detailFilter = filtreNoDate with { Arama = null };
                var detailQuery = ApplyFilter(
                        AylikKayitlariFiltrele(IsKaydiDetayQuery(), baslangic, bitis),
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
                items = AylikModelleriOlustur(pageRecords, templateNames);
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
                FinansOzeti = financeSummary,
                GrupToplamlari = groupTotals,
                PageNumber = page,
                PageSize = size,
                TotalCount = totalCount
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
            var activeStructured = ApplyFilter(
                periodQuery,
                filter with { Arama = null, IptalEdilenleriDahilEt = false });
            if (string.IsNullOrWhiteSpace(filter.Arama))
                return activeStructured;

            var directMatches = ApplyFilter(periodQuery, filter);
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
            if (yil is < 2000 or > 2100 || ay is < 1 or > 12)
                throw new ArgumentOutOfRangeException(nameof(ay), "Yıl 2000-2100, ay 1-12 aralığında olmalıdır.");
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

            return await _context.Set<AmbalajIcSandikSablonu>()
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
                         Boy = OzelSandikTuru(x.IsTuru) ? x.Boy : null,
                         En = OzelSandikTuru(x.IsTuru) ? x.En : null,
                         Yukseklik = OzelSandikTuru(x.IsTuru) ? x.Yukseklik : null,
                         IcSandikSablonId = x.IsTuru == FinansIsTuru.IcSandik ? x.IcSandikSablonId : null,
                         x.FinansUrunId,
                         x.FiyatlandirmaBirimiSnapshot,
                         x.BirimFiyatSnapshot,
                         x.ParaBirimiSnapshot,
                         x.KdvOraniSnapshot
                     }))
            {
                var satirlar = grup.ToList();
                var tarife = satirlar.OrderBy(x => x.Id).First();
                var projeNo = tarife.ProjeNo;
                var miktar = Miktar(tarife.FiyatlandirmaBirimiSnapshot, satirlar);
                if (grup.Key.IsTuru == FinansIsTuru.AnaAmbalaj)
                    miktar = Para(miktar);

                var siparisler = satirlar.SelectMany(x => x.SiparisKalemleri)
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .ToList();
                var faturalar = siparisler.SelectMany(x => x.FaturaKalemleri)
                    .Where(x => !x.FinansFatura.IptalEdildi)
                    .ToList();
                var siparisMiktari = Math.Min(miktar, siparisler.Sum(SiparisMiktari));
                var faturalananMiktar = Math.Min(siparisMiktari, faturalar.Sum(FaturaMiktari));
                if (grup.Key.IsTuru == FinansIsTuru.AnaAmbalaj)
                {
                    siparisMiktari = Para(siparisMiktari);
                    faturalananMiktar = Para(faturalananMiktar);
                }

                var net = Para(miktar * tarife.BirimFiyatSnapshot);
                var kdv = Para(net * tarife.KdvOraniSnapshot / 100m);
                var isAdi = IsAdi(
                    grup.Key.IsTuru,
                    grup.Key.SandikTipi,
                    grup.Key.Boy,
                    grup.Key.En,
                    grup.Key.Yukseklik,
                    grup.Key.IcSandikSablonId,
                    sablonAdlari);
                sonuc.Add(new FinansAylikIsModel(
                    "Proje", null, grup.Key.ProjeId,
                    ProjectUnitKey(grup.Key.ProjeId, projeNo, tarife.Musteri), tarife.Musteri,
                    grup.Key.IsTuru, IsGrubu(grup.Key.IsTuru),
                    projeNo, isAdi, grup.Key.SandikTipi, grup.Key.Boy, grup.Key.En,
                    grup.Key.Yukseklik, satirlar.Min(x => x.UretimTarihi), satirlar.Max(x => x.UretimTarihi),
                    grup.Key.IsTuru == FinansIsTuru.SarfKereste ? 0m : satirlar.Sum(x => x.Adet),
                    miktar, Birim(tarife.FiyatlandirmaBirimiSnapshot), tarife.BirimFiyatSnapshot,
                    tarife.KdvOraniSnapshot, net, kdv, net + kdv, tarife.ParaBirimiSnapshot,
                    siparisMiktari, faturalananMiktar,
                    Para(siparisler.Sum(x => x.ToplamTutarSnapshot)),
                    Para(faturalar.Sum(x => x.ToplamTutarSnapshot)),
                    satirlar.Select(x => x.Id).ToArray(),
                    siparisler.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                    faturalar.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                    Durum(miktar, siparisMiktari, faturalananMiktar),
                    false, false, satirlar.All(x => x.IptalEdildi),
                    satirlar.Select(x => x.IptalAciklamasi).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))));
            }

            foreach (var kayit in kayitlar.Where(x => x.IsTuru == FinansIsTuru.OzelIs))
            {
                var siparisler = kayit.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
                var faturalar = siparisler.SelectMany(x => x.FaturaKalemleri)
                    .Where(x => !x.FinansFatura.IptalEdildi).ToList();
                var miktar = kayit.FiyatlandirmaBirimiSnapshot switch
                {
                    FinansFiyatlandirmaBirimi.Metrekup => kayit.ToplamM3,
                    FinansFiyatlandirmaBirimi.SabitTutar => 1m,
                    _ => kayit.Adet
                };
                var siparisMiktari = Math.Min(miktar, siparisler.Sum(SiparisMiktari));
                var faturalananMiktar = Math.Min(siparisMiktari, faturalar.Sum(FaturaMiktari));
                var net = Para(miktar * kayit.BirimFiyatSnapshot);
                var kdv = Para(net * kayit.KdvOraniSnapshot / 100m);
                var miktarBekliyor = kayit.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenTutar
                    ? kayit.BirimFiyatSnapshot <= 0
                    : miktar <= 0;
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
                    Para(siparisler.Sum(x => x.ToplamTutarSnapshot)),
                    Para(faturalar.Sum(x => x.ToplamTutarSnapshot)), [kayit.Id],
                    siparisler.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                    faturalar.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                    kayit.IptalEdildi ? "İptal" : miktarBekliyor ? "Miktar Bekliyor" : Durum(miktar, siparisMiktari, faturalananMiktar),
                    !kayit.IptalEdildi && siparisler.Count == 0 && kayit.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenAdet,
                    !kayit.IptalEdildi && siparisler.Count == 0 && kayit.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenTutar,
                    kayit.IptalEdildi, kayit.IptalAciklamasi));
            }

            return sonuc.OrderBy(x => x.IsGrubu).ThenBy(x => x.ProjeNo).ThenBy(x => x.IsAdi).ToArray();
        }

        private async Task<IReadOnlyList<FinansAylikGrupToplamiModel>> AylikIsToplamlariAsync(
            IQueryable<FinansIsKaydi> query,
            CancellationToken cancellationToken)
        {
            var regular = await query
                .Where(x => x.IsTuru != FinansIsTuru.OzelIs)
                .GroupBy(x => new
                {
                    x.ProjeId,
                    ProjeNo = x.ProjeId.HasValue ? null : x.ProjeNo,
                    Musteri = x.ProjeId.HasValue ? null : x.Musteri,
                    x.IsTuru,
                    SandikTipi = x.IsTuru == FinansIsTuru.IlaveSandik ||
                                  x.IsTuru == FinansIsTuru.IcSandik ||
                                  x.IsTuru == FinansIsTuru.SahaSandigi ||
                                  x.IsTuru == FinansIsTuru.YedekSandik
                        ? x.SandikTipi
                        : null,
                    Boy = x.IsTuru == FinansIsTuru.IlaveSandik ||
                          x.IsTuru == FinansIsTuru.IcSandik ||
                          x.IsTuru == FinansIsTuru.SahaSandigi ||
                          x.IsTuru == FinansIsTuru.YedekSandik
                        ? x.Boy
                        : null,
                    En = x.IsTuru == FinansIsTuru.IlaveSandik ||
                         x.IsTuru == FinansIsTuru.IcSandik ||
                         x.IsTuru == FinansIsTuru.SahaSandigi ||
                         x.IsTuru == FinansIsTuru.YedekSandik
                        ? x.En
                        : null,
                    Yukseklik = x.IsTuru == FinansIsTuru.IlaveSandik ||
                                x.IsTuru == FinansIsTuru.IcSandik ||
                                x.IsTuru == FinansIsTuru.SahaSandigi ||
                                x.IsTuru == FinansIsTuru.YedekSandik
                        ? x.Yukseklik
                        : null,
                    IcSandikSablonId = x.IsTuru == FinansIsTuru.IcSandik ? x.IcSandikSablonId : null,
                    x.FinansUrunId,
                    x.FiyatlandirmaBirimiSnapshot,
                    x.BirimFiyatSnapshot,
                    x.ParaBirimiSnapshot,
                    x.KdvOraniSnapshot
                })
                .Select(x => new FinansAylikHesapGrubu
                {
                    OzelIs = false,
                    IsTuru = x.Key.IsTuru,
                    RaporGrubu = null,
                    FiyatlandirmaBirimi = x.Key.FiyatlandirmaBirimiSnapshot,
                    BirimFiyat = x.Key.BirimFiyatSnapshot,
                    ParaBirimi = x.Key.ParaBirimiSnapshot,
                    KdvOrani = x.Key.KdvOraniSnapshot,
                    Adet = x.Sum(y => y.Adet),
                    ToplamM3 = x.Sum(y => y.ToplamM3)
                })
                .ToListAsync(cancellationToken);
            var special = await query
                .Where(x => x.IsTuru == FinansIsTuru.OzelIs)
                .Select(x => new FinansAylikHesapGrubu
                {
                    OzelIs = true,
                    IsTuru = x.IsTuru,
                    RaporGrubu = x.RaporGrubu,
                    FiyatlandirmaBirimi = x.FiyatlandirmaBirimiSnapshot,
                    BirimFiyat = x.BirimFiyatSnapshot,
                    ParaBirimi = x.ParaBirimiSnapshot,
                    KdvOrani = x.KdvOraniSnapshot,
                    Adet = x.Adet,
                    ToplamM3 = x.ToplamM3
                })
                .ToListAsync(cancellationToken);

            return regular.Concat(special)
                .Select(x =>
                {
                    var quantity = x.FiyatlandirmaBirimi switch
                    {
                        FinansFiyatlandirmaBirimi.Adet => x.Adet,
                        FinansFiyatlandirmaBirimi.SabitTutar => 1m,
                        _ => x.ToplamM3
                    };
                    if (!x.OzelIs && x.IsTuru == FinansIsTuru.AnaAmbalaj)
                        quantity = Para(quantity);
                    var net = Para(quantity * x.BirimFiyat);
                    var vat = Para(net * x.KdvOrani / 100m);
                    var group = x.OzelIs
                        ? x.RaporGrubu is "Kira" or "Sevkiyat" ? "Sabit İşler" : "Ekstra İşler"
                        : "Ana Ambalaj";
                    return new FinansAylikGrupToplamiModel(group, x.ParaBirimi, net, vat, net + vat);
                })
                .GroupBy(x => new { x.Grup, x.ParaBirimi })
                .Select(x => new FinansAylikGrupToplamiModel(
                    x.Key.Grup,
                    x.Key.ParaBirimi,
                    Para(x.Sum(y => y.NetTutar)),
                    Para(x.Sum(y => y.KdvTutari)),
                    Para(x.Sum(y => y.ToplamTutar))))
                .OrderBy(x => x.Grup)
                .ThenBy(x => x.ParaBirimi)
                .ToArray();
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
                    Toplam = x.Sum(y => y.ToplamTutarSnapshot)
                })
                .ToListAsync(cancellationToken);
            var invoiceTotals = await activeMonthQuery
                .SelectMany(x => x.SiparisKalemleri.Where(y => !y.FinansSiparis.IptalEdildi))
                .SelectMany(
                    x => x.FaturaKalemleri.Where(y => !y.FinansFatura.IptalEdildi),
                    (orderLine, invoiceLine) => new
                    {
                        orderLine.ParaBirimiSnapshot,
                        invoiceLine.ToplamTutarSnapshot
                    })
                .GroupBy(x => x.ParaBirimiSnapshot)
                .Select(x => new FinansAylikTutarProjection
                {
                    ParaBirimi = x.Key,
                    Toplam = x.Sum(y => y.ToplamTutarSnapshot)
                })
                .ToListAsync(cancellationToken);
            var expenseTotals = await _context.Set<FinansGider>()
                .AsNoTracking()
                .Where(x => !x.IptalEdildi && x.Tarih >= baslangic && x.Tarih < bitis)
                .GroupBy(x => x.ParaBirimi)
                .Select(x => new FinansAylikTutarProjection
                {
                    ParaBirimi = x.Key,
                    Toplam = x.Sum(y => y.ToplamTutar)
                })
                .ToListAsync(cancellationToken);

            var workByCurrency = workTotals
                .GroupBy(x => x.ParaBirimi)
                .ToDictionary(x => x.Key, x => Para(x.Sum(y => y.ToplamTutar)), StringComparer.OrdinalIgnoreCase);
            var orderByCurrency = orderTotals.ToDictionary(x => x.ParaBirimi, x => Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
            var invoiceByCurrency = invoiceTotals.ToDictionary(x => x.ParaBirimi, x => Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
            var expenseByCurrency = expenseTotals.ToDictionary(x => x.ParaBirimi, x => Para(x.Toplam), StringComparer.OrdinalIgnoreCase);
            var currencies = new[] { "TRY", "EUR", "USD" }
                .Concat(workByCurrency.Keys)
                .Concat(orderByCurrency.Keys)
                .Concat(invoiceByCurrency.Keys)
                .Concat(expenseByCurrency.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x == "TRY" ? 0 : x == "EUR" ? 1 : x == "USD" ? 2 : 3)
                .ThenBy(x => x)
                .ToArray();

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
                    Para(invoiced - expense));
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
                x.UretimTarihi >= baslangic &&
                x.UretimTarihi < bitis &&
                (x.IsTuru == FinansIsTuru.OzelIs || (x.KaynakAktif && !x.IptalEdildi)));

        private static bool OzelSandikTuru(FinansIsTuru tur) => tur is
            FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or
            FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik;

        private static decimal Miktar(FinansFiyatlandirmaBirimi birim, IReadOnlyCollection<FinansIsKaydi> kayitlar) => birim switch
        {
            FinansFiyatlandirmaBirimi.Adet => kayitlar.Sum(x => x.Adet),
            FinansFiyatlandirmaBirimi.SabitTutar => 1m,
            _ => kayitlar.Sum(x => x.ToplamM3)
        };

        private static decimal SiparisMiktari(FinansSiparisKalemi satir) => satir.FiyatlandirmaBirimiSnapshot switch
        {
            FinansFiyatlandirmaBirimi.Adet => satir.Adet,
            FinansFiyatlandirmaBirimi.SabitTutar => 1m,
            _ => satir.M3
        };

        private static decimal FaturaMiktari(FinansFaturaKalemi satir) => satir.FinansSiparisKalemi.FiyatlandirmaBirimiSnapshot switch
        {
            FinansFiyatlandirmaBirimi.Adet => satir.Adet,
            FinansFiyatlandirmaBirimi.SabitTutar => 1m,
            _ => satir.M3
        };

        private static string Birim(FinansFiyatlandirmaBirimi birim) => birim switch
        {
            FinansFiyatlandirmaBirimi.Adet => "Adet",
            FinansFiyatlandirmaBirimi.SabitTutar => "Hizmet",
            _ => "m³"
        };

        private static string Durum(decimal miktar, decimal siparisMiktari, decimal faturalananMiktar)
        {
            if (miktar <= Tolerance) return "Miktar Bekliyor";
            if (siparisMiktari <= Tolerance) return "Sipariş Bekliyor";
            if (miktar - siparisMiktari > Tolerance) return "Kısmi Sipariş";
            return siparisMiktari - faturalananMiktar > Tolerance ? "Fatura Bekliyor" : "Tamamlandı";
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
            decimal? boy,
            decimal? en,
            decimal? yukseklik,
            int? sablonId,
            IReadOnlyDictionary<int, string> sablonlar)
        {
            var ad = tur == FinansIsTuru.IcSandik && sablonId.HasValue
                ? sablonlar.GetValueOrDefault(sablonId.Value, "Silinmiş İç Sandık Tipi")
                : tur is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik && !string.IsNullOrWhiteSpace(sandikTipi)
                    ? $"{IsGrubu(tur)} · {sandikTipi}"
                    : IsGrubu(tur);
            return sandikTipi == "Katlanır Sandık"
                ? $"{ad} · {boy:0.##}×{en:0.##}×{yukseklik:0.##} mm"
                : ad;
        }

        private static decimal Para(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

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

        private sealed class FinansAylikHesapGrubu
        {
            public bool OzelIs { get; set; }
            public FinansIsTuru IsTuru { get; set; }
            public string? RaporGrubu { get; set; }
            public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; set; }
            public decimal BirimFiyat { get; set; }
            public string ParaBirimi { get; set; } = string.Empty;
            public decimal KdvOrani { get; set; }
            public decimal Adet { get; set; }
            public decimal ToplamM3 { get; set; }
        }

        private sealed class FinansAylikTutarProjection
        {
            public string ParaBirimi { get; set; } = string.Empty;
            public decimal Toplam { get; set; }
        }
    }
}
