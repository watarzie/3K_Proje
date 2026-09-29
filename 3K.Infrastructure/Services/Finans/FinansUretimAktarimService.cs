using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Üretim kayıtlarının finansa aktarımı: kaynak anahtarıyla oluşturma, güncelleme ve pasifleştirme; sipariş geçmişi olan kayıtta miktar ve fiyat korumaları.</summary>
internal sealed class FinansUretimAktarimService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    FinansFiyatBelirleyici fiyatlar) : IFinansUretimAktarimService
{
    public async Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
        IReadOnlyList<FinansUretimAktarimModel> modeller,
        CancellationToken cancellationToken)
    {
        if (modeller.Count == 0) return new FinansSenkronizasyonSonucModel(0, 0, 0);
        modeller = modeller.SelectMany(model => model.SarfM3.HasValue && model.KaynakBileseni == "NET"
            ? new[] { model, model with { KaynakBileseni = "SARF", IsTuru = FinansIsTuru.SarfKereste,
                IsAdi = $"Sarf Kereste - {model.SandikNo}", BirimM3 = model.Adet > 0 ? model.SarfM3.Value / model.Adet : 0,
                KaynakAktif = model.KaynakAktif && model.UretimM3Hesaplanabilir && model.SarfM3 > 0, SarfM3 = null } }
            : new[] { model }).ToArray();
        var duplicate = modeller.GroupBy(x => new { Tur = x.KaynakTuru.Trim().ToUpperInvariant(), Id = x.KaynakKayitId.Trim(), x.KaynakBileseni })
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw IsKuraliException.Cakisma("Aynı aktarım paketinde yinelenen üretim kaynak anahtarı bulunuyor.");

        return await islem.AtomikAsync(
            () => UretimKayitlariniAktarCoreAsync(modeller, cancellationToken), cancellationToken);
    }

    private async Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarCoreAsync(
        IReadOnlyList<FinansUretimAktarimModel> modeller,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var deactivated = 0;
        foreach (var model in modeller)
        {
            var sourceType = model.KaynakTuru.Trim().ToUpperInvariant();
            var sourceId = model.KaynakKayitId.Trim();
            var component = model.KaynakBileseni.Trim().ToUpperInvariant();
            if (sourceType.Length == 0 || sourceId.Length == 0)
                throw IsKuraliException.Cakisma("Üretim aktarımında kaynak türü ve kaynak kayıt kimliği zorunludur.");

            var entity = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(
                x => x.KaynakTuru == sourceType && x.KaynakKayitId == sourceId && x.KaynakBileseni == component,
                cancellationToken);
            if (entity is null)
            {
                if (await context.Set<FinansKaynakBastirma>().AnyAsync(x => x.KaynakTuru == sourceType && x.KaynakKayitId == sourceId && x.KaynakBileseni == component, cancellationToken))
                    continue;
                // Üretime hiç alınmamış/pasif bir kaynak Finans'ta hayalet
                // kayıt oluşturmamalı. Daha önce aktarılmış kayıtlarda false
                // güncellemesi aşağıdaki mevcut-entity akışında işlenir.
                if (!model.KaynakAktif)
                    continue;
                entity = new FinansIsKaydi
                {
                    KaynakTuru = sourceType,
                    KaynakKayitId = sourceId,
                    KaynakBileseni = component,
                    KayitTarihi = TurkeyTime.Now
                };
                ApplyProductionValues(entity, model);
                // Üretim verisi her değiştiğinde fiyatı yeniden hesaplamak tarihsel
                // snapshot'ı bozar. Yalnız daha önce eşleşmeyen/fiyatlanmayan ve henüz
                // siparişe bağlanmayan kayıt, sonradan tanımlanan tarifeyle tamamlanır.
                if (entity.BirimFiyatSnapshot <= 0 &&
                    !entity.TarifeYiliSnapshot.HasValue &&
                    !entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
                {
                    await fiyatlar.OtomatikFiyatUygulaAsync(entity, cancellationToken);
                }
                context.Set<FinansIsKaydi>().Add(entity);
                await context.SaveChangesAsync(cancellationToken);
                denetim.Ekle(nameof(FinansIsKaydi), entity.Id, "Üretim Aktarımı", "*", null, "Kayıt oluşturuldu");
                created++;
            }
            else
            {
                var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
                var oldActive = entity.KaynakAktif;
                var activeOrderLines = entity.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
                var orderedAdet = activeOrderLines.Sum(x => x.Adet);
                var orderedM3 = activeOrderLines.Sum(x => x.M3);
                var requestedM3 = decimal.Round(model.Adet * model.BirimM3, 6);
                var pricingUnit = activeOrderLines.Select(x => x.FiyatlandirmaBirimiSnapshot).Distinct().Count() == 1
                    ? activeOrderLines[0].FiyatlandirmaBirimiSnapshot
                    : entity.FiyatlandirmaBirimiSnapshot;
                if (!entity.FinansMiktariManuel && model.KaynakAktif && FinansMiktarKurallari.KapasiteAsiliyor(
                        pricingUnit,
                        model.Adet,
                        requestedM3,
                        orderedAdet,
                        orderedM3,
                        activeOrderLines.Count))
                    throw IsKuraliException.Cakisma($"{sourceType}/{sourceId} üretim miktarı aktif sipariş miktarının altına düşürülemez.");
                // İptal edilmiş olsa bile oluşturulmuş bir PO, fiyat snapshot'ının
                // tarihsel olarak kilitlendiği anlamına gelir. Yalnız aktif PO'ya
                // bakmak, iptalden sonraki senkronizasyonda eski fiyatı değiştirebilirdi.
                var hasFinancialHistory = entity.SiparisKalemleri.Count > 0;
                var priceMatchChanged = ProductionPriceMatchChanged(entity, model);
                if (model.KaynakAktif && hasFinancialHistory && priceMatchChanged)
                    throw IsKuraliException.Cakisma(
                        $"{sourceType}/{sourceId} kaydının tür/eşleşme veya finans dönemi değişikliği mevcut PO fiyat snapshot'ını etkiliyor. " +
                        "Önce finansal belgeleri uzlaştırın ya da mevcut kaydı koruyun.");

                ApplyProductionValues(entity, model);
                if (model.KaynakAktif && FinansTutarKurallari.SiparisNet(entity) > FinansTutarKurallari.IsNet(entity))
                    throw IsKuraliException.Cakisma("Kaynak güncellemesi aktif PO net toplamını iş bedelinin üzerine çıkaramaz.");
                if (!hasFinancialHistory && !entity.FinansMiktariManuel && priceMatchChanged && entity.BirimFiyatSnapshot <= 0)
                {
                    FinansFiyatBelirleyici.FiyatsizBirak(entity);
                    await fiyatlar.OtomatikFiyatUygulaAsync(entity, cancellationToken);
                }
                else if (entity.BirimFiyatSnapshot <= 0 &&
                         !entity.FinansMiktariManuel &&
                         !entity.TarifeYiliSnapshot.HasValue &&
                         !hasFinancialHistory)
                {
                    await fiyatlar.OtomatikFiyatUygulaAsync(entity, cancellationToken);
                }
                denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), entity, auditBefore);
                if (oldActive && !entity.KaynakAktif && hasFinancialHistory)
                {
                    denetim.Ekle(
                        nameof(FinansIsKaydi),
                        entity.Id,
                        "Üretim Kaynağı Pasifleştirme",
                        nameof(entity.KaynakAktif),
                        true,
                        false,
                        "Kayda bağlı PO/fatura geçmişi korundu; üretim iptali finansal belgeleri değiştirmedi.");
                }
                if (oldActive && !entity.KaynakAktif) deactivated++;
                else updated++;
            }

            entity.Durum = FinansTutarKurallari.Durum(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        return new FinansSenkronizasyonSonucModel(created, updated, deactivated);
    }

    private static void ApplyProductionValues(FinansIsKaydi entity, FinansUretimAktarimModel model)
    {
        var finansIptali = entity.IptalEdildi && entity.KaynakAktif;
        entity.ProjeId = model.ProjeId;
        entity.ProjeNo = model.ProjeNo.Trim();
        entity.Musteri = model.Musteri.Trim();
        entity.ManuelProjeMi = !model.ProjeId.HasValue;
        entity.IsTuru = model.IsTuru;
        entity.IsAdi = model.IsAdi.Trim();
        entity.Aciklama = model.Aciklama?.Trim();
        entity.TalepEdenKisi = model.TalepEdenKisi?.Trim();
        entity.TalepEdenBolum = model.TalepEdenBolum?.Trim();
        entity.SandikNo = model.SandikNo?.Trim();
        entity.SandikAdi = model.SandikAdi?.Trim();
        entity.SandikTipi = model.SandikTipi?.Trim();
        entity.Boy = model.Boy;
        entity.En = model.En;
        entity.Yukseklik = model.Yukseklik;
        entity.IcSandikSablonId = model.IcSandikSablonId;
        entity.SandikCinsi = model.SandikCinsi;
        if (!entity.FinansMiktariManuel)
        {
            entity.Adet = model.Adet;
            entity.Birim = model.BirimM3 > 0 ? "m³" : "Adet";
            entity.BirimM3 = model.UretimM3Hesaplanabilir ? model.BirimM3 : 0;
            entity.ToplamM3 = decimal.Round(entity.Adet * entity.BirimM3, 6);
        }
        entity.UretimTarihi = model.UretimTarihi;
        // Finans tarihi elle değiştirilmediyse üretimden gelir (A6: formda girilen tarih önceliklidir).
        // PO'ya bağlanmış kayıt ayından sessizce taşınmaz; bunun için izinli Finans Tarihi değişikliği gerekir.
        if (!entity.FinansTarihiManuel && !entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
        {
            entity.FinansTarihi = (model.FinansTarihi ?? model.UretimTarihi).Date;
            entity.FinansDonemi = FinansTarihKurallari.AyBasi(entity.FinansTarihi);
        }
        entity.KaynakAktif = model.KaynakAktif;
        if (!finansIptali)
        {
            entity.IptalEdildi = !model.KaynakAktif;
            entity.IptalTarihi = model.KaynakAktif ? null : TurkeyTime.Now;
            entity.IptalAciklamasi = model.KaynakAktif ? null : "Üretim kaynağı pasifleştirildi.";
        }
    }

    private static bool ProductionPriceMatchChanged(FinansIsKaydi entity, FinansUretimAktarimModel model)
        => entity.IsTuru != model.IsTuru ||
           !string.Equals(entity.SandikAdi?.Trim(), model.SandikAdi?.Trim(), StringComparison.OrdinalIgnoreCase) ||
           !string.Equals(entity.SandikTipi?.Trim(), model.SandikTipi?.Trim(), StringComparison.OrdinalIgnoreCase) ||
           entity.Boy != model.Boy || entity.En != model.En || entity.Yukseklik != model.Yukseklik ||
           entity.IcSandikSablonId != model.IcSandikSablonId;
}
