using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace _3K.Application.Tests;

public sealed class AtomicTransactionTests
{
    [Fact]
    public async Task Basarisiz_sonuc_transactioni_geri_alir_ve_ayni_sonucu_dondurur()
    {
        var unitOfWork = new KayitliUnitOfWork();

        var sonuc = await unitOfWork.ExecuteAtomicAsync(
            _ => Task.FromResult(Result.Failure("Satır kilitli.", 409)));

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal("Satır kilitli.", sonuc.Error!.Message);
        Assert.Equal(KayitliUnitOfWork.Durum.GeriAlindi, unitOfWork.Son);
    }

    [Fact]
    public async Task Basarili_sonuc_commit_edilir()
    {
        var unitOfWork = new KayitliUnitOfWork();

        var sonuc = await unitOfWork.ExecuteAtomicAsync(_ => Task.FromResult(Result<int>.Success(7)));

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(7, sonuc.Value);
        Assert.Equal(KayitliUnitOfWork.Durum.CommitEdildi, unitOfWork.Son);
    }

    [Fact]
    public async Task Dis_transactiona_katilan_adimin_hatasi_dis_islemi_de_geri_aldirir()
    {
        var unitOfWork = new KayitliUnitOfWork();

        var sonuc = await unitOfWork.ExecuteAtomicAsync(async token =>
        {
            await unitOfWork.ExecuteAtomicAsync(_ => Task.FromResult(Result.Failure("İç adım reddedildi.", 400)), token);
            return Result.Success();
        });

        Assert.False(sonuc.IsSuccess);
        Assert.Equal("İç adım reddedildi.", sonuc.Error!.Message);
        Assert.Equal(KayitliUnitOfWork.Durum.GeriAlindi, unitOfWork.Son);
    }

    [Fact]
    public async Task Pipelinea_ulasan_geri_alma_istisnasi_ayni_hata_sonucuna_donusur()
    {
        var behavior = new UnhandledExceptionBehavior<TestKomutu, Result>(
            NullLogger<UnhandledExceptionBehavior<TestKomutu, Result>>.Instance);
        var sorunlar = new[] { new { SandikIcerikId = 5 } };

        var sonuc = await behavior.Handle(new TestKomutu(),
            () => throw new ResultRollbackException(Result.Failure("İçerik #5 taşınamadı.", 409, sorunlar)),
            CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Same(sorunlar, sonuc.Error!.Issues);
    }

    public sealed class TestKomutu;

    /// <summary>ExecuteInTransactionAsync sözleşmesini (normal dönüş commit, istisna rollback) taklit eder.</summary>
    private sealed class KayitliUnitOfWork : IUnitOfWork
    {
        public enum Durum { Yok, CommitEdildi, GeriAlindi }

        private int _derinlik;
        public Durum Son { get; private set; }
        public bool HasActiveTransaction => _derinlik > 0;

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            if (_derinlik > 0)
                return await operation(cancellationToken);

            _derinlik++;
            try
            {
                var sonuc = await operation(cancellationToken);
                Son = Durum.CommitEdildi;
                return sonuc;
            }
            catch
            {
                Son = Durum.GeriAlindi;
                throw;
            }
            finally
            {
                _derinlik--;
            }
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) { }
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) { }
        public void Dispose() { }
    }
}
