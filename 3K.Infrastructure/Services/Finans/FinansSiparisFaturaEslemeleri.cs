using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Sipariş ve fatura → model eşlemeleri. Liste filtresi verildiğinde yalnız filtreye uyan kalemler
/// ve onların toplamları döner; belge mutabakat toplamı yalnız bütün kalemler seçiliyken kullanılır.
/// </summary>
internal static class FinansSiparisFaturaEslemeleri
{
    internal static FinansSiparisModel MapSiparis(FinansSiparis entity, FinansListeFiltre? filtre = null)
    {
        var selectedOrderLines = filtre is null
            ? entity.Kalemler.ToList()
            : entity.Kalemler.Where(x => SiparisKalemiFiltreyleEslesir(x, filtre, entity)).ToList();
        var activeInvoiceLines = selectedOrderLines.SelectMany(x => x.FaturaKalemleri)
            .Where(x => !x.FinansFatura.IptalEdildi).ToList();
        var lineModels = selectedOrderLines.Select(line =>
        {
            var invoiceLines = activeInvoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
            var invoicedAdet = invoiceLines.Sum(x => x.Adet);
            var invoicedM3 = invoiceLines.Sum(x => x.M3);
            return new FinansSiparisKalemiModel
            {
                Id = line.Id,
                IsKaydiId = line.FinansIsKaydiId,
                SandikNo = line.FinansIsKaydi.SandikNo ?? string.Empty,
                SandikAdi = line.FinansIsKaydi.SandikAdi ?? line.FinansIsKaydi.IsAdi,
                IsTuru = line.FinansIsKaydi.IsTuru,
                Adet = line.Adet,
                M3 = line.M3,
                FaturalananAdet = invoicedAdet,
                FaturalananM3 = invoicedM3,
                KalanAdet = Math.Max(0, line.Adet - invoicedAdet),
                KalanM3 = Math.Max(0, line.M3 - invoicedM3),
                FinansUrunId = line.FinansUrunId,
                UrunKodu = line.FinansUrun?.Kod ?? string.Empty,
                UrunAdi = line.FinansUrun?.Ad ?? string.Empty,
                FiyatlandirmaBirimi = line.FiyatlandirmaBirimiSnapshot,
                FiyatlandirmaMiktari = FinansTutarKurallari.FiyatlandirmaMiktari(line.FiyatlandirmaBirimiSnapshot, line.Adet, line.M3),
                BirimFiyat = line.BirimFiyatSnapshot,
                ParaBirimi = line.ParaBirimiSnapshot,
                KdvOrani = line.KdvOraniSnapshot,
                NetTutar = line.NetTutarSnapshot,
                KdvTutari = line.KdvTutariSnapshot,
                ToplamTutar = line.ToplamTutarSnapshot,
                FaturalananNetTutar = invoiceLines.Sum(x => x.NetTutarSnapshot),
                KalanFaturaNetTutar = Math.Max(0, line.NetTutarSnapshot - invoiceLines.Sum(x => x.NetTutarSnapshot)),
                TutarBazli = line.TutarBazli,
                FiyatManuelDegistirildi = line.FinansUrunId != line.FinansIsKaydi.FinansUrunId ||
                                         line.BirimFiyatSnapshot != line.FinansIsKaydi.BirimFiyatSnapshot
            };
        }).ToArray();
        var works = selectedOrderLines.Select(x => x.FinansIsKaydi).DistinctBy(x => x.Id).ToList();
        var totals = selectedOrderLines.GroupBy(x => x.ParaBirimiSnapshot)
            .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.NetTutarSnapshot), x.Sum(y => y.KdvTutariSnapshot), x.Sum(y => y.ToplamTutarSnapshot)))
            .OrderBy(x => x.ParaBirimi).ToArray();
        return new FinansSiparisModel
        {
            Id = entity.Id,
            KayitNo = entity.KayitNo,
            PoNumarasi = entity.PoNumarasi,
            ProjeNo = string.Join(", ", works.Select(x => x.ProjeNo).Distinct().Order()),
            Musteri = string.Join(", ", works.Select(x => x.Musteri).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Order()),
            IsTurleri = works.Select(x => x.IsTuru.ToString()).Distinct().Order().ToArray(),
            SiparisTarihi = entity.SiparisTarihi,
            SandikAdedi = selectedOrderLines.Sum(x => x.Adet),
            ToplamM3 = selectedOrderLines.Sum(x => x.M3),
            FaturalananAdet = activeInvoiceLines.Sum(x => x.Adet),
            FaturalananM3 = activeInvoiceLines.Sum(x => x.M3),
            KalanAdet = Math.Max(0, selectedOrderLines.Sum(x => x.Adet) - activeInvoiceLines.Sum(x => x.Adet)),
            KalanM3 = Math.Max(0, selectedOrderLines.Sum(x => x.M3) - activeInvoiceLines.Sum(x => x.M3)),
            Durum = entity.Durum,
            Aciklama = entity.Aciklama,
            Tutarlar = totals,
            Kalemler = lineModels,
            IptalEdildi = entity.IptalEdildi,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy
        };
    }

    internal static FinansFaturaModel MapFatura(FinansFatura entity, FinansListeFiltre? filtre = null)
    {
        var selectedInvoiceLines = filtre is null
            ? entity.Kalemler.ToList()
            : entity.Kalemler.Where(x => FaturaKalemiFiltreyleEslesir(x, filtre, entity)).ToList();
        var works = selectedInvoiceLines.Select(x => x.FinansSiparisKalemi.FinansIsKaydi).DistinctBy(x => x.Id).ToList();
        var calculatedTotals = selectedInvoiceLines.GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
            .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.NetTutarSnapshot), x.Sum(y => y.KdvTutariSnapshot), x.Sum(y => y.ToplamTutarSnapshot)))
            .OrderBy(x => x.ParaBirimi).ToArray();
        var totals = selectedInvoiceLines.Count == entity.Kalemler.Count &&
                     entity.BelgeNetTutarSnapshot.HasValue &&
                     entity.BelgeKdvTutariSnapshot.HasValue &&
                     entity.BelgeToplamTutarSnapshot.HasValue &&
                     !string.IsNullOrWhiteSpace(entity.BelgeParaBirimiSnapshot)
            ?
            [
                new FinansParaToplamiModel(
                    entity.BelgeParaBirimiSnapshot,
                    entity.BelgeNetTutarSnapshot.Value,
                    entity.BelgeKdvTutariSnapshot.Value,
                    entity.BelgeToplamTutarSnapshot.Value)
            ]
            : calculatedTotals;
        return new FinansFaturaModel
        {
            Kalemler = selectedInvoiceLines.Select(x => new FinansFaturaKalemiModel(x.Id, x.FinansSiparisKalemiId,
                x.FinansSiparisKalemi.FinansIsKaydiId, x.NetTutarSnapshot, x.KdvTutariSnapshot, x.ToplamTutarSnapshot,
                x.FinansSiparisKalemi.ParaBirimiSnapshot, x.TutarBazli)).ToArray(),
            Id = entity.Id,
            KayitNo = entity.KayitNo,
            FaturaNumarasi = entity.FaturaNumarasi,
            FaturaTarihi = entity.FaturaTarihi,
            SiparisId = entity.FinansSiparisId,
            PoNumarasi = entity.FinansSiparis.PoNumarasi,
            ProjeNo = string.Join(", ", works.Select(x => x.ProjeNo).Distinct().Order()),
            IsTurleri = works.Select(x => x.IsTuru.ToString()).Distinct().Order().ToArray(),
            SandikAdedi = selectedInvoiceLines.Sum(x => x.Adet),
            ToplamM3 = selectedInvoiceLines.Sum(x => x.M3),
            Durum = entity.Durum,
            Aciklama = entity.Aciklama,
            Tutarlar = totals,
            BelgeParaBirimi = entity.BelgeParaBirimiSnapshot,
            BelgeNetTutar = entity.BelgeNetTutarSnapshot,
            BelgeKdvTutari = entity.BelgeKdvTutariSnapshot,
            BelgeToplamTutar = entity.BelgeToplamTutarSnapshot,
            MutabakatFarki = entity.MutabakatFarkiSnapshot,
            MutabakatAciklamasi = entity.MutabakatAciklamasi,
            IptalEdildi = entity.IptalEdildi,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy
        };
    }

    internal static bool SiparisKalemiFiltreyleEslesir(
        FinansSiparisKalemi line,
        FinansListeFiltre filtre,
        FinansSiparis order)
    {
        var work = line.FinansIsKaydi;
        if (filtre.SandikCinsi.HasValue && work.SandikCinsi != filtre.SandikCinsi) return false;
        if (!string.IsNullOrWhiteSpace(filtre.Firma) && !work.Musteri.Contains(filtre.Firma, StringComparison.OrdinalIgnoreCase)) return false;
        if (filtre.ProjeId.HasValue && work.ProjeId != filtre.ProjeId) return false;
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo) && !string.Equals(work.ProjeNo, filtre.ProjeNo.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (filtre.IsTuru.HasValue && work.IsTuru != filtre.IsTuru) return false;
        if (filtre.Durum.HasValue && work.Durum != filtre.Durum) return false;
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi) && !string.Equals(line.ParaBirimiSnapshot, filtre.ParaBirimi.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim();
            if (!(work.TalepEdenKisi?.Contains(requester, StringComparison.OrdinalIgnoreCase) ?? false) &&
                !(work.TalepEdenBolum?.Contains(requester, StringComparison.OrdinalIgnoreCase) ?? false)) return false;
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim();
            var headerMatches = order.PoNumarasi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                order.KayitNo.Contains(search, StringComparison.OrdinalIgnoreCase);
            if (!headerMatches && !work.ProjeNo.Contains(search, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    internal static bool FaturaKalemiFiltreyleEslesir(
        FinansFaturaKalemi line,
        FinansListeFiltre filtre,
        FinansFatura invoice)
    {
        if (!SiparisKalemiFiltreyleEslesir(
                line.FinansSiparisKalemi,
                filtre with { Arama = null },
                invoice.FinansSiparis))
            return false;
        if (string.IsNullOrWhiteSpace(filtre.Arama))
            return true;

        var search = filtre.Arama.Trim();
        return invoice.FaturaNumarasi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               SiparisKalemiFiltreyleEslesir(
                   line.FinansSiparisKalemi,
                   new FinansListeFiltre(Arama: search),
                   invoice.FinansSiparis);
    }
}
