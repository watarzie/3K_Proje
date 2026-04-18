using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Data
{
    /// <summary>
    /// EF Core SaveChanges Interceptor — Audit alanlarını (CreatedDate/By, UpdatedDate/By)
    /// otomatik doldurur. Böylece AppDbContext.SaveChangesAsync override'ına gerek kalmaz.
    /// ICurrentUserService üzerinden aktif kullanıcı bilgisi alınır.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUser;

        public AuditInterceptor(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
                ApplyAuditFields(eventData.Context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
                ApplyAuditFields(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        private void ApplyAuditFields(DbContext context)
        {
            var userName = _currentUser.UserId?.ToString();

            foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        entry.Entity.CreatedBy = userName;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = userName;
                        // CreatedDate readonly — değiştirilemesin
                        entry.Property(nameof(BaseEntity.CreatedDate)).IsModified = false;
                        entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                        break;
                }
            }
        }
    }
}
