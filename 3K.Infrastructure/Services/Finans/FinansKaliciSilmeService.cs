using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Finans kayıtlarının bağımlılık önizlemesi, sürüm kontrolü ve ikinci onayla kalıcı silinmesi. Başka
/// kayıtlarca kullanılan kayıt silinmez; PO ve faturanın kendi satırları ile ekleri kayıtla birlikte
/// silinir, böylece hiçbir PO veya fatura bozuk toplamla kalmaz.
/// </summary>
internal sealed class FinansKaliciSilmeService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansKaliciSilmeService
{
    public async Task<FinansKaliciSilOnizlemeModel?> KaliciSilOnizlemeAsync(string varlikTuru, int id, CancellationToken cancellationToken)
    {
        var target = await context.FinansHedefKaydiAsync(varlikTuru, id, cancellationToken);
        if (target is null) return null;
        var blockers = new List<(FinansBagimlilikModel Kayit, string Mesaj)>();
        var cascade = new List<FinansBagimlilikModel>();
        switch (target)
        {
            case FinansIsKaydi:
                foreach (var po in await context.Set<FinansSiparisKalemi>().Where(x => x.FinansIsKaydiId == id)
                             .Select(x => new { x.FinansSiparisId, x.FinansSiparis.PoNumarasi }).Distinct()
                             .OrderBy(x => x.PoNumarasi).ToListAsync(cancellationToken))
                    blockers.Add((new("Siparis", po.FinansSiparisId, po.PoNumarasi),
                        $"Bu kayıt PO {po.PoNumarasi} içerisinde kullanılmaktadır."));
                break;
            case FinansSiparis:
                foreach (var invoice in await context.Set<FinansFatura>().Where(x => x.FinansSiparisId == id)
                             .OrderBy(x => x.FaturaNumarasi).Select(x => new { x.Id, x.FaturaNumarasi }).ToListAsync(cancellationToken))
                    blockers.Add((new("Fatura", invoice.Id, invoice.FaturaNumarasi),
                        $"Bu PO {invoice.FaturaNumarasi} numaralı faturada kullanılmaktadır."));
                foreach (var expense in await context.Set<FinansGider>().Where(x => x.FinansSiparisId == id)
                             .OrderBy(x => x.Id).Select(x => new { x.Id, x.Aciklama }).ToListAsync(cancellationToken))
                    blockers.Add((new("Gider", expense.Id, expense.Aciklama),
                        $"Bu PO {expense.Aciklama} giderine bağlıdır."));
                cascade.AddRange(await context.Set<FinansSiparisKalemi>().Where(x => x.FinansSiparisId == id).OrderBy(x => x.Id)
                    .Select(x => new FinansBagimlilikModel("SiparisKalemi", x.Id, x.FinansIsKaydi.ProjeNo + " / " + x.FinansIsKaydi.IsAdi))
                    .ToListAsync(cancellationToken));
                break;
            case FinansFatura:
                cascade.AddRange(await context.Set<FinansFaturaKalemi>().Where(x => x.FinansFaturaId == id).OrderBy(x => x.Id)
                    .Select(x => new FinansBagimlilikModel("FaturaKalemi", x.Id,
                        x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo + " / " + x.FinansSiparisKalemi.FinansIsKaydi.IsAdi))
                    .ToListAsync(cancellationToken));
                break;
            case FinansGider:
                foreach (var settlement in await context.Set<FinansGider>().Where(x => x.MahsupEdilenAvansId == id)
                             .OrderBy(x => x.Id).Select(x => new { x.Id, x.Aciklama }).ToListAsync(cancellationToken))
                    blockers.Add((new("Gider", settlement.Id, settlement.Aciklama),
                        $"Bu avans {settlement.Aciklama} mahsup giderinde kullanılmaktadır."));
                break;
        }
        cascade.AddRange(await context.Set<FinansBelge>().Where(x => x.HedefTuru == varlikTuru && x.HedefId == id).OrderBy(x => x.Id)
            .Select(x => new FinansBagimlilikModel("Belge", x.Id, x.OrijinalAd)).ToListAsync(cancellationToken));

        var dependencies = blockers.Select(x => x.Kayit).ToArray();
        var reference = target switch { FinansIsKaydi x => $"{x.ProjeNo} / {x.IsAdi}", FinansSiparis x => x.PoNumarasi, FinansFatura x => x.FaturaNumarasi, FinansGider x => x.Aciklama, _ => id.ToString() };
        var snapshot = JsonSerializer.Serialize(new
        {
            varlikTuru, id, State = FinansDenetimKaydi.AnlikGoruntu(target), target.UpdatedDate,
            Dependencies = dependencies.OrderBy(x => x.VarlikTuru).ThenBy(x => x.Id),
            Cascade = cascade.OrderBy(x => x.VarlikTuru).ThenBy(x => x.Id)
        });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        return new(varlikTuru, id, reference, version, dependencies.Length == 0, dependencies,
            blockers.Select(x => x.Mesaj).ToArray(), cascade);
    }

    public Task<bool> KaliciSilAsync(FinansKaliciSilModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            FinansGerekceKurali.Dogrula(model.Aciklama);
            if (!model.IkinciOnay) throw IsKuraliException.Cakisma("Geri alınamaz kalıcı silme için ikinci onay zorunludur.");
            var preview = await KaliciSilOnizlemeAsync(model.VarlikTuru, model.Id, cancellationToken);
            if (preview is null) return false;
            if (preview.Surum != model.Surum) throw IsKuraliException.Cakisma("Kayıt veya ilişkileri değişti; bağımlılık önizlemesini yeniden açın.");
            if (!preview.Silinebilir) throw IsKuraliException.Cakisma(string.Join(" ", preview.Engeller));
            var entity = (await context.FinansHedefKaydiAsync(model.VarlikTuru, model.Id, cancellationToken))!;
            if (entity is FinansIsKaydi work && work.KaynakKayitId is not null)
                context.Add(new FinansKaynakBastirma { KaynakTuru = work.KaynakTuru, KaynakKayitId = work.KaynakKayitId, KaynakBileseni = work.KaynakBileseni, Aciklama = model.Aciklama.Trim() });
            denetim.Ekle(entity.GetType().Name, model.Id, "Kalıcı Silme", "*", JsonSerializer.Serialize(FinansDenetimKaydi.AnlikGoruntu(entity)), preview.Referans, model.Aciklama);
            var affectedWorkIds = await BirlikteSilAsync(entity, model.VarlikTuru, cancellationToken);
            context.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
            await DurumlariYenileAsync(entity, affectedWorkIds, cancellationToken);
            return true;
        }, cancellationToken);

    /// <summary>Kaydın kendi satırlarını ve eklerini siler; durumu değişecek işlerin kimliklerini döndürür.</summary>
    private async Task<IReadOnlyList<int>> BirlikteSilAsync(BaseEntity entity, string varlikTuru, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> workIds = [];
        switch (entity)
        {
            case FinansSiparis order:
                var orderLines = await context.Set<FinansSiparisKalemi>().Where(x => x.FinansSiparisId == order.Id).ToListAsync(cancellationToken);
                workIds = orderLines.Select(x => x.FinansIsKaydiId).Distinct().ToArray();
                context.RemoveRange(orderLines);
                break;
            case FinansFatura invoice:
                var invoiceLines = await context.Set<FinansFaturaKalemi>().Include(x => x.FinansSiparisKalemi)
                    .Where(x => x.FinansFaturaId == invoice.Id).ToListAsync(cancellationToken);
                workIds = invoiceLines.Select(x => x.FinansSiparisKalemi.FinansIsKaydiId).Distinct().ToArray();
                context.RemoveRange(invoiceLines);
                break;
        }
        var attachments = await context.Set<FinansBelge>().Where(x => x.HedefTuru == varlikTuru && x.HedefId == entity.Id).ToListAsync(cancellationToken);
        context.RemoveRange(attachments);
        return workIds;
    }

    /// <summary>Silinen PO/fatura satırlarından sonra PO ve iş durumları yeniden türetilir.</summary>
    private async Task DurumlariYenileAsync(BaseEntity entity, IReadOnlyList<int> workIds, CancellationToken cancellationToken)
    {
        if (entity is FinansFatura invoice)
        {
            var order = await context.Set<FinansSiparis>()
                .Include(x => x.Kalemler).ThenInclude(x => x.FaturaKalemleri).ThenInclude(x => x.FinansFatura)
                .AsSplitQuery()
                .FirstAsync(x => x.Id == invoice.FinansSiparisId, cancellationToken);
            if (!order.IptalEdildi)
            {
                var oldStatus = order.Durum;
                order.Durum = FinansTutarKurallari.SiparisDurumu(order);
                denetim.DegisiklikEkle(nameof(FinansSiparis), order.Id, nameof(order.Durum), oldStatus, order.Durum);
            }
        }
        if (workIds.Count == 0) return;
        var works = await context.IsKaydiDetayQuery(true).Where(x => workIds.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var work in works)
        {
            var oldStatus = work.Durum;
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(FinansIsKaydi.Durum), oldStatus, work.Durum);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
