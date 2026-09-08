using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class CekiRevizyonAktifSahaKaynakKorumaTests
{
    [Fact]
    public async Task Onizleme_AktifNormalKaynakU_SahaEngeliIleIsaretlenir()
    {
        var koruma = new KorumaStub(11);
        var satir = OnizlemeSatiri(11, "U");

        await OnizlemeyiKontrolEtAsync(koruma, ProjeTipi.Normal, [satir], [KaynakSatir(11)]);

        Assert.False(satir.UygulanabilirMi);
        Assert.Equal("Engel", satir.RiskSeviyesi);
        var sorun = Assert.Single(satir.Sorunlar);
        Assert.Equal(CekiRevizyonSorunKodlari.AktifSahaAktarimi, sorun.Kod);
        Assert.Equal(CekiRevizyonSorunKategorileri.DurumCakismasi, sorun.Kategori);
        Assert.Contains("U satırı", sorun.Mesaj);
        Assert.Contains("Sevk kilidini açmak", sorun.Mesaj);
        Assert.Equal(new[] { 11 }, Assert.Single(koruma.Sorgular));
    }

    [Fact]
    public async Task Onizleme_UVeD_TekTopluSorgu_DSilmeMesajiKorunur()
    {
        var koruma = new KorumaStub(11, 12);
        var u = OnizlemeSatiri(11, "U");
        var d = OnizlemeSatiri(12, "D");
        var hedef = KaynakSatir(12);
        hedef.KaynakCekiSatiriId = 100;

        await OnizlemeyiKontrolEtAsync(koruma, ProjeTipi.Normal, [u, d], [KaynakSatir(11), hedef]);

        Assert.False(u.UygulanabilirMi);
        Assert.False(d.UygulanabilirMi);
        Assert.Equal(SahaAktarimSilmeKorumaMesajlari.RevizyonCekiSatiriDetay, Assert.Single(d.Sorunlar).Mesaj);
        Assert.Equal(new[] { 11, 12 }, Assert.Single(koruma.Sorgular).OrderBy(id => id));
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task Onizleme_DigerProjeTiplerininUIslemi_YeniKaynakKorumasiDisindadir(ProjeTipi tip)
    {
        var koruma = new KorumaStub(11);
        var satir = OnizlemeSatiri(11, "U");

        await OnizlemeyiKontrolEtAsync(koruma, tip, [satir], [KaynakSatir(11)]);

        Assert.True(satir.UygulanabilirMi);
        Assert.Empty(satir.Sorunlar);
        Assert.Empty(koruma.Sorgular);
    }

    [Fact]
    public async Task Onizleme_NormalProjedekiTuremisUVeYeniA_YeniKorumadanEtkilenmez()
    {
        var koruma = new KorumaStub(11, 12);
        var turemis = KaynakSatir(11);
        turemis.KaynakCekiSatiriId = 100;
        var u = OnizlemeSatiri(11, "U");
        var a = OnizlemeSatiri(12, "A");

        await OnizlemeyiKontrolEtAsync(koruma, ProjeTipi.Normal, [u, a], [turemis, KaynakSatir(12)]);

        Assert.True(u.UygulanabilirMi);
        Assert.True(a.UygulanabilirMi);
        Assert.Empty(koruma.Sorgular);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task Onizleme_DSilmeKorumasi_DigerProjeTiplerindeAynenSurdurulur(ProjeTipi tip)
    {
        var koruma = new KorumaStub(11);
        var d = OnizlemeSatiri(11, "D");

        await OnizlemeyiKontrolEtAsync(koruma, tip, [d], [KaynakSatir(11)]);

        Assert.False(d.UygulanabilirMi);
        Assert.Equal(SahaAktarimSilmeKorumaMesajlari.RevizyonCekiSatiriDetay, Assert.Single(d.Sorunlar).Mesaj);
        Assert.Single(koruma.Sorgular);
    }

    [Fact]
    public async Task Uygulama_YeniAktifIliskiyiYenidenKontrolEder_KaynakVeTeslimMiktarlariniDegistirmez()
    {
        var koruma = new KorumaStub();
        var kaynak = KaynakSatir(11);
        kaynak.IstenenAdet = 4;
        kaynak.GelenMiktar = 3;
        kaynak.GridGelenAdet = 4;
        var icerik = new SandikIcerik { Id = 21, CekiSatiriId = 11, TahsisMiktari = 4, KonulanAdet = 3, EksikAdet = 1 };
        kaynak.SandikIcerikleri.Add(icerik);
        var onizleme = OnizlemeSatiri(11, "U");
        await OnizlemeyiKontrolEtAsync(koruma, ProjeTipi.Normal, [onizleme], [kaynak]);
        Assert.True(onizleme.UygulanabilirMi);

        // Onaydan sonra yeni/legacy aktif iliski servisi artik bu kaynagi donduruyor.
        koruma.AktifIds.Add(11);
        var hata = await Assert.ThrowsAsync<CekiRevizyonConflictException>(() =>
            UygulamayiKontrolEtAsync(koruma, ProjeTipi.Normal, [kaynak]));

        Assert.Equal(CekiRevizyonSorunKodlari.AktifSahaAktarimi, Assert.Single(hata.Sorunlar).Kod);
        Assert.Equal(2, koruma.Sorgular.Count);
        Assert.Equal(4, kaynak.IstenenAdet);
        Assert.Equal(3, kaynak.GelenMiktar);
        Assert.Equal(4, kaynak.GridGelenAdet);
        Assert.Equal(4, icerik.TahsisMiktari);
        Assert.Equal(3, icerik.KonulanAdet);
        Assert.Equal(1, icerik.EksikAdet);
    }

    [Fact]
    public async Task Uygulama_TekTopluSorgu_TekrarlananKaynaklarTekilleştirilir_TuremisSatirSorgulanmaz()
    {
        var koruma = new KorumaStub(11, 12, 13);
        var kaynak = KaynakSatir(11);
        var turemis = KaynakSatir(13);
        turemis.KaynakCekiSatiriId = 100;
        using var cancellation = new CancellationTokenSource();

        var hata = await Assert.ThrowsAsync<CekiRevizyonConflictException>(() =>
            UygulamayiKontrolEtAsync(koruma, ProjeTipi.Normal,
                [kaynak, kaynak, KaynakSatir(12), turemis], cancellation.Token));

        Assert.Equal(2, hata.Sorunlar.Count);
        Assert.Equal(new[] { 11, 12 }, Assert.Single(koruma.Sorgular).OrderBy(id => id));
        Assert.Equal(cancellation.Token, koruma.SonCancellationToken);
    }

    [Fact]
    public async Task Uygulama_AktifIliskiYoksa_KaynakGuncellemesiEngellenmez()
    {
        var koruma = new KorumaStub();

        await UygulamayiKontrolEtAsync(koruma, ProjeTipi.Normal, [KaynakSatir(11)]);

        Assert.Single(koruma.Sorgular);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task Uygulama_HedefProjeGuncellemeleri_YeniKorumadanEtkilenmez(ProjeTipi tip)
    {
        var koruma = new KorumaStub(11);

        await UygulamayiKontrolEtAsync(koruma, tip, [KaynakSatir(11)]);

        Assert.Empty(koruma.Sorgular);
    }

    [Fact]
    public async Task Uygulama_NormalProjedekiYalnizTuremisSatir_YeniKorumadanEtkilenmez()
    {
        var koruma = new KorumaStub(11);
        var turemis = KaynakSatir(11);
        turemis.KaynakCekiSatiriId = 100;

        await UygulamayiKontrolEtAsync(koruma, ProjeTipi.Normal, [turemis]);

        Assert.Empty(koruma.Sorgular);
    }

    private static CekiSatiri KaynakSatir(int id) => new() { Id = id, SiraNo = id, BarkodNo = $"TEST-{id}" };

    private static CekiRevizyonOnizlemeSatiri OnizlemeSatiri(int id, string kod) =>
        new() { MevcutCekiSatiriId = id, CheckKodu = kod, ExcelSatirNo = id };

    private static Task OnizlemeyiKontrolEtAsync(KorumaStub koruma, ProjeTipi tip,
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> onizleme,
        IReadOnlyCollection<CekiSatiri> kaynaklar) =>
        InvokeAsync(koruma, "RevizyonSahaAktarimRiskleriniEkleAsync", onizleme, (int)tip, kaynaklar);

    private static Task UygulamayiKontrolEtAsync(KorumaStub koruma, ProjeTipi tip,
        IReadOnlyCollection<CekiSatiri> kaynaklar, CancellationToken cancellationToken = default) =>
        InvokeAsync(koruma, "RevizyonKaynakSahaGuncellemeleriniDogrulaAsync", (int)tip, kaynaklar, cancellationToken);

    private static Task InvokeAsync(KorumaStub koruma, string methodName, params object[] args)
    {
        // Koruma birim testi: DbContext/UoW yok; bu asamada sorgu/degisiklik yapilmamali.
        var service = new CekiService(null!, null!, null!, null!, koruma, NullLogger<CekiService>.Instance);
        var method = typeof(CekiService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return Assert.IsAssignableFrom<Task>(method.Invoke(service, args));
    }

    private sealed class KorumaStub(params int[] aktifIds) : ISahaAktarimSilmeKorumaService
    {
        public HashSet<int> AktifIds { get; } = aktifIds.ToHashSet();
        public List<int[]> Sorgular { get; } = [];
        public CancellationToken SonCancellationToken { get; private set; }

        public Task<HashSet<int>> GetAktifAktarimBagliCekiSatiriIdsAsync(
            IEnumerable<int> cekiSatiriIds, CancellationToken cancellationToken = default)
        {
            var ids = cekiSatiriIds.ToArray();
            Sorgular.Add(ids);
            SonCancellationToken = cancellationToken;
            return Task.FromResult(ids.Where(AktifIds.Contains).ToHashSet());
        }

        public Task<HashSet<int>> GetAktifAktarimBagliSandikIdsAsync(
            IEnumerable<int> sandikIds, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Bu koruma sandik sorgusu yapmamali.");
    }
}
