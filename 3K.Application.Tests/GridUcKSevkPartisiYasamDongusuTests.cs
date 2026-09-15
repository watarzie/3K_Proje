using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

/// <summary>
/// Aktif Grid sevk partisinin açık, erken sonuçlanmış ve legacy durumlarını
/// birbirinden ayıran yaşam döngüsü sözleşmesini sabitler.
/// </summary>
public sealed class GridUcKSevkPartisiYasamDongusuTests
{
    [Fact]
    public void ErkenSonuclananEksikParti_AlternatifKaynakBorcuKapatsaDaTeslimeKapanir_IadeyeAcikKalir()
    {
        var satir = AktifSatirOlustur(
            istenen: 5,
            aktifParti: 5,
            aktifKarsilanan: 3,
            gelen: 3);
        satir.StokKarsilanan = 2;
        satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;

        Assert.Equal(0, satir.KalanMiktar);
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiSonuclandirildiMi(satir));
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(satir));
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
        Assert.True(GridUcKSevkPartisiKurali.GeriGonderimeAcikMi(satir, null));
    }

    [Fact]
    public void KismiYenidenSevkPartisi_TeslimeAcikKalirVeAyniPartiBitmedenYeniPartiUretmez()
    {
        var satir = AktifSatirOlustur(
            istenen: 6,
            aktifParti: 4,
            aktifKarsilanan: 2,
            gelen: 2);
        satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        satir.YenidenSevkGerekliAdet = 2;

        var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);

        Assert.Equal(2, GridUcKSevkPartisiKurali.AktifPartiKalanMiktariniHesapla(satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
        Assert.False(karar.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.Yok, karar.Tip);
        Assert.Equal(0, karar.UstSinir);
    }

    [Fact]
    public void AlternatifKaynaklaIhtiyacKapanmisAktifParti_FazlaGeldiIslemineAcikKalir()
    {
        var satir = AktifSatirOlustur(
            istenen: 5,
            aktifParti: 5,
            aktifKarsilanan: 3,
            gelen: 3);
        satir.StokKarsilanan = 2;

        Assert.Equal(0, satir.KalanMiktar);
        Assert.Equal(2, GridUcKSevkPartisiKurali.AktifPartiKalanMiktariniHesapla(satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiFazlaTeslimeAcikMi(satir));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir(
        bool sayacVar,
        bool sonucBayragiVar)
    {
        var satir = AktifSatirOlustur(
            istenen: 2,
            aktifParti: 2,
            aktifKarsilanan: 0,
            gelen: 0);
        satir.AktifGridSevkKarsilananMiktari = sayacVar ? 0m : null;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = sonucBayragiVar ? false : null;

        Assert.True(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(satir));
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
    }

    [Fact]
    public async Task YeniPartiBaslatmaBayragiFalseYapar_TakipTemizlemeParentVeChildAlanlariniNullYapar()
    {
        using var unitOfWork = new TestUnitOfWork();
        var satir = new CekiSatiri
        {
            Id = 11,
            IstenenAdet = 3,
            GridSevkMiktari = 1,
            AktifGridSevkKarsilananMiktari = 1,
            AktifGridSevkPartisiErkenSonuclandirildiMi = true,
            UcKDurumuId = (int)UcKDurum.EksikGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            TeslimTarihi = DateTime.UtcNow
        };
        var icerik = new SandikIcerik
        {
            Id = 21,
            CekiSatiriId = satir.Id,
            TahsisMiktari = 3,
            AktifGridSevkKarsilananMiktari = 1
        };
        var icerikler = new[] { icerik };

        await GridUcKSevkPartisiKurali.YeniPartiBaslatAsync(
            unitOfWork,
            satir,
            sevkMiktari: 2,
            GridUcKDevamSevkKarari.UygunDegil,
            icerikler);

        Assert.Equal(2, satir.GridSevkMiktari);
        Assert.Equal(0, satir.AktifGridSevkKarsilananMiktari);
        Assert.False(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, icerik.AktifGridSevkKarsilananMiktari);

        await GridUcKSevkPartisiKurali.AktifPartiTakibiniTemizleAsync(
            unitOfWork,
            satir,
            icerikler);

        Assert.Null(satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(icerik.AktifGridSevkKarsilananMiktari);
    }

    private static CekiSatiri AktifSatirOlustur(
        decimal istenen,
        decimal aktifParti,
        decimal aktifKarsilanan,
        decimal gelen)
    {
        return new CekiSatiri
        {
            IstenenAdet = istenen,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = istenen,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = aktifParti,
            AktifGridSevkKarsilananMiktari = aktifKarsilanan,
            AktifGridSevkPartisiErkenSonuclandirildiMi = false,
            UcKDurumuId = (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
            GelenMiktar = gelen
        };
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly TestRepository<SandikIcerik> _icerikler = new();

        public bool HasActiveTransaction => false;

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            if (typeof(T) == typeof(SandikIcerik))
                return (IGenericRepository<T>)(object)_icerikler;

            throw new NotSupportedException($"{typeof(T).Name} repository is not required by this test.");
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class TestRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly List<T> _rows = [];

        public Task<T?> GetByIdAsync(int id) =>
            Task.FromResult(_rows.SingleOrDefault(entity => entity.Id == id));

        public Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(_rows);

        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(
            Expression<Func<T, TProperty>> include) =>
            GetAllAsync();

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(_rows.Where(predicate.Compile()).ToList());

        public IQueryable<T> Queryable() => _rows.AsQueryable();

        public Task AddAsync(T entity)
        {
            _rows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
        }

        public void Remove(T entity) => _rows.Remove(entity);
    }
}
