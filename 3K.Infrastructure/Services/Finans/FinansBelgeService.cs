using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>PO, fatura ve gider kayıtlarına bağlı, sürümlenen PDF belgeleri.</summary>
internal sealed class FinansBelgeService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    ICurrentUserService currentUser) : IFinansBelgeService
{
    public async Task<IReadOnlyList<FinansBelgeModel>> BelgelerAsync(string hedefTuru, int hedefId, CancellationToken cancellationToken)
    {
        if (await context.FinansHedefKaydiAsync(hedefTuru, hedefId, cancellationToken) is null) throw IsKuraliException.Cakisma("Belge hedefi bulunamadı.");
        return await context.Set<FinansBelge>().AsNoTracking().Where(x => x.HedefTuru == hedefTuru && x.HedefId == hedefId).OrderByDescending(x => x.Surum)
            .Select(x => new FinansBelgeModel(x.Id, x.HedefTuru, x.HedefId, x.Surum, x.OrijinalAd, x.Boyut, x.IcerikTuru, x.Hash, x.Yukleyen, x.CreatedDate)).ToListAsync(cancellationToken);
    }

    internal static void ValidatePdf(byte[] bytes, string name)
    {
        if (bytes.Length is < 8 or > 10 * 1024 * 1024 || !name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) || !Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024))).Contains("%%EOF", StringComparison.Ordinal))
            throw IsKuraliException.Cakisma("En fazla 10 MB boyutunda geçerli bir PDF yükleyin.");
    }

    public Task<FinansBelgeModel> BelgeYukleAsync(FinansBelgeYukleModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            ValidatePdf(model.Icerik, model.OrijinalAd);
            if (model.HedefTuru == "IsKaydi") throw IsKuraliException.Cakisma("PDF yalnız PO, fatura veya gider belgesine bağlanabilir.");
            if (await context.FinansHedefKaydiAsync(model.HedefTuru, model.HedefId, cancellationToken) is null) throw IsKuraliException.Cakisma("Belge hedefi bulunamadı.");
            var name = Path.GetFileName(model.OrijinalAd.Replace('\\', '/'));
            name = new string(name.Where(c => !char.IsControl(c)).ToArray());
            if (name.Length > 250) name = name[..246] + ".pdf";
            var entity = new FinansBelge
            {
                HedefTuru = model.HedefTuru, HedefId = model.HedefId,
                Surum = (await context.Set<FinansBelge>().Where(x => x.HedefTuru == model.HedefTuru && x.HedefId == model.HedefId).MaxAsync(x => (int?)x.Surum, cancellationToken) ?? 0) + 1,
                OrijinalAd = name, GuvenliAd = $"{Guid.NewGuid():N}.pdf", Icerik = model.Icerik,
                Boyut = model.Icerik.Length, Hash = Convert.ToHexString(SHA256.HashData(model.Icerik)),
                Yukleyen = currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture) ?? "SYSTEM"
            };
            context.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            denetim.Ekle(nameof(FinansBelge), entity.Id, "PDF Yükleme", "Sürüm", null, entity.Surum, $"{model.HedefTuru}/{model.HedefId}");
            await context.SaveChangesAsync(cancellationToken);
            return new FinansBelgeModel(entity.Id, entity.HedefTuru, entity.HedefId, entity.Surum, entity.OrijinalAd, entity.Boyut, entity.IcerikTuru, entity.Hash, entity.Yukleyen, entity.CreatedDate);
        }, cancellationToken);

    public async Task<DosyaIcerigi?> BelgeIndirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansBelge>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (await context.FinansHedefKaydiAsync(entity.HedefTuru, entity.HedefId, cancellationToken) is null) throw IsKuraliException.Cakisma("Belge hedefi bulunamadı.");
        return new(entity.Icerik, entity.IcerikTuru, entity.OrijinalAd);
    }
}
