using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Eksik ve gerçekleşen raporların paylaştığı saha tamamlama planı ve kalan miktar hesabı.</summary>
internal static class RaporSorgulari
{
    /// <summary>Kaynak satırların saha projelerinde tamamlanan (istenen) miktarı; kaynak satır kimliğine göre.</summary>
    internal static async Task<Dictionary<int, decimal>> GetTamamlamaPlanMapAsync(
        this AppDbContext context,
        IReadOnlyCollection<CekiSatiri> satirlar,
        bool sadeceSevkEdilenSandiklar,
        CancellationToken cancellationToken = default)
    {
        var kaynakSatirIds = satirlar
            .Where(s => !s.KaynakCekiSatiriId.HasValue)
            .Select(s => s.Id)
            .Distinct()
            .ToList();

        if (!kaynakSatirIds.Any())
            return new Dictionary<int, decimal>();

        var query = context.CekiSatirlari
            .AsNoTracking()
            .Where(cs => cs.KaynakCekiSatiriId.HasValue &&
                kaynakSatirIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha);

        if (sadeceSevkEdilenSandiklar)
            query = query.Where(cs => cs.SandikIcerikleri.Any(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi));

        return await query
            .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
            .Select(g => new { CekiSatiriId = g.Key, TamamlananAdet = g.Sum(cs => cs.IstenenAdet) })
            .ToDictionaryAsync(x => x.CekiSatiriId, x => x.TamamlananAdet, cancellationToken);
    }

    /// <summary>Projenin sevkiyat kayıtlarındaki sandıkların son sevk tarihi (sandık kimliğine göre).</summary>
    internal static async Task<Dictionary<int, DateTime>> GetSandikSevkTarihleriAsync(
        this AppDbContext context,
        int projeId,
        CancellationToken cancellationToken = default) =>
        await context.SevkiyatSandiklari
            .AsNoTracking()
            .Where(ss => ss.Sevkiyat.ProjeId == projeId)
            .GroupBy(ss => ss.SandikId)
            .Select(g => new { SandikId = g.Key, Tarih = g.Max(ss => ss.Sevkiyat.SevkTarihi) })
            .ToDictionaryAsync(x => x.SandikId, x => x.Tarih, cancellationToken);

    /// <summary>
    /// Kaynak satırların sahaya aktarılan kısımları (aktif aktarım kalemleri; kaynak satır kimliğine göre): kalem
    /// miktarı ve saha satırının fiziksel izi. Sahadan giden adet kalem durumundan değil, saha satırının sevk
    /// edilmiş saha sandığına fiilen konan adedinden okunur (R-07); kalem durumu sandık bazlı olduğundan içine
    /// konmamış ürünü de "sevk edildi" sayabilir. Geri alınan ve iptal edilen kalemler gelmez.
    /// </summary>
    internal static async Task<Dictionary<int, List<SahaAktarimParcasi>>> GetSahaAktarimParcalariAsync(
        this AppDbContext context,
        IEnumerable<CekiSatiri> satirlar,
        CancellationToken cancellationToken = default)
    {
        var satirIds = satirlar.Where(s => s.Id > 0).Select(s => s.Id).Distinct().ToList();
        if (satirIds.Count == 0)
            return new Dictionary<int, List<SahaAktarimParcasi>>();

        var kalemler = await context.SahaAktarimKalemleri
            .AsNoTracking()
            .Where(k => satirIds.Contains(k.KaynakCekiSatiriId) &&
                k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                k.DurumId != (int)SahaAktarimDurum.Iptal)
            .Select(k => new
            {
                k.KaynakCekiSatiriId,
                k.Miktar,
                SevkliSandigaKonulan = context.SandikIcerikleri
                    .Where(si => k.SahaCekiSatiriId.HasValue && si.CekiSatiriId == k.SahaCekiSatiriId &&
                        si.Sandik.ProjeId == k.SahaProjeId && si.Sandik.DurumId == (int)SandikDurum.Sevkedildi)
                    .Sum(si => (decimal?)si.KonulanAdet) ?? 0,
                Trafo = context.CekiSatirlari
                    .Where(cs => cs.Id == k.SahaCekiSatiriId)
                    .Select(cs => (decimal?)cs.TrafoSevkAdet)
                    .FirstOrDefault() ?? 0,
                SahaGridDurumuId = context.CekiSatirlari
                    .Where(cs => cs.Id == k.SahaCekiSatiriId)
                    .Select(cs => (int?)cs.GridDurumuId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return kalemler
            .GroupBy(k => k.KaynakCekiSatiriId)
            .ToDictionary(g => g.Key, g => g.Select(k => new SahaAktarimParcasi(
                k.Miktar,
                k.SevkliSandigaKonulan,
                k.Trafo,
                k.SahaGridDurumuId is (int)GridDurum.Iptal or (int)GridDurum.IptalEdildi or (int)GridDurum.GridKapandi)).ToList());
    }

    internal static decimal GetEksikRaporKalan(CekiSatiri satir, IReadOnlyDictionary<int, decimal> tamamlamaPlanMap)
    {
        if (satir.KaynakCekiSatiriId.HasValue)
            return satir.KalanMiktar;

        var sevkEdilenSahaTamamlama = tamamlamaPlanMap.TryGetValue(satir.Id, out var value) ? value : 0;
        return Math.Max(satir.KalanMiktar - sevkEdilenSahaTamamlama, 0);
    }
}
