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
            var reference = bitis ?? baslangic ?? TurkeyTime.Now;
            var thisMonthStart = new DateTime(reference.Year, reference.Month, 1);
            var nextMonthStart = thisMonthStart.AddMonths(1);
            IQueryable<FinansIsKaydi>? workQuery = null;
            if (bolumler.HasFlag(DashboardBolumu.Operasyon) || bolumler.HasFlag(DashboardBolumu.DurumTutarlari))
            {
                workQuery = _context.Set<FinansIsKaydi>().AsNoTracking()
                    .Where(x => !x.IptalEdildi && x.KaynakAktif);
                if (start.HasValue) workQuery = workQuery.Where(x => x.FinansTarihi >= start.Value);
                if (endExclusive.HasValue) workQuery = workQuery.Where(x => x.FinansTarihi < endExclusive.Value);
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
                        ToplamSandik = group.Where(x => x.IsTuru != FinansIsTuru.SarfKereste).Sum(x => x.Adet),
                        ToplamM3 = group.Where(x => x.IsTuru != FinansIsTuru.SarfKereste).Sum(x => x.ToplamM3),
                        SiparisBekleyen = group.Count(x => x.Durum == FinansIsDurumu.SiparisBekliyor),
                        SiparisAcik = group.Count(x => x.Durum == FinansIsDurumu.SiparisAcildi),
                        KismiSiparis = group.Count(x => x.Durum == FinansIsDurumu.KismiSiparis),
                        FaturaBekleyen = group.Count(x =>
                            x.Durum == FinansIsDurumu.SiparisAcildi ||
                            x.Durum == FinansIsDurumu.KismiFaturalandi),
                        Faturalanan = group.Count(x => x.Durum == FinansIsDurumu.Faturalandi),
                        BuAyOzelIs = group.Count(x =>
                            x.IsTuru == FinansIsTuru.OzelIs &&
                            x.FinansTarihi >= thisMonthStart &&
                            x.FinansTarihi < nextMonthStart)
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
                var invoiceLines = ApplyInvoiceLineFilter(_context.Set<FinansFaturaKalemi>().AsNoTracking(),
                    new FinansListeFiltre(Baslangic: baslangic, Bitis: bitis));
                income = await BuildInvoiceTotalsQuery(invoiceLines).OrderBy(x => x.ParaBirimi).ToListAsync(cancellationToken);
            }

            var buAyGiderKaydi = 0;
            IReadOnlyList<FinansParaToplamiModel> expenseTotals = Array.Empty<FinansParaToplamiModel>();
            IReadOnlyList<FinansParaToplamiModel> thisMonthExpenses = Array.Empty<FinansParaToplamiModel>();
            if (bolumler.HasFlag(DashboardBolumu.Gider))
            {
                var scopedExpenses = _context.Set<FinansGider>().AsNoTracking()
                    .Where(x => !x.IptalEdildi);
                var expenseQuery = scopedExpenses;
                if (start.HasValue) expenseQuery = expenseQuery.Where(x => x.FinansTarihi >= start.Value);
                if (endExclusive.HasValue) expenseQuery = expenseQuery.Where(x => x.FinansTarihi < endExclusive.Value);
                expenseTotals = await BuildExpenseTotalsQuery(expenseQuery)
                    .OrderBy(x => x.ParaBirimi)
                    .ToListAsync(cancellationToken);

                var currentMonthQuery = scopedExpenses.Where(x =>
                    x.FinansTarihi >= thisMonthStart &&
                    x.FinansTarihi < nextMonthStart);
                if (start.HasValue) currentMonthQuery = currentMonthQuery.Where(x => x.FinansTarihi >= start.Value);
                if (endExclusive.HasValue) currentMonthQuery = currentMonthQuery.Where(x => x.FinansTarihi < endExclusive.Value);
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
                return new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = currency, NetTutar = (gelir?.NetTutar ?? 0) - (gider?.NetTutar ?? 0), KdvTutari = (gelir?.KdvTutari ?? 0) - (gider?.KdvTutari ?? 0), ToplamTutar = (gelir?.ToplamTutar ?? 0) - (gider?.ToplamTutar ?? 0) };
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

        internal static IQueryable<FinansParaToplamiModel> BuildPendingAmountQuery(IQueryable<FinansIsKaydi> works)
        {
            var amounts = works.Select(work => new
            {
                Currency = work.ParaBirimiSnapshot, Vat = work.KdvOraniSnapshot,
                Capacity = work.ManuelNetTutar ?? decimal.Round(work.BirimFiyatSnapshot *
                    (work.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet ? work.Adet :
                     work.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Metrekup ? work.ToplamM3 : 1m), 2),
                Used = work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).Sum(x => (decimal?)x.NetTutarSnapshot) ?? 0m
            }).Select(x => new { x.Currency, x.Vat, Net = x.Capacity > x.Used ? x.Capacity - x.Used : 0m });
            return amounts.GroupBy(x => x.Currency).Select(g => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = g.Key, NetTutar = g.Sum(x => x.Net), KdvTutari = g.Sum(x => decimal.Round(x.Net * x.Vat / 100m, 2)), ToplamTutar = g.Sum(x => x.Net + decimal.Round(x.Net * x.Vat / 100m, 2)) });
        }

        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceTotalsQuery(
            IQueryable<FinansFaturaKalemi> invoiceLines)
            => invoiceLines
                .GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.NetTutarSnapshot), KdvTutari = group.Sum(x => x.KdvTutariSnapshot), ToplamTutar = group.Sum(x => x.ToplamTutarSnapshot) });

        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceDocumentTotalsQuery(
            IQueryable<FinansFatura> invoices)
            => invoices
                .Where(x => x.BelgeParaBirimiSnapshot != null &&
                            x.BelgeParaBirimiSnapshot != string.Empty &&
                            x.BelgeNetTutarSnapshot.HasValue &&
                            x.BelgeKdvTutariSnapshot.HasValue &&
                            x.BelgeToplamTutarSnapshot.HasValue)
                .GroupBy(x => x.BelgeParaBirimiSnapshot!)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.BelgeNetTutarSnapshot!.Value), KdvTutari = group.Sum(x => x.BelgeKdvTutariSnapshot!.Value), ToplamTutar = group.Sum(x => x.BelgeToplamTutarSnapshot!.Value) });

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
            => expenses.Where(x => !x.AvansMi)
                .GroupBy(x => x.ParaBirimi)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.Matrah), KdvTutari = group.Sum(x => x.KdvTutari), ToplamTutar = group.Sum(x => x.ToplamTutar) });

        internal static IReadOnlyList<FinansParaToplamiModel> MergeMoneyTotals(
            IEnumerable<FinansParaToplamiModel> totals)
            => totals
                .GroupBy(x => x.ParaBirimi)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.NetTutar), KdvTutari = group.Sum(x => x.KdvTutari), ToplamTutar = group.Sum(x => x.ToplamTutar) })
                .OrderBy(x => x.ParaBirimi)
                .ToArray();

        internal static IQueryable<FinansParaToplamiModel> BuildOpenOrderAmountQuery(IQueryable<FinansSiparisKalemi> orderLines)
        {
            var amounts = orderLines.Select(line => new
            {
                Currency = line.ParaBirimiSnapshot,
                Net = line.NetTutarSnapshot - (line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).Sum(x => (decimal?)x.NetTutarSnapshot) ?? 0m),
                Vat = line.KdvTutariSnapshot - (line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).Sum(x => (decimal?)x.KdvTutariSnapshot) ?? 0m)
            });
            return amounts.GroupBy(x => x.Currency).Select(g => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = g.Key, NetTutar = g.Sum(x => x.Net), KdvTutari = g.Sum(x => x.Vat), ToplamTutar = g.Sum(x => x.Net + x.Vat) });
        }
    }
}
