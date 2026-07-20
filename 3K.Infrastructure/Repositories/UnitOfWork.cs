using System.Collections;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private Hashtable? _repositories;
        private readonly List<Func<CancellationToken, Task>> _afterCommitCallbacks = new();
        private readonly List<Func<CancellationToken, Task>> _afterRollbackCallbacks = new();

        public UnitOfWork(
            AppDbContext context,
            ILogger<UnitOfWork> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            _repositories ??= new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException(
                    "Kayıt başka bir işlem tarafından değiştirildi.",
                    ex);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
            {
                throw new UniqueConstraintViolationException(
                    postgresException.ConstraintName,
                    "Benzersizlik kuralı ihlal edildi.",
                    ex);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.ForeignKeyViolation
                } postgresException)
            {
                _logger.LogWarning(
                    ex,
                    "Veritabanı referans bütünlüğü kuralı işlemi engelledi. Şema: {SchemaName}, Tablo: {TableName}, Constraint: {ConstraintName}",
                    postgresException.SchemaName,
                    postgresException.TableName,
                    postgresException.ConstraintName);

                throw new ReferentialIntegrityConflictException(
                    "İşlem, ilişkili kayıtların veri bütünlüğünü bozacağı için tamamlanamadı. İlişkili kayıtları kontrol edip tekrar deneyin.",
                    ex);
            }
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (_context.Database.CurrentTransaction != null)
                return await operation(cancellationToken);

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                var committed = false;
                TResult result = default!;

                try
                {
                    await using (var transaction = await _context.Database.BeginTransactionAsync(
                                     IsolationLevel.Serializable,
                                     cancellationToken))
                    {
                        try
                        {
                            result = await operation(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);
                            committed = true;
                        }
                        catch
                        {
                            try
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                            }
                            catch (Exception rollbackException)
                            {
                                _logger.LogError(
                                    rollbackException,
                                    "Transaction geri alınırken ek bir hata oluştu.");
                            }

                            throw;
                        }
                    }

                    // Transaction nesnesi dispose edildikten sonra çalıştırılır;
                    // callback'ler yeni sorgu/SaveChanges işlemi yapabilir.
                    await CompleteTransactionCallbacksAsync(committed: true);
                    return result;
                }
                catch (Exception exception) when (committed)
                {
                    // Commit sunucu tarafından başarıyla onaylandıktan sonra
                    // transaction dispose işlemi hata verse bile veri kalıcıdır.
                    // Rollback telafisini çalıştırmak veya operasyonu retry etmek
                    // bu noktada veri/dosya tutarsızlığı oluşturur.
                    _logger.LogError(
                        exception,
                        "Commit edilen transaction kapatılırken hata oluştu; işlem yeniden çalıştırılmayacak.");
                    await CompleteTransactionCallbacksAsync(committed: true);
                    return result;
                }
                catch (Exception ex) when (IsTransactionConcurrencyConflict(ex))
                {
                    await CompleteTransactionCallbacksAsync(committed: false);
                    throw new ConcurrencyConflictException(
                        "Kayıtlar eşzamanlı başka bir işlem tarafından değiştirildi. Lütfen ekranı yenileyip tekrar deneyin.",
                        ex);
                }
                catch
                {
                    await CompleteTransactionCallbacksAsync(committed: false);
                    throw;
                }
            });
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback)
        {
            RegisterTransactionCallback(callback, _afterCommitCallbacks);
        }

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback)
        {
            RegisterTransactionCallback(callback, _afterRollbackCallbacks);
        }

        private void RegisterTransactionCallback(
            Func<CancellationToken, Task> callback,
            ICollection<Func<CancellationToken, Task>> callbacks)
        {
            ArgumentNullException.ThrowIfNull(callback);

            if (_context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException(
                    "Transaction callback'i yalnızca aktif transaction içinde kaydedilebilir.");
            }

            callbacks.Add(callback);
        }

        private async Task CompleteTransactionCallbacksAsync(bool committed)
        {
            var callbacks = (committed ? _afterCommitCallbacks : _afterRollbackCallbacks)
                .ToArray();

            // Callback içinden yeni bir transaction açılabilmesi ve eski
            // callback'lerin sonraki işlemlere taşınmaması için önce temizle.
            _afterCommitCallbacks.Clear();
            _afterRollbackCallbacks.Clear();

            foreach (var callback in callbacks)
            {
                try
                {
                    await callback(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    // Commit/rollback tamamlandıktan sonraki ikincil bir yan etki,
                    // ana işlemin sonucunu tersine çeviremez.
                    _logger.LogError(
                        exception,
                        "Transaction {TransactionState} callback'i çalıştırılamadı.",
                        committed ? "commit" : "rollback");
                }
            }
        }

        private static bool IsTransactionConcurrencyConflict(Exception exception)
        {
            var postgresException = exception as PostgresException ??
                (exception as DbUpdateException)?.InnerException as PostgresException;

            return postgresException?.SqlState is
                PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.DeadlockDetected;
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
