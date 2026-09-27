using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    /// <summary>Fatura ve gider tutarlarını para birimine göre SQL tarafında toplayan sorgular.</summary>
    public sealed partial class FinansService
    {
        internal static IQueryable<FinansParaToplamiModel> BuildInvoiceTotalsQuery(
            IQueryable<FinansFaturaKalemi> invoiceLines)
            => invoiceLines
                .GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.NetTutarSnapshot), KdvTutari = group.Sum(x => x.KdvTutariSnapshot), ToplamTutar = group.Sum(x => x.ToplamTutarSnapshot) });

        internal static IQueryable<FinansParaToplamiModel> BuildExpenseTotalsQuery(
            IQueryable<FinansGider> expenses)
            => expenses.Where(x => !x.AvansMi)
                .GroupBy(x => x.ParaBirimi)
                .Select(group => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = group.Key, NetTutar = group.Sum(x => x.Matrah), KdvTutari = group.Sum(x => x.KdvTutari), ToplamTutar = group.Sum(x => x.ToplamTutar) });
    }
}
