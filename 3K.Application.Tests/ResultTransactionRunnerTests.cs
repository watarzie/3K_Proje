using _3K.Application.Common;
using _3K.Application.Behaviors;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace _3K.Application.Tests;

public sealed class ResultTransactionRunnerTests
{
    [Fact]
    public async Task Failure_TransactionSahibiyken_RollbackOlurVeAyniResultDoner()
    {
        var unitOfWork = new FakeUnitOfWork();
        var beklenen = Result.Failure("doğrulama hatası", 409);

        var sonuc = await ResultTransactionRunner.ExecuteAsync(
            unitOfWork,
            _ => Task.FromResult(beklenen),
            CancellationToken.None);

        Assert.Same(beklenen, sonuc);
        Assert.True(unitOfWork.RolledBack);
        Assert.False(unitOfWork.Committed);
    }

    [Fact]
    public async Task Success_TransactionSahibiyken_CommitOlur()
    {
        var unitOfWork = new FakeUnitOfWork();

        var sonuc = await ResultTransactionRunner.ExecuteAsync(
            unitOfWork,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.True(unitOfWork.Committed);
        Assert.False(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task Failure_DisTransactionVarken_RollbackSinyaliYutulmaz()
    {
        var unitOfWork = new FakeUnitOfWork(transactionAktif: true);

        await Assert.ThrowsAsync<ResultTransactionRollbackException>(() =>
            ResultTransactionRunner.ExecuteAsync(
                unitOfWork,
                _ => Task.FromResult(Result.Failure("doğrulama hatası", 409)),
                CancellationToken.None));

        Assert.False(unitOfWork.Committed);
    }

    [Fact]
    public async Task RollbackSinyali_MediatRExceptionBehaviorTarafindanYutulmaz()
    {
        var behavior = new UnhandledExceptionBehavior<object, Result>(
            NullLogger<UnhandledExceptionBehavior<object, Result>>.Instance);
        var rollbackSinyali = new ResultTransactionRollbackException(
            Result.Failure("doğrulama hatası", 409));

        var firlatilan = await Assert.ThrowsAsync<ResultTransactionRollbackException>(() =>
            behavior.Handle(
                new object(),
                () => Task.FromException<Result>(rollbackSinyali),
                CancellationToken.None));

        Assert.Same(rollbackSinyali, firlatilan);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private bool _transactionAktif;

        public FakeUnitOfWork(bool transactionAktif = false)
        {
            _transactionAktif = transactionAktif;
        }

        public bool HasActiveTransaction => _transactionAktif;
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity =>
            throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_transactionAktif)
                return await operation(cancellationToken);

            _transactionAktif = true;
            try
            {
                var result = await operation(cancellationToken);
                Committed = true;
                return result;
            }
            catch
            {
                RolledBack = true;
                throw;
            }
            finally
            {
                _transactionAktif = false;
            }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
