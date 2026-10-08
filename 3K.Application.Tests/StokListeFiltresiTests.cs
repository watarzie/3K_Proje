using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Application.Features.StokIslemleri.Queries;
using _3K.Application.Features.StokIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Stok listesinin Aktif Stok / Tükendiler sekmeleri: durum ve arama sunucuda filtrelenir, parametresiz liste
/// bütün kayıtları döndürmeye devam eder, sekme sayaçları aynı aramayla hesaplanır.
/// </summary>
public sealed class StokListeFiltresiTests
{
    private static readonly StokKaydi[] Kayitlar =
    [
        new() { Id = 1, MalzemeKodu = "BC-1", MalzemeAdi = "Conta", Miktar = 3, DurumId = (int)StokDurum.Aktif, KaynakProje = "PA-1" },
        new() { Id = 2, MalzemeKodu = "BC-2", MalzemeAdi = "Conta seti", Miktar = 0, DurumId = (int)StokDurum.Tukendi },
        new() { Id = 3, MalzemeKodu = "BC-3", MalzemeAdi = "Pompa", Miktar = 1, DurumId = (int)StokDurum.Aktif }
    ];

    [Theory]
    [InlineData(null, null, new[] { 1, 2, 3 })]
    [InlineData(null, (int)StokDurum.Aktif, new[] { 1, 3 })]
    [InlineData(null, (int)StokDurum.Tukendi, new[] { 2 })]
    [InlineData("conta", (int)StokDurum.Aktif, new[] { 1 })]
    [InlineData("pa-1", null, new[] { 1 })]
    public void Durum_ve_arama_birlikte_uygulanir(string? arama, int? durumId, int[] beklenen) =>
        Assert.Equal(beklenen, StokListeFiltresi.Uygula(Kayitlar.AsQueryable(), arama, durumId).Select(s => s.Id).Order());

    [Fact]
    public void Gecersiz_durum_reddedilir()
    {
        var validator = new StokListeleQueryValidator();
        Assert.True(validator.Validate(new StokListeleQuery()).IsValid);
        Assert.True(validator.Validate(new StokListeleQuery { DurumId = (int)StokDurum.Tukendi }).IsValid);
        Assert.False(validator.Validate(new StokListeleQuery { DurumId = 9 }).IsValid);
    }

    [Fact]
    public async Task Handler_durumu_servise_aktarir_parametresiz_istek_filtresizdir()
    {
        var servis = new SahteStokServisi();
        var handler = new StokListeleQueryHandler(servis);

        await handler.Handle(new StokListeleQuery { PageNumber = 1, PageSize = 15 }, default);
        Assert.Null(servis.SonDurum);

        var sonuc = await handler.Handle(new StokListeleQuery { PageNumber = 1, PageSize = 15, DurumId = (int)StokDurum.Tukendi }, default);
        Assert.Equal((int)StokDurum.Tukendi, servis.SonDurum);
        Assert.All(sonuc.Value!.Items, s => Assert.Equal((int)StokDurum.Tukendi, s.DurumId));
    }

    [Fact]
    public void Sayac_ve_pdf_istekleri_stok_okuma_iznine_baglidir()
    {
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(new StokDurumSayilariQuery()));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(new GetStokPdfQuery { DurumId = 2 }));
    }

    /// <summary>Gerçek PostgreSQL'de filtre, arama ve durum sayaçlarının SQL çevirisi.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Servis_postgreste_durum_filtresi_ve_sayaclari_hesaplar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            db.StokKayitlari.AddRange(Kayitlar.Select(k => new StokKaydi
            {
                MalzemeKodu = k.MalzemeKodu, MalzemeAdi = k.MalzemeAdi, Miktar = k.Miktar, DurumId = k.DurumId,
                KaynakProje = k.KaynakProje, BirimId = (int)Birim.Adet
            }));
            await db.SaveChangesAsync();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var servis = new StokService(uow, db);

            var tukenen = await servis.GetPaginatedStoklarAsync(null, 1, 15, (int)StokDurum.Tukendi);
            var hepsi = await servis.GetPaginatedStoklarAsync(null, 1, 15);
            var aranan = await servis.GetPaginatedStoklarAsync("CONTA", 1, 15, (int)StokDurum.Aktif);

            Assert.Equal(("Conta seti", 1), (Assert.Single(tukenen.Items).MalzemeAdi, tukenen.TotalCount));
            Assert.Equal(3, hepsi.TotalCount);
            Assert.Equal("Conta", Assert.Single(aranan.Items).MalzemeAdi);
            Assert.Equal(new StokDurumSayilari(2, 1), await servis.GetDurumSayilariAsync(null));
            Assert.Equal(new StokDurumSayilari(1, 1), await servis.GetDurumSayilariAsync("conta"));
        });
    }

    private sealed class SahteStokServisi : IStokService
    {
        public int? SonDurum { get; private set; } = -1;

        public Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi) => throw new NotSupportedException();

        public Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(
            string? searchTerm, int pageNumber, int pageSize, int? durumId = null)
        {
            SonDurum = durumId;
            var sonuc = StokListeFiltresi.Uygula(Kayitlar.AsQueryable(), searchTerm, durumId).ToList();
            return Task.FromResult<(IEnumerable<StokKaydi>, int)>((sonuc, sonuc.Count));
        }

        public Task<StokDurumSayilari> GetDurumSayilariAsync(string? searchTerm, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StokDurumSayilari(2, 1));
    }
}
