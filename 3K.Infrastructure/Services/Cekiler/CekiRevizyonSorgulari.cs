using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Revizyon bağlamı (proje, ana çeki ve satırları), dışarı giden aktif transferler ve stok geri alma kontrolleri için sorgular.</summary>
internal sealed class CekiRevizyonSorgulari(
    AppDbContext context)
{
    internal async Task<RevizyonBaglami> RevizyonBaglaminiYukleAsync(CiktiImportBilgisi import)
    {
        if (string.IsNullOrWhiteSpace(import.FbNo))
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyasında FB NO (Proje Adı) bulunamadı.",
                CekiRevizyonSorunKodlari.ProjeBilgisiEksik);
        }

        var proje = await context.Projeler
            .FirstOrDefaultAsync(p => p.FBNo == import.FbNo || p.ProjeNo == import.FbNo);

        if (proje == null)
        {
            var mesaj = $"Revizyon dosyasındaki proje ({import.FbNo}) sistemde bulunamadı. Önce ana çeki yüklenmiş olmalı.";
            throw new CekiRevizyonConflictException(
                mesaj,
                new[]
                {
                    CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                        CekiRevizyonSorunKodlari.ProjeBulunamadi,
                        mesaj,
                        CekiRevizyonSorunKategorileri.DurumCakismasi)
                });
        }

        var anaCeki = await context.Cekiler
            .Where(c => c.ProjeId == proje.Id && c.CekiTipiId == (int)CekiTipi.Normal)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();

        if (anaCeki == null)
        {
            var mesaj = $"{proje.ProjeNo} projesi için revize edilecek ana çeki bulunamadı.";
            throw new CekiRevizyonConflictException(
                mesaj,
                new[]
                {
                    CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                        CekiRevizyonSorunKodlari.AnaCekiBulunamadi,
                        mesaj,
                        CekiRevizyonSorunKategorileri.DurumCakismasi)
                });
        }

        var anaSatirlar = await context.CekiSatirlari
            .Include(s => s.SandikIcerikleri)
            .ThenInclude(i => i.Sandik)
            .Where(s => s.CekiId == anaCeki.Id)
            .ToListAsync();

        return new RevizyonBaglami(proje, anaCeki, anaSatirlar);
    }

    internal async Task<bool> RevizyonDisariGidenAktifTransferVarMiAsync(int cekiSatiriId)
    {
        return await context.ProjeTransferleri.AnyAsync(t =>
            t.KaynakCekiSatiriId == cekiSatiriId &&
            t.DurumId == (int)ProjeTransferDurum.Aktif &&
            (!t.HedefCekiSatiriId.HasValue || t.HedefCekiSatiriId.Value != cekiSatiriId));
    }

    internal async Task<string?> RevizyonOtomatikGeriAlEngelMesajiAsync(CekiSatiri satir)
    {
        if (satir.SandikIcerikleri.Any(i => CekiRevizyonKurallari.SandikSevkKilitliMi(i.Sandik)))
            return "Satır sevk edilmiş sandıkta. Otomatik geri alma yapılamaz; önce ilgili sandığın sevk kilidini açın.";

        if (await RevizyonDisariGidenAktifTransferVarMiAsync(satir.Id))
            return "Satır başka bir proje/satıra kaynak olarak verilmiş. Otomatik geri alma için önce hedef projedeki karşılama/transfer geri alınmalı.";

        return null;
    }

    internal async Task<List<RevizyonStokEngeli>> RevizyonStokGeriAlEngelleriniBulAsync(
        IReadOnlyCollection<StokHareketi> stokHareketleri)
    {
        if (stokHareketleri.Count == 0)
            return new List<RevizyonStokEngeli>();

        var kontrolBaglami = await RevizyonStokKontrolBaglaminiYukleAsync(
            stokHareketleri.Select(h => h.StokKaydiId));

        return CekiRevizyonKurallari.RevizyonStokGeriAlEngelleriniBul(stokHareketleri, kontrolBaglami);
    }

    internal async Task<RevizyonStokKontrolBaglami> RevizyonStokKontrolBaglaminiYukleAsync(
        IEnumerable<int> stokKaydiIds)
    {
        var stokIdler = stokKaydiIds.Distinct().ToList();
        if (stokIdler.Count == 0)
            return RevizyonStokKontrolBaglami.Bos;

        var stoklar = await context.StokKayitlari
            .AsNoTracking()
            .Where(s => stokIdler.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
        var tumIlgiliStokHareketleri = await context.StokHareketleri
            .AsNoTracking()
            .Where(h => stokIdler.Contains(h.StokKaydiId))
            .ToListAsync();

        return new RevizyonStokKontrolBaglami(stoklar, tumIlgiliStokHareketleri);
    }
}
