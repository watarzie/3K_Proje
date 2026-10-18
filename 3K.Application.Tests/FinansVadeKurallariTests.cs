using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

/// <summary>Vade/tahsilat kuralı (D3), para birimi tanımı (F1), yeni finans isteklerinin sekme izinleri.</summary>
public sealed class FinansVadeKurallariTests
{
    private static readonly DateTime Bugun = new(2026, 9, 29);

    [Theory]
    [InlineData(false, -1, null, 7, FinansOdemeDurumu.VadesiGecti)]
    [InlineData(false, 0, null, 7, FinansOdemeDurumu.VadesiYaklasiyor)]
    [InlineData(false, 7, null, 7, FinansOdemeDurumu.VadesiYaklasiyor)]
    [InlineData(false, 8, null, 7, FinansOdemeDurumu.Bekliyor)]
    [InlineData(false, 0, null, 0, FinansOdemeDurumu.VadesiYaklasiyor)]
    [InlineData(false, 1, null, 0, FinansOdemeDurumu.Bekliyor)]
    [InlineData(true, 0, 0, 7, FinansOdemeDurumu.ZamanindaOdendi)]
    [InlineData(true, 5, -3, 7, FinansOdemeDurumu.ZamanindaOdendi)]
    [InlineData(true, -4, 0, 7, FinansOdemeDurumu.GecOdendi)]
    public void Odeme_durumu_vade_ve_odeme_isaretinden_turetilir(bool odendi, int vadeGun, int? odemeGun, int uyariGunu, FinansOdemeDurumu beklenen)
        => Assert.Equal(beklenen, FinansVadeKurallari.Durum(odendi, Bugun.AddDays(vadeGun),
            odemeGun.HasValue ? Bugun.AddDays(odemeGun.Value) : null, Bugun.AddHours(17), uyariGunu));

    [Theory]
    [InlineData(false, 10, null, 10)]
    [InlineData(false, -3, null, -3)]
    [InlineData(true, -3, 0, 3)]
    [InlineData(true, 4, 0, -4)]
    public void Vade_farki_odenmemiste_kalan_odenende_gecikme_gunudur(bool odendi, int vadeGun, int? odemeGun, int beklenen)
        => Assert.Equal(beklenen, FinansVadeKurallari.VadeFarkiGun(odendi, Bugun.AddDays(vadeGun),
            odemeGun.HasValue ? Bugun.AddDays(odemeGun.Value) : null, Bugun));

    [Theory]
    [InlineData(5, "Vadesi gelmedi")]
    [InlineData(0, "Vadesi gelmedi")]
    [InlineData(-1, "1–30")]
    [InlineData(-30, "1–30")]
    [InlineData(-31, "31–60")]
    [InlineData(-60, "31–60")]
    [InlineData(-61, "61–90")]
    [InlineData(-90, "61–90")]
    [InlineData(-91, "90+")]
    public void Gecikme_gruplari_cakismaz(int vadeFarkiGun, string grup)
        => Assert.Equal(grup, FinansVadeKurallari.GecikmeGrubu(vadeFarkiGun));

    [Fact]
    public void Vade_ve_odeme_tarihi_kurallari()
    {
        Assert.Equal(Bugun.AddDays(30), FinansVadeKurallari.VadeTarihi(Bugun.AddHours(9), 30));
        Assert.Equal(Bugun, FinansVadeKurallari.VadeDogrula(Bugun, Bugun.AddHours(3)));
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.VadeDogrula(Bugun, Bugun.AddDays(-1)));
        Assert.Null(FinansVadeKurallari.OdemeTarihiDogrula(false, Bugun, Bugun));
        Assert.Equal(Bugun, FinansVadeKurallari.OdemeTarihiDogrula(true, Bugun.AddHours(10), Bugun));
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.OdemeTarihiDogrula(true, null, Bugun));
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.OdemeTarihiDogrula(true, Bugun.AddDays(1), Bugun));
        FinansVadeKurallari.VadeGunuDogrula(0);
        FinansVadeKurallari.VadeGunuDogrula(FinansVadeKurallari.EnFazlaVadeGunu);
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.VadeGunuDogrula(FinansVadeKurallari.EnFazlaVadeGunu + 1));
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.UyariGunuDogrula(-1));
        Assert.Throws<IsKuraliException>(() => FinansVadeKurallari.UyariGunuDogrula(FinansVadeKurallari.EnFazlaUyariGunu + 1));
    }

    /// <summary>§10: listedeki ödeme filtresi (SQL ifadesi) ile model üzerindeki durum kuralı aynı faturaları seçer.</summary>
    [Fact]
    public void Odeme_filtresi_ifadesi_durum_kuraliyla_ayni_faturalari_secer()
    {
        var faturalar = Enumerable.Range(-12, 25).SelectMany(gun => new[]
        {
            new FinansFatura { VadeTarihi = Bugun.AddDays(gun) },
            new FinansFatura { VadeTarihi = Bugun.AddDays(gun), OdendiMi = true, OdemeTarihi = Bugun.AddDays(-2) }
        }).Select((fatura, sira) => { fatura.Id = sira + 1; return fatura; }).ToList();
        var eslesmeler = new (FinansOdemeFiltresi Filtre, FinansOdemeDurumu[] Durumlar)[]
        {
            (FinansOdemeFiltresi.VadesiGecen, [FinansOdemeDurumu.VadesiGecti]),
            (FinansOdemeFiltresi.VadesiYaklasan, [FinansOdemeDurumu.VadesiYaklasiyor]),
            (FinansOdemeFiltresi.Odenmedi, [FinansOdemeDurumu.Bekliyor, FinansOdemeDurumu.VadesiYaklasiyor, FinansOdemeDurumu.VadesiGecti]),
            (FinansOdemeFiltresi.Odendi, [FinansOdemeDurumu.ZamanindaOdendi, FinansOdemeDurumu.GecOdendi])
        };
        foreach (var uyariGunu in new[] { 0, 7 })
        foreach (var (filtre, durumlar) in eslesmeler)
        {
            var sorgu = FinansSorgulari.OdemeFiltresi(faturalar.AsQueryable(), filtre, Bugun.AddHours(15), uyariGunu).Select(x => x.Id).Order();
            var kural = faturalar.Where(x => durumlar.Contains(FinansVadeKurallari.Durum(x.OdendiMi, x.VadeTarihi, x.OdemeTarihi, Bugun, uyariGunu)))
                .Select(x => x.Id).Order();
            Assert.Equal(kural, sorgu);
        }
        Assert.Equal(faturalar.Count, FinansSorgulari.OdemeFiltresi(faturalar.AsQueryable(), null, Bugun, 7).Count());
    }

    [Fact]
    public void Istek_dogrulayicilari_vade_odeme_ve_para_birimi_girdisini_sinirlar()
    {
        var odeme = new FinansFaturaOdemeCommandValidator();
        Assert.False(odeme.Validate(new FinansFaturaOdemeCommand { Id = 1, Model = new(true, null, null) }).IsValid);
        Assert.True(odeme.Validate(new FinansFaturaOdemeCommand { Id = 1, Model = new(true, Bugun, "Dekont") }).IsValid);
        Assert.True(odeme.Validate(new FinansFaturaOdemeCommand { Id = 1, Model = new(false, null, null) }).IsValid);

        var fatura = new FinansFaturaOlusturModelValidator();
        FinansFaturaOlusturModel Fatura(DateTime? vade) => new(1, "F-1", Bugun, null, [new(1, 0, 0, 10m)], VadeTarihi: vade);
        Assert.True(fatura.Validate(Fatura(null)).IsValid);
        Assert.True(fatura.Validate(Fatura(Bugun)).IsValid);
        Assert.False(fatura.Validate(Fatura(Bugun.AddDays(-1))).IsValid);

        var paraBirimi = new FinansParaBirimiKaydetModelValidator();
        Assert.True(paraBirimi.Validate(new FinansParaBirimiKaydetModel("usd", "ABD Doları", "$", true, 3)).IsValid);
        Assert.False(paraBirimi.Validate(new FinansParaBirimiKaydetModel("US", "Kısa", null, true, 3)).IsValid);
        Assert.False(paraBirimi.Validate(new FinansParaBirimiKaydetModel("U$D", "Simge", null, true, 3)).IsValid);
        Assert.False(paraBirimi.Validate(new FinansParaBirimiKaydetModel("USD", "", null, true, 3)).IsValid);

        var parametre = new FinansParametreleriKaydetCommandValidator();
        Assert.True(parametre.Validate(new FinansParametreleriKaydetCommand { Model = new(30, 7) }).IsValid);
        Assert.False(parametre.Validate(new FinansParametreleriKaydetCommand { Model = new(366, 7) }).IsValid);
        Assert.False(parametre.Validate(new FinansParametreleriKaydetCommand { Model = new(30, 91) }).IsValid);
    }
}
