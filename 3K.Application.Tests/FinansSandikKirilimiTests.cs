using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Finans İş Takibi proje → sandık kırılımı (P6): sandık başına satır (NET + SARF aynı sandıkta), toplamlar ve en düşük
/// iş durumu, sandık no filtresi ve projeye bağlı özel işlerin proje bloğunda listelenmesi.
/// </summary>
public sealed class FinansSandikKirilimiTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 9, 20);

    [Fact]
    public void IkiSandik_NetVeSarf_IkiSatirToplanir_HacimYalnizSandigin_DurumEnDusuk()
    {
        var satirlar = FinansSandikKirilimiKurali.Olustur(
        [
            Kayit(1, "10", "K-10", net: 400m, siparis: 400m, fatura: 400m, m3: 2m, po: ["PO-2"], ft: ["FT-1"]),
            Kayit(2, "10", "K-10", net: 50m, siparis: 50m, fatura: 0m, m3: 0.4m, sarf: true, po: ["PO-1"]),
            Kayit(3, "2", "K-2", net: 300m, siparis: 0m, fatura: 0m, m3: 1.5m),
            Kayit(4, "2", "K-2", net: 0m, siparis: 0m, fatura: 0m, m3: 0.3m, sarf: true),
        ]);

        Assert.Equal(["2", "10"], satirlar.Select(x => x.SandikNo));
        var on = satirlar[1];
        Assert.Equal((2, 450m, 450m, 400m), (on.KayitSayisi, on.NetTutar, on.SiparisNetTutar, on.FaturalananNetTutar));
        Assert.Equal((1m, 2m, 2m, 0m), (on.Adet, on.ToplamM3, on.AhsapM3, on.KontraM3));
        Assert.Equal(FinansIsDurumu.SiparisAcildi, on.Durum);
        Assert.Equal(["PO-1", "PO-2"], on.PoNumaralari);
        Assert.Equal(["FT-1"], on.FaturaNumaralari);
        Assert.Equal(["NET", "SARF"], on.Kayitlar.Select(x => x.KaynakBileseni));
        Assert.Equal([FinansIsDurumu.Faturalandi, FinansIsDurumu.SiparisAcildi], on.Kayitlar.Select(x => x.Durum));
        Assert.Equal(FinansSandikMalzemesi.Ahsap, on.SandikMalzemesi);

        var iki = satirlar[0];
        Assert.Equal((FinansIsDurumu.SiparisBekliyor, 1, 1.5m, 300m), (iki.Durum, iki.FiyatBekleyenKayitSayisi, iki.ToplamM3, iki.NetTutar));
    }

    [Fact]
    public void IptalKaydi_ToplamaGirmez_HepsiIptalseSandikIptal_ParaBirimiAyriSatir()
    {
        var satirlar = FinansSandikKirilimiKurali.Olustur(
        [
            Kayit(1, "1", "K-1", net: 100m, siparis: 0m, fatura: 0m, m3: 1m),
            Kayit(2, "1", "K-1", net: 70m, siparis: 0m, fatura: 0m, m3: 1m, aktif: false),
            Kayit(3, "1", "K-1", net: 20m, siparis: 0m, fatura: 0m, m3: 0.2m, para: "TRY"),
            Kayit(4, "3", "K-3", net: 90m, siparis: 0m, fatura: 0m, m3: 1m, aktif: false),
            Kayit(5, null, null, net: 10m, siparis: 0m, fatura: 0m, m3: 0m),
        ]);

        Assert.Equal([("1", "EUR"), ("1", "TRY"), ("3", "EUR"), (null, "EUR")],
            satirlar.Select(x => (x.SandikNo, x.ParaBirimi)));
        var eur = satirlar[0];
        Assert.Equal((2, 100m, 1m), (eur.KayitSayisi, eur.NetTutar, eur.ToplamM3));
        Assert.False(eur.Kayitlar.Single(x => x.Id == 2).Aktif);
        Assert.Equal(FinansIsDurumu.IptalEdildi, eur.Kayitlar.Single(x => x.Id == 2).Durum);
        Assert.Equal((FinansIsDurumu.IptalEdildi, 0m), (satirlar[2].Durum, satirlar[2].NetTutar));
        Assert.Equal(satirlar.Select(x => x.Anahtar).Distinct().Count(), satirlar.Count);
    }

    [Theory]
    [InlineData(null, null, null, null, false)]
    [InlineData(5, null, null, null, true)]
    [InlineData(null, "PA-1", null, null, true)]
    [InlineData(5, null, 2026, null, false)]
    [InlineData(5, null, 2026, 13, false)]
    [InlineData(5, null, 2026, 9, true)]
    public void Dogrulayici_ProjeVeDonemIster(int? projeId, string? projeNo, int? yil, int? ay, bool gecerli)
        => Assert.Equal(gecerli, new FinansProjeSandikKirilimiQueryValidator()
            .Validate(new FinansProjeSandikKirilimiQuery { ProjeId = projeId, ProjeNo = projeNo, Yil = yil, Ay = ay }).IsValid);

    [Fact]
    public void Sorgu_IsTakibiSekmesiVeKayitGoruntulemeOkumaIzniIster()
    {
        var sorgu = new FinansProjeSandikKirilimiQuery();
        Assert.Equal(YetkiKodlari.Finans.KayitGoruntule, sorgu.RequiredMenuKod);
        Assert.Equal(
            [(YetkiKodlari.Finans.SekmeIsTakibi, YetkiTipi.R), (YetkiKodlari.Finans.KayitGoruntule, YetkiTipi.R)],
            sorgu.RequiredMenuPermissions.Select(x => (x.MenuKod, x.YetkiTipi)));
    }

    /// <summary>Kırılım yanıtı da HTTP sınırında maskelenir: tutar ve m³ null, sayaçlar açık; sarf kaydının m³'ü sarf iznine bağlı.</summary>
    [Fact]
    public void KirilimYaniti_AlanMaskesineUyar()
    {
        var model = new FinansProjeSandikKirilimiModel
        {
            ProjeNo = "PA-1",
            KayitSayisi = 2,
            Sandiklar = FinansSandikKirilimiKurali.Olustur(
            [
                Kayit(1, "1", "K-1", net: 100m, siparis: 0m, fatura: 0m, m3: 2m),
                Kayit(2, "1", "K-1", net: 10m, siparis: 0m, fatura: 0m, m3: 0.5m, sarf: true),
            ])
        };
        var json = JsonSerializer.SerializeToNode(model, JsonSerializerOptions.Web)!;
        FinansAlanProjeksiyonu.Uygula(json, Tam with { Tutar = false, Sarf = false });
        var sandik = json["sandiklar"]![0]!;
        Assert.Null(sandik["netTutar"]);
        Assert.Null(sandik["siparisNetTutar"]);
        Assert.Null(sandik["faturalananNetTutar"]);
        Assert.Equal(2m, sandik["toplamM3"]!.GetValue<decimal>());
        Assert.Equal(2, sandik["kayitSayisi"]!.GetValue<int>());
        Assert.Equal(2, json["kayitSayisi"]!.GetValue<int>());
        var sarf = sandik["kayitlar"]![1]!;
        Assert.Null(sarf["toplamM3"]);
        Assert.Null(sarf["ahsapM3"]);
        Assert.Equal(2m, sandik["kayitlar"]![0]!["toplamM3"]!.GetValue<decimal>());

        json = JsonSerializer.SerializeToNode(model, JsonSerializerOptions.Web)!;
        FinansAlanProjeksiyonu.Uygula(json, Tam with { UretimM3 = false });
        Assert.Null(json["sandiklar"]![0]!["toplamM3"]);
        Assert.Null(json["sandiklar"]![0]!["kontraM3"]);
        Assert.Equal(110m, json["sandiklar"]![0]!["netTutar"]!.GetValue<decimal>());
    }

    /// <summary>
    /// Gerçek SQL: iki sandık × NET + SARF, PO/fatura ve iptal; kırılım dönem ve proje anahtarıyla sınırlanır, 250 sınırı
    /// yoktur; genel arama sandık no ile süzülür; projeye bağlı özel iş aylık sonuçta proje bloğuna da gelir.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_KirilimSandikBasinaToplanir_SandikNoFiltresi_ProjeOzelIsleri()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kaynak10 = Guid.NewGuid().ToString("D");
            var kaynak2 = Guid.NewGuid().ToString("D");
            var net10 = Sandik("10", 2m, kaynak10);
            var sarf10 = Sandik("10", 0.5m, kaynak10, sarf: true);
            var net2 = Sandik("2", 1.5m, kaynak2);
            var sarf2 = Sandik("2", 0.25m, kaynak2, sarf: true);
            var iptal2 = Sandik("2", 1m, Guid.NewGuid().ToString("D"));
            iptal2.IptalEdildi = true;
            iptal2.IptalAciklamasi = "Mükerrer";
            var ekim = Sandik("2", 3m, Guid.NewGuid().ToString("D"), tarih: new DateTime(2026, 10, 5));
            var baskaProje = Sandik("1", 9m, Guid.NewGuid().ToString("D"));
            baskaProje.ProjeNo = "PA-DIGER";
            var cokluSandik = Enumerable.Range(100, 260).Select(i => Sandik(i.ToString(), 0.1m, Guid.NewGuid().ToString("D"))).ToArray();
            var ozelIs = new FinansIsKaydi
            {
                ProjeNo = "PA727-03", Musteri = "GE Vernova", IsTuru = FinansIsTuru.OzelIs, IsAdi = "Forklift kiralama",
                RaporGrubu = "Özel İş", Adet = 1, Birim = "Adet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar,
                BirimFiyatSnapshot = 75m, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, UretimTarihi = Tarih,
                FinansTarihi = Tarih, FinansDonemi = FinansTarihKurallari.AyBasi(Tarih), KayitTarihi = Tarih,
                KaynakTuru = "Manuel", KaynakAktif = true
            };
            db.AddRange(net10, sarf10, net2, sarf2, iptal2, ekim, baskaProje, ozelIs);
            db.AddRange(cokluSandik);
            await db.SaveChangesAsync();
            var finans = Finans(db);
            var po = await finans.SiparisFatura.SiparisOlusturAsync(
                new("PO-10", Tarih, "QA", [new(net10.Id, 0, 0, NetTutar: 700m), new(sarf10.Id, 0, 0, NetTutar: 100m)], "EUR", 800m), Ct);
            await finans.SiparisFatura.FaturaOlusturAsync(
                new(po.Id, "FT-10", Tarih, "QA", [new(po.Kalemler.Single(x => x.IsKaydiId == net10.Id).Id, 0, 0, 700m)], "EUR", 700m, 140m, 840m), Ct);
            db.ChangeTracker.Clear();

            var kirilim = await finans.AylikOzet.ProjeSandikKirilimiAsync(
                new(null, "PA727-03", "GE Vernova", 2026, 9, new FinansListeFiltre()), Ct);
            Assert.Equal(("PA727-03", "GE Vernova"), (kirilim.ProjeNo, kirilim.Musteri));
            Assert.Equal(4 + cokluSandik.Length, kirilim.KayitSayisi);
            Assert.Equal(2 + cokluSandik.Length, kirilim.Sandiklar.Count);
            Assert.Equal(["2", "10", "100"], kirilim.Sandiklar.Take(3).Select(x => x.SandikNo));
            var on = kirilim.Sandiklar[1];
            Assert.Equal((2, 800m, 800m, 700m, 2m, 2m), (on.KayitSayisi, on.NetTutar, on.SiparisNetTutar, on.FaturalananNetTutar, on.ToplamM3, on.AhsapM3));
            Assert.Equal(FinansIsDurumu.SiparisAcildi, on.Durum);
            Assert.Equal(["PO-10"], on.PoNumaralari);
            Assert.Equal(["FT-10"], on.FaturaNumaralari);
            Assert.Equal([(net10.Id, "NET", FinansIsDurumu.Faturalandi), (sarf10.Id, "SARF", FinansIsDurumu.SiparisAcildi)],
                on.Kayitlar.Select(x => (x.Id, x.KaynakBileseni, x.Durum)));
            var iki = kirilim.Sandiklar[0];
            Assert.Equal((2, 575m, 1.5m, FinansIsDurumu.SiparisBekliyor), (iki.KayitSayisi, iki.NetTutar, iki.ToplamM3, iki.Durum));

            var iptalli = await finans.AylikOzet.ProjeSandikKirilimiAsync(
                new(null, "PA727-03", "GE Vernova", 2026, 9, new FinansListeFiltre(IptalEdilenleriDahilEt: true)), Ct);
            var ikiIptalli = iptalli.Sandiklar.Where(x => x.SandikNo == "2").ToList();
            Assert.Equal(2, ikiIptalli.Count);
            Assert.Contains(ikiIptalli, x => x.Durum == FinansIsDurumu.IptalEdildi && x.KayitSayisi == 1 && x.NetTutar == 0m);

            var tumDonemler = await finans.AylikOzet.ProjeSandikKirilimiAsync(
                new(null, "PA727-03", null, null, null, new FinansListeFiltre(SandikNo: "2")), Ct);
            Assert.All(tumDonemler.Sandiklar, x => Assert.Contains("2", x.SandikNo));
            // Ekim'deki ayrı üretim kaydı aynı sandık no ile ayrı satırdır (kaynak üretim kaydı farklı).
            var ikiTum = tumDonemler.Sandiklar.Where(x => x.SandikNo == "2").ToList();
            Assert.Equal([1.5m, 3m], ikiTum.Select(x => x.ToplamM3).Order());
            Assert.Equal(new[] { net2.Id, sarf2.Id, ekim.Id }.Order(), ikiTum.SelectMany(x => x.Kayitlar).Select(x => x.Id).Order());

            var arama = await finans.Panel.GenelAramaAsync(new(PageSize: 250, ProjeNo: "PA727-03", SandikNo: "10"), Ct);
            Assert.Contains(arama.Items, x => x.Id == net10.Id);
            Assert.All(arama.Items, x => Assert.Contains("10", x.SandikNo));

            var aylik = await finans.AylikOzet.AylikOzetSayfaliAsync(2026, 9, new(), Ct);
            var projeOzelIs = Assert.Single(aylik.ProjeOzelIsleri);
            Assert.Equal((ozelIs.Id, "M:PA727-03\u001fGE Vernova"), (projeOzelIs.OzelIsId, projeOzelIs.ProjeBirimAnahtari));
            Assert.Contains(aylik.Projeler, x => x.ProjeBirimAnahtari == projeOzelIs.ProjeBirimAnahtari);
        });
    }

    private static readonly AlanErisimYetkileri Tam = new(true, true, true, true, true, true, true, true, true);

    private static FinansSandikKirilimKaydi Kayit(
        int id, string? sandikNo, string? kaynak, decimal net, decimal siparis, decimal fatura, decimal m3,
        bool sarf = false, bool aktif = true, string para = "EUR", string[]? po = null, string[]? ft = null) =>
        new(id, sarf ? FinansIsTuru.SarfKereste : FinansIsTuru.AnaAmbalaj, sarf ? "SARF" : "NET", kaynak, sandikNo,
            sandikNo is null ? null : $"Sandık {sandikNo}", "Ahşap Kapalı", AmbalajSandikCinsi.AhsapKapali,
            sarf ? "Sarf kereste" : "Normal Ambalaj", Tarih.AddDays(id), 1, m3, sarf ? 0 : m3, 0, para,
            net, siparis, fatura, aktif, po ?? [], ft ?? []);

    private static FinansIsKaydi Sandik(string no, decimal m3, string kaynak, bool sarf = false, DateTime? tarih = null)
    {
        var gun = tarih ?? Tarih;
        return new FinansIsKaydi
        {
            ProjeNo = "PA727-03", Musteri = "GE Vernova", IsTuru = sarf ? FinansIsTuru.SarfKereste : FinansIsTuru.AnaAmbalaj,
            IsAdi = sarf ? $"Sarf Kereste - {no}" : $"Normal Ambalaj - {no}", SandikNo = no, SandikAdi = $"Sandık {no}",
            SandikTipi = "Ahşap Kapalı", SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
            Adet = 1, Birim = "m³", BirimM3 = m3, ToplamM3 = m3,
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup, BirimFiyatSnapshot = sarf ? 200m : 350m,
            ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, TarifeYiliSnapshot = 2026,
            UretimTarihi = gun, FinansTarihi = gun, FinansDonemi = FinansTarihKurallari.AyBasi(gun), KayitTarihi = gun,
            KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = kaynak,
            KaynakBileseni = sarf ? "SARF" : "NET", KaynakAktif = true
        };
    }

    private static FinansTestServisleri Finans(AppDbContext db) => new(db, new Kullanici());

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
