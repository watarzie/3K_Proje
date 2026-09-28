using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Birden çok finans servisinin paylaştığı varlık → model eşlemeleri.</summary>
internal static class FinansModelEslemeleri
{
    internal static FinansIsKaydiModel MapIsKaydi(FinansIsKaydi entity)
    {
        var activeOrderLines = entity.SiparisKalemleri
            .Where(x => !x.FinansSiparis.IptalEdildi)
            .ToList();
        var activeInvoiceLines = activeOrderLines
            .SelectMany(x => x.FaturaKalemleri)
            .Where(x => !x.FinansFatura.IptalEdildi)
            .ToList();
        var orderedAdet = activeOrderLines.Sum(x => x.Adet);
        var orderedM3 = activeOrderLines.Sum(x => x.M3);
        var invoicedAdet = activeInvoiceLines.Sum(x => x.Adet);
        var invoicedM3 = activeInvoiceLines.Sum(x => x.M3);
        var money = FinansTutarKurallari.Hesapla(1, FinansTutarKurallari.IsNet(entity), entity.KdvOraniSnapshot);

        return new FinansIsKaydiModel
        {
            Id = entity.Id,
            ProjeId = entity.ProjeId,
            ProjeNo = entity.ProjeNo,
            Musteri = entity.Musteri,
            ManuelProjeMi = entity.ManuelProjeMi,
            IsAdi = entity.IsAdi,
            OzelIsTuru = entity.OzelIsTuru,
            HesaplamaYontemi = entity.HesaplamaYontemi,
            RaporGrubu = entity.RaporGrubu,
            Aciklama = entity.Aciklama,
            TalepEdenKisi = entity.TalepEdenKisi,
            TalepEdenBolum = entity.TalepEdenBolum,
            IsTuru = entity.IsTuru,
            SandikNo = entity.SandikNo,
            SandikAdi = entity.SandikAdi,
            SandikTipi = entity.SandikTipi,
            Boy = entity.Boy,
            En = entity.En,
            Yukseklik = entity.Yukseklik,
            IcSandikSablonId = entity.IcSandikSablonId,
            Adet = entity.Adet,
            Birim = entity.Birim,
            BirimM3 = entity.BirimM3,
            ToplamM3 = entity.ToplamM3,
            FinansUrunId = entity.FinansUrunId,
            FiyatlandirmaBirimi = entity.FiyatlandirmaBirimiSnapshot,
            BirimFiyat = entity.BirimFiyatSnapshot,
            ParaBirimi = entity.ParaBirimiSnapshot,
            KdvOrani = entity.KdvOraniSnapshot,
            TarifeYili = entity.TarifeYiliSnapshot,
            NetTutar = money.Net,
            KdvTutari = money.Kdv,
            ToplamTutar = money.Toplam,
            UretimTarihi = entity.UretimTarihi,
            FinansDonemi = entity.FinansDonemi,
            FinansTarihi = entity.FinansTarihi == default ? entity.FinansDonemi : entity.FinansTarihi,
            FinansTarihiManuel = entity.FinansTarihiManuel,
            FinansMiktariManuel = entity.FinansMiktariManuel,
            KaynakBileseni = entity.KaynakBileseni,
            FiyatlandirmaHazir = money.Net > 0,
            SiparisNetTutar = activeOrderLines.Sum(x => x.NetTutarSnapshot),
            FaturalananNetTutar = activeInvoiceLines.Sum(x => x.NetTutarSnapshot),
            KalanSiparisNetTutar = Math.Max(0, money.Net - activeOrderLines.Sum(x => x.NetTutarSnapshot)),
            KalanFaturaNetTutar = Math.Max(0, activeOrderLines.Sum(x => x.NetTutarSnapshot) - activeInvoiceLines.Sum(x => x.NetTutarSnapshot)),
            SablonSurumId = entity.SablonSurumId,
            ManuelNetTutar = entity.ManuelNetTutar,
            AlanDegerleri = entity.AlanDegerleriJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(entity.AlanDegerleriJson),
            Bilesenler = entity.FiyatBilesenleriJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<FinansFiyatBileseniModel[]>(entity.FiyatBilesenleriJson),
            KayitTarihi = entity.KayitTarihi,
            Durum = FinansTutarKurallari.Durum(entity),
            SiparisAdedi = orderedAdet,
            SiparisM3 = orderedM3,
            SiparisBekleyenAdet = Math.Max(0, entity.Adet - orderedAdet),
            SiparisBekleyenM3 = Math.Max(0, entity.ToplamM3 - orderedM3),
            FaturalananAdet = invoicedAdet,
            FaturalananM3 = invoicedM3,
            PoNumaralari = activeOrderLines.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
            FaturaNumaralari = activeInvoiceLines.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
            KaynakTuru = entity.KaynakTuru,
            KaynakKayitId = entity.KaynakKayitId,
            KaynakAktif = entity.KaynakAktif,
            IptalEdildi = entity.IptalEdildi,
            IptalAciklamasi = entity.IptalAciklamasi,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy
        };
    }

    internal static FinansGiderModel MapExpense(FinansGider entity) => new()
    {
        Id = entity.Id,
        Tarih = entity.Tarih,
        FinansTarihi = entity.FinansTarihi,
        BelgeNo = entity.BelgeNo,
        AvansMi = entity.AvansMi,
        MahsupEdilenAvansId = entity.MahsupEdilenAvansId,
        FinansDonemi = entity.FinansDonemi,
        KategoriId = entity.FinansGiderKategoriId,
        Kategori = entity.Kategori.Ad,
        GiderKalemiId = entity.FinansGiderKalemiId,
        GiderKalemi = entity.GiderKalemi?.Ad,
        AltKategori = entity.AltKategori,
        FirmaVeyaKisi = entity.FirmaVeyaKisi,
        Aciklama = entity.Aciklama,
        Miktar = entity.Miktar,
        Birim = entity.Birim,
        BirimFiyat = entity.BirimFiyat,
        Tutar = entity.Tutar,
        ParaBirimi = entity.ParaBirimi,
        KdvDahil = entity.KdvDahil,
        KdvOrani = entity.KdvOrani,
        Matrah = entity.Matrah,
        KdvTutari = entity.KdvTutari,
        ToplamTutar = entity.ToplamTutar,
        ProjeId = entity.ProjeId,
        ProjeNo = entity.Proje?.ProjeNo ?? entity.ManuelProjeNo ?? string.Empty,
        IsTuru = entity.IsTuru,
        IptalEdildi = entity.IptalEdildi,
        IptalAciklamasi = entity.IptalAciklamasi
    };
}
