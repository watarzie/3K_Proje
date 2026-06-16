using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace _3K.Infrastructure.Data.Interceptors
{
    public class ProjectLockInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Audit log entity'leri — proje kilitli olsa bile her zaman yazılabilir.
        /// </summary>
        private static readonly HashSet<Type> AuditBypassTypes = new()
        {
            typeof(HareketGecmisi),
            typeof(OnayBekleyenIslem)
        };

        private static readonly HashSet<string> SevkiyatDuzeltmeBypassProperties = new()
        {
            nameof(Sandik.SevkiyatDuzeltmeAcikMi),
            nameof(BaseEntity.UpdatedDate),
            nameof(BaseEntity.UpdatedBy)
        };

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            if (!entries.Any()) return await base.SavingChangesAsync(eventData, result, cancellationToken);

            // Kilit kontrolüne tabi olmayan entry'leri ayır
            var lockableEntries = entries
                .Where(e => !AuditBypassTypes.Contains(e.Entity.GetType()))
                .Where(e => !IsSafeSevkiyatDuzeltmeFlagChange(e))
                .ToList();

            if (!lockableEntries.Any()) return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var projeIdsToCheck = new HashSet<int>();

            foreach (var entry in lockableEntries)
            {
                if (entry.Entity is CekiSatiri cs)
                {
                    if (cs.Ceki != null)
                        projeIdsToCheck.Add(cs.Ceki.ProjeId);
                    else
                    {
                        var projeId = await context.Set<Ceki>().Where(c => c.Id == cs.CekiId).Select(c => c.ProjeId).FirstOrDefaultAsync(cancellationToken);
                        projeIdsToCheck.Add(projeId);
                    }
                }
                else if (entry.Entity is Sandik s)
                {
                    projeIdsToCheck.Add(s.ProjeId);
                }
                else if (entry.Entity is SandikIcerik si)
                {
                    if (si.Sandik != null)
                        projeIdsToCheck.Add(si.Sandik.ProjeId);
                    else
                    {
                        var projeId = await context.Set<Sandik>().Where(sa => sa.Id == si.SandikId).Select(sa => sa.ProjeId).FirstOrDefaultAsync(cancellationToken);
                        projeIdsToCheck.Add(projeId);
                    }
                }
                else if (entry.Entity is Ceki c)
                {
                    projeIdsToCheck.Add(c.ProjeId);
                }
                else if (entry.Entity is Proje p)
                {
                    projeIdsToCheck.Add(p.Id);
                }
            }

            foreach (var pid in projeIdsToCheck.Where(p => p > 0))
            {
                // Proje durumu ve tipini birlikte çek
                var projeInfo = await context.Set<Proje>()
                    .Where(p => p.Id == pid)
                    .Select(p => new { p.DurumId, p.ProjeTipiId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (projeInfo == null) continue;

                // Normal proje, Saha, Yedek hepsi için SevkEdildi kontrolü geçerlidir.

                // Normal proje — SevkEdildi kontrolü
                if (projeInfo.DurumId == (int)ProjeDurum.SevkEdildi)
                {
                    // ProjeKilidiAcCommand çalışıyorsa izin ver
                    var projeEntry = lockableEntries.FirstOrDefault(e => e.Entity is Proje pEntry && pEntry.Id == pid);
                    if (projeEntry != null && projeEntry.Entity is Proje pEntity)
                    {
                        if (pEntity.DurumId != (int)ProjeDurum.SevkEdildi)
                        {
                            continue; // Kilit açılıyor, engelleme!
                        }
                    }

                    throw new ProjectLockedException("Bu proje sevk edilmiş ve kilitlenmiştir. Üzerinde hiçbir değişiklik yapılamaz!");
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static bool IsSafeSevkiyatDuzeltmeFlagChange(EntityEntry entry)
        {
            if (entry.Entity is not Sandik || entry.State != EntityState.Modified)
                return false;

            var originalDurumId = GetPropertyValue<int>(entry, nameof(Sandik.DurumId), useOriginal: true);
            var currentDurumId = GetPropertyValue<int>(entry, nameof(Sandik.DurumId), useOriginal: false);
            if (originalDurumId != (int)SandikDurum.Sevkedildi || currentDurumId != (int)SandikDurum.Sevkedildi)
                return false;

            var changedProperties = entry.Properties
                .Where(p => !Equals(p.OriginalValue, p.CurrentValue))
                .Select(p => p.Metadata.Name)
                .ToList();

            return changedProperties.Contains(nameof(Sandik.SevkiyatDuzeltmeAcikMi)) &&
                   changedProperties.All(SevkiyatDuzeltmeBypassProperties.Contains);
        }

        private static T? GetPropertyValue<T>(EntityEntry entry, string propertyName, bool useOriginal)
        {
            var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
            if (property == null)
                return default;

            var value = useOriginal ? property.OriginalValue : property.CurrentValue;
            return value is T typed ? typed : default;
        }
    }
}
