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

/// <summary>Finans kayıtlarının bağımlılık önizlemesi, sürüm kontrolü ve ikinci onayla kalıcı silinmesi.</summary>
internal sealed class FinansKaliciSilmeService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansKaliciSilmeService
{
    public async Task<FinansKaliciSilOnizlemeModel?> KaliciSilOnizlemeAsync(string varlikTuru, int id, CancellationToken cancellationToken)
    {
        var target = await context.FinansHedefKaydiAsync(varlikTuru, id, cancellationToken);
        if (target is null) return null;
        var dependencies = new List<FinansBagimlilikModel>();
        if (target is FinansIsKaydi)
        {
            dependencies.AddRange(await context.Set<FinansSiparisKalemi>().Where(x => x.FinansIsKaydiId == id)
                .Select(x => new FinansBagimlilikModel("Siparis", x.FinansSiparisId, x.FinansSiparis.PoNumarasi)).Distinct().ToListAsync(cancellationToken));
        }
        if (target is FinansSiparis)
        {
            dependencies.AddRange(await context.Set<FinansFatura>().Where(x => x.FinansSiparisId == id)
                .Select(x => new FinansBagimlilikModel("Fatura", x.Id, x.FaturaNumarasi)).ToListAsync(cancellationToken));
            dependencies.AddRange(await context.Set<FinansSiparisKalemi>().Where(x => x.FinansSiparisId == id)
                .Select(x => new FinansBagimlilikModel("SiparisKalemi", x.Id, x.FinansIsKaydi.ProjeNo)).ToListAsync(cancellationToken));
        }
        if (target is FinansFatura)
            dependencies.AddRange(await context.Set<FinansFaturaKalemi>().Where(x => x.FinansFaturaId == id)
                .Select(x => new FinansBagimlilikModel("FaturaKalemi", x.Id, x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo)).ToListAsync(cancellationToken));
        if (target is FinansGider)
            dependencies.AddRange(await context.Set<FinansGider>().Where(x => x.MahsupEdilenAvansId == id)
                .Select(x => new FinansBagimlilikModel("Gider", x.Id, x.Aciklama)).ToListAsync(cancellationToken));
        dependencies.AddRange(await context.Set<FinansBelge>().Where(x => x.HedefTuru == varlikTuru && x.HedefId == id)
            .Select(x => new FinansBagimlilikModel("Belge", x.Id, x.OrijinalAd)).ToListAsync(cancellationToken));
        var reference = target switch { FinansIsKaydi x => $"{x.ProjeNo} / {x.IsAdi}", FinansSiparis x => x.PoNumarasi, FinansFatura x => x.FaturaNumarasi, FinansGider x => x.Aciklama, _ => id.ToString() };
        var snapshot = JsonSerializer.Serialize(new { varlikTuru, id, State = FinansDenetimKaydi.AnlikGoruntu(target), target.UpdatedDate, Dependencies = dependencies.OrderBy(x => x.VarlikTuru).ThenBy(x => x.Id) });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        return new(varlikTuru, id, reference, version, dependencies.Count == 0, dependencies,
            dependencies.Count == 0 ? Array.Empty<string>() : ["Bağlı kayıtlar bulundu. Ortak PO/fatura satırları birlikte silinmez; normal iptal/düzeltme akışını kullanın."]);
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
            context.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
}
