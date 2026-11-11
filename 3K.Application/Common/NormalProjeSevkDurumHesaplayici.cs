using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Common;

/// <summary>
/// Normal projenin sevk durumunu, listelemede kullanılan ürün/saha kurallarıyla hesaplar.
/// Yalnız okur; saha tamamlama kayıtlarını, miktarları veya sevkiyat bağlarını değiştirmez.
/// </summary>
public static class NormalProjeSevkDurumHesaplayici
{
    public static async Task<int> HesaplaAsync(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        ISahaTamamlamaService sahaTamamlamaService,
        int projeId,
        int mevcutDurumId,
        IReadOnlyCollection<Sandik> guncelSandiklar,
        IReadOnlySet<int> sahaUzerindenSevkEdilenSandikIds,
        CancellationToken cancellationToken)
    {
        // Sandıkların henüz SaveChanges yapılmamış sevk/geri alma durumu çağırandan gelir.
        // Ürünlerde yalnız hesaplamanın ihtiyaç duyduğu alanlar okunur.
        var satirlar = await readQueries.ToListAsync(
            readQueries.AsNoTracking(unitOfWork.GetRepository<CekiSatiri>().Queryable())
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .Select(cs => new CekiSatiri
                {
                    Id = cs.Id,
                    KaynakCekiSatiriId = cs.KaynakCekiSatiriId,
                    IstenenAdet = cs.IstenenAdet,
                    GelenMiktar = cs.GelenMiktar,
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    HataliMiktar = cs.HataliMiktar,
                    DurumId = cs.DurumId,
                    GridDurumuId = cs.GridDurumuId
                }),
            cancellationToken);

        // Aktif/plânlanan aktarım değil, mevcut servisin doğruladığı gerçekleşmiş saha sevki.
        var kokSatirIds = satirlar.Where(cs => !cs.KaynakCekiSatiriId.HasValue).Select(cs => cs.Id).ToList();
        var sevkEdilenSahaTamamlama = await sahaTamamlamaService.GetSevkEdilenGerceklesenTamamlamaMapAsync(
            kokSatirIds,
            cancellationToken);
        var sahaIptalKapanan = await sahaTamamlamaService.GetSahaIptalKapananMapAsync(kokSatirIds, cancellationToken);

        return Hesapla(mevcutDurumId, guncelSandiklar, satirlar,
            sahaUzerindenSevkEdilenSandikIds, sevkEdilenSahaTamamlama, sahaIptalKapanan);
    }

    /// <param name="sahaIptalKapanan">
    /// S-27: sahada Grid İptal edilen aktarılmış miktar; yalnız "bütün ürünler tamamlandı mı" kararında kapanmış sayılır,
    /// saha sevki var sayılmaz.
    /// </param>
    public static int Hesapla(
        int mevcutDurumId,
        IReadOnlyCollection<Sandik> guncelSandiklar,
        IReadOnlyCollection<CekiSatiri> satirlar,
        IReadOnlySet<int> sahaUzerindenSevkEdilenSandikIds,
        IReadOnlyDictionary<int, decimal> sevkEdilenSahaTamamlama,
        IReadOnlyDictionary<int, decimal> sahaIptalKapanan)
    {
        var sevkKapanis = SahaIptalSevkKapanisKurali.SevkKapanisHaritasi(sevkEdilenSahaTamamlama, sahaIptalKapanan);
        var fizikselSevk = guncelSandiklar.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);
        var etkinSevk = guncelSandiklar.Count(s =>
            s.DurumId == (int)SandikDurum.Sevkedildi || sahaUzerindenSevkEdilenSandikIds.Contains(s.Id));
        var sahaSevkiVar = guncelSandiklar.Any(s => sahaUzerindenSevkEdilenSandikIds.Contains(s.Id)) ||
            satirlar.Any(cs => sevkEdilenSahaTamamlama.GetValueOrDefault(cs.Id) > 0);
        var tumUrunlerTamamlandi = satirlar.Count > 0 &&
            satirlar.All(cs => CekiSatiriKalanHelper.HesaplaEtkinKalan(cs, sevkKapanis) <= 0);

        var sevkDurumu = NormalProjeSevkDurumHelper.Hesapla(
            guncelSandiklar.Count,
            fizikselSevk,
            sahaSevkiVar,
            tumUrunlerTamamlandi,
            sahaSandiklariylaTumSandiklarEtkinSevkEdildi:
                guncelSandiklar.Count > 0 && etkinSevk == guncelSandiklar.Count &&
                guncelSandiklar.Any(s => s.DurumId != (int)SandikDurum.Sevkedildi &&
                    sahaUzerindenSevkEdilenSandikIds.Contains(s.Id)));

        // Hiçbir sevk kalmadığında mevcut geri-alma davranışı korunur.
        return sevkDurumu ?? ProjeSevkDurumHelper.Hesapla(guncelSandiklar, mevcutDurumId);
    }
}
