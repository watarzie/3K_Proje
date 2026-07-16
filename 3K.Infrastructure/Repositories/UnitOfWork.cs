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

        public UnitOfWork(
            AppDbContext context,
            ILogger<UnitOfWork> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                try
                {
                    var result = await operation(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch (Exception ex) when (IsTransactionConcurrencyConflict(ex))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw new ConcurrencyConflictException(
                        "Kayıtlar eşzamanlı başka bir işlem tarafından değiştirildi. Lütfen ekranı yenileyip tekrar deneyin.",
                        ex);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            });
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
