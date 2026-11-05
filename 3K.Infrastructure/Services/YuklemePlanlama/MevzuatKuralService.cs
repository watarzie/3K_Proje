using Microsoft.EntityFrameworkCore;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>Mevzuat kural setleri, kuralları ve kaynakları (salt okunur; parametreler veritabanındaki JSON'dur).</summary>
internal sealed class MevzuatKuralService(AppDbContext context) : IMevzuatKuralService
{
    public async Task<IReadOnlyList<MevzuatKuralSetiOzeti>> KuralSetleriAsync(CancellationToken cancellationToken) =>
        await context.MevzuatKuralSetleri.AsNoTracking()
            .OrderBy(s => s.YetkiAlani).ThenByDescending(s => s.SurumNo)
            .Select(s => new MevzuatKuralSetiOzeti
            {
                Id = s.Id,
                YetkiAlani = s.YetkiAlani,
                Ad = s.Ad,
                SurumNo = s.SurumNo,
                GecerlilikBaslangici = s.GecerlilikBaslangici,
                GecerlilikBitisi = s.GecerlilikBitisi,
                Durum = s.Durum,
                OnayTarihi = s.OnayTarihi,
                KuralSayisi = s.Kurallar.Count
            })
            .ToListAsync(cancellationToken);

    public async Task<MevzuatKuralSetiDetayi?> KuralSetiGetirAsync(int kuralSetiId, CancellationToken cancellationToken)
    {
        var set = await context.MevzuatKuralSetleri.AsNoTracking()
            .Where(s => s.Id == kuralSetiId)
            .Select(s => new
            {
                Ozet = new MevzuatKuralSetiOzeti
                {
                    Id = s.Id,
                    YetkiAlani = s.YetkiAlani,
                    Ad = s.Ad,
                    SurumNo = s.SurumNo,
                    GecerlilikBaslangici = s.GecerlilikBaslangici,
                    GecerlilikBitisi = s.GecerlilikBitisi,
                    Durum = s.Durum,
                    OnayTarihi = s.OnayTarihi,
                    KuralSayisi = s.Kurallar.Count
                },
                s.Aciklama
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (set == null)
            return null;

        var kurallar = await context.MevzuatKurallari.AsNoTracking()
            .Where(k => k.KuralSetiId == kuralSetiId)
            .OrderBy(k => k.Sira).ThenBy(k => k.Id)
            .Select(k => new MevzuatKuraliModeli
            {
                Id = k.Id,
                Kod = k.Kod,
                Tip = k.Tip,
                ParametrelerJson = k.ParametrelerJson,
                KaynakId = k.KaynakId,
                Madde = k.Madde,
                Aciklama = k.Aciklama,
                Sira = k.Sira
            })
            .ToListAsync(cancellationToken);
        var kaynakIdleri = kurallar.Where(k => k.KaynakId != null).Select(k => k.KaynakId!.Value).Distinct().ToList();
        var kaynaklar = await context.MevzuatKaynaklari.AsNoTracking()
            .Where(k => kaynakIdleri.Contains(k.Id))
            .OrderBy(k => k.Id)
            .Select(k => new MevzuatKaynagiModeli
            {
                Id = k.Id,
                Ad = k.Ad,
                Tur = k.Tur,
                ResmiGazeteTarihi = k.ResmiGazeteTarihi,
                ResmiGazeteSayisi = k.ResmiGazeteSayisi,
                Url = k.Url,
                ErisimTarihi = k.ErisimTarihi,
                Aciklama = k.Aciklama
            })
            .ToListAsync(cancellationToken);

        return new MevzuatKuralSetiDetayi { Ozet = set.Ozet, Aciklama = set.Aciklama, Kurallar = kurallar, Kaynaklar = kaynaklar };
    }
}
