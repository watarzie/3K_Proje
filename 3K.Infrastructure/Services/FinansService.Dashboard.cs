using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        [Flags]
        private enum DashboardBolumu
        {
            Operasyon = 1,
            Gelir = 2,
            Gider = 4,
            DurumTutarlari = 8,
            Tum = Operasyon | Gelir | Gider | DurumTutarlari
        }

        public Task<FinansDashboardModel> DashboardAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(baslangic, bitis, DashboardBolumu.Tum, cancellationToken);

        public Task<FinansDashboardModel> DashboardOperasyonAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(baslangic, bitis, DashboardBolumu.Operasyon, cancellationToken);

        public Task<FinansDashboardModel> DashboardGelirAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(baslangic, bitis, DashboardBolumu.Gelir, cancellationToken);

        public Task<FinansDashboardModel> DashboardGiderAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(baslangic, bitis, DashboardBolumu.Gider, cancellationToken);

        public Task<FinansDashboardModel> DashboardNetAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(
                baslangic,
                bitis,
                DashboardBolumu.Gelir | DashboardBolumu.Gider,
                cancellationToken);

        public Task<FinansDashboardModel> DashboardDurumTutarlariAsync(
            DateTime? baslangic,
            DateTime? bitis,
            CancellationToken cancellationToken)
            => DashboardBolumuAsync(
                baslangic,
                bitis,
                DashboardBolumu.DurumTutarlari | DashboardBolumu.Gelir,
                cancellationToken);

        private async Task<FinansDashboardModel> DashboardBolumuAsync(
            DateTime? baslangic,
            DateTime? bitis,
            DashboardBolumu bolumler,
            CancellationToken cancellationToken)
        {
            var start = baslangic?.Date;
            var endExclusive = bitis?.Date.AddDays(1);
            var thisMonthStart = new DateTime(TurkeyTime.Now.Year, TurkeyTime.Now.Month, 1);
            var nextMonthStart = thisMonthStart.AddMonths(1);
            IQueryable<FinansIsKaydi>? workQuery = null;
            if (bolumler.HasFlag(DashboardBolumu.Operasyon) || bolumler.HasFlag(DashboardBolumu.DurumTutarlari))
            {
                workQuery = _context.Set<FinansIsKaydi>().AsNoTracking()
                    .Where(x => !x.IptalEdildi && x.KaynakAktif);
                if (start.HasValue) workQuery = workQuery.Where(x => x.FinansDonemi >= start.Value);
                if (endExclusive.HasValue) workQuery = workQuery.Where(x => x.FinansDonemi < endExclusive.Value);
            }

            var toplamIs = 0;
            var toplamSandik = 0m;
            var toplamM3 = 0m;
            var siparisBekleyen = 0;
            var siparisAcik = 0;
            var kismiSiparis = 0;
            var faturaBekleyen = 0;
            var faturalanan = 0;
            var buAyOzelIs = 0;
            if (bolumler.HasFlag(DashboardBolumu.Operasyon))
            {
                var summary = await workQuery!.GroupBy(_ => 1)
                    .Select(group => new
                    {
                        ToplamIs = group.Count(),
                        ToplamSandik = group.Sum(x => x.Adet),
                        ToplamM3 = group.Sum(x => x.ToplamM3),
                        SiparisBekleyen = group.Count(x => x.Durum == FinansIsDurumu.SiparisBekliyor),
                        SiparisAcik = group.Count(x => x.Durum == FinansIsDurumu.SiparisAcildi),
                        KismiSiparis = group.Count(x => x.Durum == FinansIsDurumu.KismiSiparis),
                        FaturaBekleyen = group.Count(x =>
                            x.Durum == FinansIsDurumu.SiparisAcildi ||
                            x.Durum == FinansIsDurumu.KismiFaturalandi),
                        Faturalanan = group.Count(x => x.Durum == FinansIsDurumu.Faturalandi),
                        BuAyOzelIs = group.Count(x =>
                            x.IsTuru == FinansIsTuru.OzelIs &&
                            x.FinansDonemi >= thisMonthStart &&
                            x.FinansDonemi < nextMonthStart)
                    })
                    .SingleOrDefaultAsync(cancellationToken);
                if (summary is not null)
                {
                    toplamIs = summary.ToplamIs;
                    toplamSandik = summary.ToplamSandik;
                    toplamM3 = summary.ToplamM3;
                    siparisBekleyen = summary.SiparisBekleyen;
                    siparisAcik = summary.SiparisAcik;
                    kismiSiparis = summary.KismiSiparis;
                    faturaBekleyen = summary.FaturaBekleyen;
                    faturalanan = summary.Faturalanan;
                    buAyOzelIs = summary.BuAyOzelIs;
                }
            }

            IReadOnlyList<FinansParaToplamiModel> income = Array.Empty<FinansParaToplamiModel>();
            if (bolumler.HasFlag(DashboardBolumu.Gelir))
            {
                var invoiceQuery = _context.Set<FinansFatura>().AsNoTracking()
                    .Where(x => !x.IptalEdildi &&
                                (!start.HasValue || x.FaturaTarihi >= start.Value) &&
                                (!endExclusive.HasValue || x.FaturaTarihi < endExclusive.Value));
                var documentTotals = await BuildInvoiceDocumentTotalsQuery(invoiceQuery)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);
                var calculatedTotals = await BuildInvoiceCalculatedTotalsQuery(invoiceQuery)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);
                income = MergeMoneyTotals(documentTotals.Concat(calculatedTotals));
            }

            var buAyGiderKaydi = 0;
            IReadOnlyList<FinansParaToplamiModel> expenseTotals = Array.Empty<FinansParaToplamiModel>();
            IReadOnlyList<FinansParaToplamiModel> thisMonthExpenses = Array.Empty<FinansParaToplamiModel>();
            if (bolumler.HasFlag(DashboardBolumu.Gider))
            {
                var scopedExpenses = _context.Set<FinansGider>().AsNoTracking()
                    .Where(x => !x.IptalEdildi);
                var expenseQuery = scopedExpenses;
                if (start.HasValue) expenseQuery = expenseQuery.Where(x => x.FinansDonemi >= start.Value);
                if (endExclusive.HasValue) expenseQuery = expenseQuery.Where(x => x.FinansDonemi < endExclusive.Value);
                expenseTotals = await BuildExpenseTotalsQuery(expenseQuery)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);

                var currentMonthQuery = scopedExpenses.Where(x =>
                    x.FinansDonemi >= thisMonthStart &&
                    x.FinansDonemi < nextMonthStart);
                if (start.HasValue) currentMonthQuery = currentMonthQuery.Where(x => x.FinansDonemi >= start.Value);
                if (endExclusive.HasValue) currentMonthQuery = currentMonthQuery.Where(x => x.FinansDonemi < endExclusive.Value);
                buAyGiderKaydi = await currentMonthQuery.CountAsync(cancellationToken);
                thisMonthExpenses = await BuildExpenseTotalsQuery(currentMonthQuery)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);
            }

            IReadOnlyList<FinansParaToplamiModel> pendingAmounts = Array.Empty<FinansParaToplamiModel>();
            IReadOnlyList<FinansParaToplamiModel> openOrderAmounts = Array.Empty<FinansParaToplamiModel>();
            if (bolumler.HasFlag(DashboardBolumu.DurumTutarlari))
            {
                // Kalan iş/sipariş miktarları, para hesabı ve para birimi toplamları
                // doğrudan PostgreSQL projection + GROUP BY sorgularında hesaplanır.
                // Böylece dashboard hacimle birlikte entity/row listesi taşımaz.
                pendingAmounts = await BuildPendingAmountQuery(workQuery!)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);
                var scopedWorkIds = workQuery!.Select(x => x.Id);
                var openOrderLines = _context.Set<FinansSiparisKalemi>().AsNoTracking()
                    .Where(line =>
                        !line.FinansSiparis.IptalEdildi &&
                        scopedWorkIds.Contains(line.FinansIsKaydiId));
                openOrderAmounts = await BuildOpenOrderAmountQuery(openOrderLines)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);
            }

            var currencies = income.Select(x => x.ParaBirimi)
                .Union(expenseTotals.Select(x => x.ParaBirimi))
                .Order()
                .ToArray();
            var nets = currencies.Select(currency =>
            {
                var gelir = income.FirstOrDefault(x => x.ParaBirimi == currency);
                var gider = expenseTotals.FirstOrDefault(x => x.ParaBirimi == currency);
                return new FinansParaToplamiModel(
                    currency,
                    (gelir?.NetTutar ?? 0) - (gider?.NetTutar ?? 0),
                    (gelir?.KdvTutari ?? 0) - (gider?.KdvTutari ?? 0),
                    (gelir?.ToplamTutar ?? 0) - (gider?.ToplamTutar ?? 0));
            }).ToArray();

            return new FinansDashboardModel
            {
                ToplamIs = toplamIs,
                ToplamSandik = toplamSandik,
                ToplamM3 = toplamM3,
                SiparisBekleyen = siparisBekleyen,
                SiparisAcik = siparisAcik,
                KismiSiparis = kismiSiparis,
                FaturaBekleyen = faturaBekleyen,
                Faturalanan = faturalanan,
                BuAyOzelIs = buAyOzelIs,
                BuAyGiderKaydi = buAyGiderKaydi,
                BuAyGiderler = thisMonthExpenses,
                Gelirler = income,
                Giderler = expenseTotals,
                Netler = nets,
                SiparisBekleyenTutarlar = pendingAmounts,
                SiparisAcikTutarlar = openOrderAmounts,
                FaturalananTutarlar = income
            };
        }

        internal static IQueryable<FinansParaToplamiModel> BuildPendingAmountQuery(
            IQueryable<FinansIsKaydi> works)
        {
            var source = works.Select(work => new
            {
                work.Adet,
                work.ToplamM3,
                WorkPricingUnit = work.FiyatlandirmaBirimiSnapshot,
                WorkUnitPrice = work.BirimFiyatSnapshot,
                WorkVat = work.KdvOraniSnapshot,
                WorkCurrency = work.ParaBirimiSnapshot,
                OrderLineCount = work.SiparisKalemleri.Count(x => !x.FinansSiparis.IptalEdildi),
                PricingUnitCount = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .Select(x => x.FiyatlandirmaBirimiSnapshot)
                    .Distinct()
                    .Count(),
                OrderPricingUnit = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .Select(x => (FinansFiyatlandirmaBirimi?)x.FiyatlandirmaBirimiSnapshot)
                    .FirstOrDefault(),
                OrderedAdet = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .Sum(x => (decimal?)x.Adet) ?? 0m,
                OrderedM3 = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .Sum(x => (decimal?)x.M3) ?? 0m,
                LatestUnitPrice = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .OrderByDescending(x => x.FinansSiparis.SiparisTarihi)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (decimal?)x.BirimFiyatSnapshot)
                    .FirstOrDefault(),
                LatestVat = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .OrderByDescending(x => x.FinansSiparis.SiparisTarihi)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (decimal?)x.KdvOraniSnapshot)
                    .FirstOrDefault(),
                LatestCurrency = work.SiparisKalemleri
                    .Where(x => !x.FinansSiparis.IptalEdildi)
                    .OrderByDescending(x => x.FinansSiparis.SiparisTarihi)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.ParaBirimiSnapshot)
                    .FirstOrDefault()
            });
            var priced = source.Select(row => new
            {
                row.Adet,
                row.ToplamM3,
                row.OrderedAdet,
                row.OrderedM3,
                row.OrderLineCount,
                PricingUnit = row.OrderLineCount > 0 &&
                              row.PricingUnitCount == 1 &&
                              row.OrderPricingUnit.HasValue
                    ? row.OrderPricingUnit.Value
                    : row.WorkPricingUnit,
                UnitPrice = row.LatestUnitPrice ?? row.WorkUnitPrice,
                Vat = row.LatestVat ?? row.WorkVat,
                Currency = row.LatestCurrency ?? row.WorkCurrency
            });
            var quantities = priced.Select(row => new
            {
                row.UnitPrice,
                row.Vat,
                row.Currency,
                Remaining = row.PricingUnit == FinansFiyatlandirmaBirimi.Adet
                    ? (row.Adet > row.OrderedAdet ? row.Adet - row.OrderedAdet : 0m)
                    : row.PricingUnit == FinansFiyatlandirmaBirimi.Metrekup
                        ? (row.ToplamM3 > row.OrderedM3 ? row.ToplamM3 - row.OrderedM3 : 0m)
                        : row.PricingUnit == FinansFiyatlandirmaBirimi.SabitTutar && row.OrderLineCount == 0
                            ? 1m
                            : 0m
            });
            var nets = quantities
                .Where(row => row.Remaining > Tolerance && row.UnitPrice > 0)
                .Select(row => new
                {
                    row.Currency,
                    row.Vat,
                    Net = decimal.Round(row.Remaining * row.UnitPrice, 2)
                });
            var amounts = nets.Select(row => new
            {
                row.Currency,
                row.Net,
                Kdv = decimal.Round(row.Net * row.Vat / 100m, 2)
            });
            return amounts
                .GroupBy(row => row.Currency)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.Net),
                    group.Sum(x => x.Kdv),
                    group.Sum(x => x.Net + x.Kdv)));
        }

        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceTotalsQuery(
            IQueryable<FinansFaturaKalemi> invoiceLines)
            => invoiceLines
                .GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.NetTutarSnapshot),
                    group.Sum(x => x.KdvTutariSnapshot),
                    group.Sum(x => x.ToplamTutarSnapshot)));

        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceDocumentTotalsQuery(
            IQueryable<FinansFatura> invoices)
            => invoices
                .Where(x => x.BelgeParaBirimiSnapshot != null &&
                            x.BelgeParaBirimiSnapshot != string.Empty &&
                            x.BelgeNetTutarSnapshot.HasValue &&
                            x.BelgeKdvTutariSnapshot.HasValue &&
                            x.BelgeToplamTutarSnapshot.HasValue)
                .GroupBy(x => x.BelgeParaBirimiSnapshot!)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.BelgeNetTutarSnapshot!.Value),
                    group.Sum(x => x.BelgeKdvTutariSnapshot!.Value),
                    group.Sum(x => x.BelgeToplamTutarSnapshot!.Value)));

        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceCalculatedTotalsQuery(
            IQueryable<FinansFatura> invoices)
            => BuildInvoiceTotalsQuery(invoices
                .Where(x => x.BelgeParaBirimiSnapshot == null ||
                            x.BelgeParaBirimiSnapshot == string.Empty ||
                            !x.BelgeNetTutarSnapshot.HasValue ||
                            !x.BelgeKdvTutariSnapshot.HasValue ||
                            !x.BelgeToplamTutarSnapshot.HasValue)
                .SelectMany(x => x.Kalemler));

        internal static IQueryable<FinansParaToplamiModel> BuildExpenseTotalsQuery(
            IQueryable<FinansGider> expenses)
            => expenses
                .GroupBy(x => x.ParaBirimi)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.Matrah),
                    group.Sum(x => x.KdvTutari),
                    group.Sum(x => x.ToplamTutar)));

        internal static IReadOnlyList<FinansParaToplamiModel> MergeMoneyTotals(
            IEnumerable<FinansParaToplamiModel> totals)
            => totals
                .GroupBy(x => x.ParaBirimi)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.NetTutar),
                    group.Sum(x => x.KdvTutari),
                    group.Sum(x => x.ToplamTutar)))
                .OrderBy(x => x.ParaBirimi)
                .ToArray();

        internal static IQueryable<FinansParaToplamiModel> BuildOpenOrderAmountQuery(
            IQueryable<FinansSiparisKalemi> orderLines)
        {
            var source = orderLines.Select(line => new
            {
                PricingUnit = line.FiyatlandirmaBirimiSnapshot,
                line.Adet,
                line.M3,
                UnitPrice = line.BirimFiyatSnapshot,
                Vat = line.KdvOraniSnapshot,
                Currency = line.ParaBirimiSnapshot,
                InvoicedAdet = line.FaturaKalemleri
                    .Where(x => !x.FinansFatura.IptalEdildi)
                    .Sum(x => (decimal?)x.Adet) ?? 0m,
                InvoicedM3 = line.FaturaKalemleri
                    .Where(x => !x.FinansFatura.IptalEdildi)
                    .Sum(x => (decimal?)x.M3) ?? 0m,
                InvoiceLineCount = line.FaturaKalemleri.Count(x => !x.FinansFatura.IptalEdildi)
            });
            var quantities = source.Select(row => new
            {
                row.UnitPrice,
                row.Vat,
                row.Currency,
                Remaining = row.PricingUnit == FinansFiyatlandirmaBirimi.Adet
                    ? (row.Adet > row.InvoicedAdet ? row.Adet - row.InvoicedAdet : 0m)
                    : row.PricingUnit == FinansFiyatlandirmaBirimi.Metrekup
                        ? (row.M3 > row.InvoicedM3 ? row.M3 - row.InvoicedM3 : 0m)
                        : row.PricingUnit == FinansFiyatlandirmaBirimi.SabitTutar && row.InvoiceLineCount == 0
                            ? 1m
                            : 0m
            });
            var nets = quantities
                .Where(row => row.Remaining > Tolerance)
                .Select(row => new
                {
                    row.Currency,
                    row.Vat,
                    Net = decimal.Round(row.Remaining * row.UnitPrice, 2)
                });
            var amounts = nets.Select(row => new
            {
                row.Currency,
                row.Net,
                Kdv = decimal.Round(row.Net * row.Vat / 100m, 2)
            });
            return amounts
                .GroupBy(row => row.Currency)
                .Select(group => new FinansParaToplamiModel(
                    group.Key,
                    group.Sum(x => x.Net),
                    group.Sum(x => x.Kdv),
                    group.Sum(x => x.Net + x.Kdv)));
        }
    }
}
