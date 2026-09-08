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

            var relevantEntries = lockableEntries.Where(e =>
                e.Entity is Proje or Ceki or CekiSatiri or Sandik or SandikIcerik).ToList();
            if (relevantEntries.Count == 0)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            // Detached Update'te OriginalValues güncel değerlerin kopyası olabilir. Sahiplik
            // ve onaylı düzeltme bilgisi bu nedenle DB'den, toplu salt-okuma ile doğrulanır.
            var snapshot = await LoadLockSnapshotAsync(context, relevantEntries, cancellationToken);
            var owningProjects = relevantEntries.ToDictionary(e => e,
                e => GetOwningProjectIds(e, entries, snapshot).Where(id => id > 0).ToHashSet());
            var projectIds = owningProjects.Values.SelectMany(ids => ids).Distinct().ToList();
            var lockedProjects = await context.Set<Proje>().AsNoTracking()
                .Where(p => projectIds.Contains(p.Id) && p.DurumId == (int)ProjeDurum.SevkEdildi)
                .Select(p => p.Id).ToListAsync(cancellationToken);
            if (lockedProjects.Count == 0)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            // Mevcut sevkiyatı geri alma akışı aynen korunur; düzeltme bayrağı bu yol değildir.
            var unlockingProjects = relevantEntries.Where(e => e.Entity is Proje p &&
                    p.DurumId != (int)ProjeDurum.SevkEdildi)
                .Select(e => ((Proje)e.Entity).Id).ToHashSet();
            var guardedEntries = relevantEntries.Where(e =>
                owningProjects[e].Any(id => lockedProjects.Contains(id) && !unlockingProjects.Contains(id))).ToList();

            if (guardedEntries.Count > 0)
            {
                await LoadSourceGuardsAsync(context, snapshot, cancellationToken);
                foreach (var entry in guardedEntries)
                    if (!IsApprovedCrateCorrection(entry, entries, snapshot))
                        throw new ProjectLockedException("Bu proje sevk edilmiş ve kilitlenmiştir. Üzerinde hiçbir değişiklik yapılamaz!");
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static async Task<LockSnapshot> LoadLockSnapshotAsync(
            DbContext context, List<EntityEntry> entries, CancellationToken cancellationToken)
        {
            var snapshot = new LockSnapshot();
            var rowIds = entries.Select(e => e.Entity).OfType<CekiSatiri>().Select(x => x.Id).Where(x => x > 0).ToList();
            var contentIds = entries.Select(e => e.Entity).OfType<SandikIcerik>().Select(x => x.Id).Where(x => x > 0).ToList();
            var crateIds = entries.Select(e => e.Entity).OfType<Sandik>().Select(x => x.Id).Where(x => x > 0).ToList();
            if (rowIds.Count > 0)
                snapshot.Rows = await context.Set<CekiSatiri>().AsNoTracking().Where(x => rowIds.Contains(x.Id))
                    .Select(x => new RowOwner(x.Id, x.CekiId, x.KaynakCekiSatiriId)).ToDictionaryAsync(x => x.Id, cancellationToken);
            if (contentIds.Count > 0 || rowIds.Count > 0 || crateIds.Count > 0)
                snapshot.Contents = await context.Set<SandikIcerik>().AsNoTracking().Where(x =>
                        contentIds.Contains(x.Id) || crateIds.Contains(x.SandikId) ||
                        (x.CekiSatiriId.HasValue && rowIds.Contains(x.CekiSatiriId.Value)))
                    .Select(x => new ContentOwner(x.Id, x.SandikId, x.CekiSatiriId))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);
            crateIds = crateIds.Concat(snapshot.Contents.Values.Select(x => x.SandikId))
                .Concat(entries.Select(e => e.Entity).OfType<SandikIcerik>().Select(x => x.SandikId))
                .Where(x => x > 0).Distinct().ToList();
            if (crateIds.Count > 0)
                snapshot.Crates = await context.Set<Sandik>().AsNoTracking().Where(x => crateIds.Contains(x.Id))
                    .Select(x => new CrateOwner(x.Id, x.ProjeId, x.DurumId, x.SevkiyatDuzeltmeAcikMi, x.SevkOncesiDurumId))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);
            var packingListIds = entries.Select(e => e.Entity).OfType<Ceki>().Select(x => x.Id)
                .Concat(entries.Select(e => e.Entity).OfType<CekiSatiri>().Select(x => x.CekiId))
                .Concat(snapshot.Rows.Values.Select(x => x.CekiId)).Where(x => x > 0).Distinct().ToList();
            if (packingListIds.Count > 0)
                snapshot.PackingLists = await context.Set<Ceki>().AsNoTracking().Where(x => packingListIds.Contains(x.Id))
                    .Select(x => new PackingListOwner(x.Id, x.ProjeId)).ToDictionaryAsync(x => x.Id, cancellationToken);
            snapshot.RelatedRowIds = rowIds.Concat(snapshot.Contents.Values.Where(x => x.CekiSatiriId.HasValue)
                    .Select(x => x.CekiSatiriId!.Value))
                .Concat(entries.Select(e => e.Entity).OfType<SandikIcerik>().Where(x => x.CekiSatiriId.HasValue)
                    .Select(x => x.CekiSatiriId!.Value)).Where(x => x > 0).Distinct().ToList();
            return snapshot;
        }

        private static IEnumerable<int> GetOwningProjectIds(EntityEntry entry, List<EntityEntry> entries, LockSnapshot snapshot)
        {
            switch (entry.Entity)
            {
                case Proje project:
                    yield return project.Id;
                    break;
                case Ceki packingList:
                    yield return packingList.ProjeId;
                    if (snapshot.PackingLists.TryGetValue(packingList.Id, out var originalList))
                        yield return originalList.ProjeId;
                    break;
                case Sandik crate:
                    yield return crate.ProjeId;
                    if (snapshot.Crates.TryGetValue(crate.Id, out var originalCrate))
                        yield return originalCrate.ProjeId;
                    break;
                case CekiSatiri row:
                    var listIds = new HashSet<int> { row.CekiId };
                    if (snapshot.Rows.TryGetValue(row.Id, out var originalRow)) listIds.Add(originalRow.CekiId);
                    foreach (var id in listIds)
                    {
                        if (snapshot.PackingLists.TryGetValue(id, out var list)) yield return list.ProjeId;
                        foreach (var pending in entries.Select(e => e.Entity).OfType<Ceki>().Where(c => c.Id == id))
                            yield return pending.ProjeId;
                    }
                    var allocatedCrateIds = snapshot.Contents.Values.Where(x => x.CekiSatiriId == row.Id).Select(x => x.SandikId)
                        .Concat(entries.Select(e => e.Entity).OfType<SandikIcerik>().Where(x => x.CekiSatiriId == row.Id).Select(x => x.SandikId));
                    foreach (var id in allocatedCrateIds.Distinct())
                    {
                        if (snapshot.Crates.TryGetValue(id, out var allocatedCrate)) yield return allocatedCrate.ProjeId;
                        foreach (var pending in entries.Select(e => e.Entity).OfType<Sandik>().Where(s => s.Id == id))
                            yield return pending.ProjeId;
                    }
                    break;
                case SandikIcerik content:
                    var crateIds = new HashSet<int> { content.SandikId };
                    if (snapshot.Contents.TryGetValue(content.Id, out var originalContent)) crateIds.Add(originalContent.SandikId);
                    foreach (var id in crateIds)
                    {
                        if (snapshot.Crates.TryGetValue(id, out var owner)) yield return owner.ProjeId;
                        foreach (var pending in entries.Select(e => e.Entity).OfType<Sandik>().Where(s => s.Id == id))
                            yield return pending.ProjeId;
                    }
                    break;
            }
        }

        private static async Task LoadSourceGuardsAsync(DbContext context, LockSnapshot snapshot, CancellationToken cancellationToken)
        {
            var crateIds = snapshot.Crates.Keys.ToList();
            var rowIds = snapshot.RelatedRowIds;
            if (crateIds.Count == 0 && rowIds.Count == 0) return;
            var activeSources = await context.Set<SahaAktarimKalemi>().AsNoTracking()
                .Where(k => k.DurumId != (int)SahaAktarimDurum.GeriAlindi && k.DurumId != (int)SahaAktarimDurum.Iptal &&
                    ((k.KaynakSandikId.HasValue && crateIds.Contains(k.KaynakSandikId.Value)) || rowIds.Contains(k.KaynakCekiSatiriId)))
                .Select(k => new { k.KaynakSandikId, k.KaynakCekiSatiriId }).ToListAsync(cancellationToken);
            snapshot.GuardedSourceCrates.UnionWith(activeSources.Where(k => k.KaynakSandikId.HasValue).Select(k => k.KaynakSandikId!.Value));
            snapshot.GuardedSourceRows.UnionWith(activeSources.Select(k => k.KaynakCekiSatiriId));
            if (rowIds.Count == 0) return;
            var legacySources = await context.Set<CekiSatiri>().AsNoTracking()
                .Where(target => target.KaynakCekiSatiriId.HasValue && rowIds.Contains(target.KaynakCekiSatiriId.Value) &&
                    target.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                    !context.Set<SahaAktarimKalemi>().Any(k => k.SahaCekiSatiriId == target.Id))
                .Select(target => target.KaynakCekiSatiriId!.Value).Distinct().ToListAsync(cancellationToken);
            snapshot.GuardedSourceRows.UnionWith(legacySources);
        }

        private static bool IsApprovedCrateCorrection(EntityEntry entry, List<EntityEntry> entries, LockSnapshot snapshot)
        {
            var crateIds = new HashSet<int>();
            var rowIds = new HashSet<int>();
            switch (entry.Entity)
            {
                case Sandik crate when entry.State == EntityState.Modified:
                    if (!snapshot.Crates.TryGetValue(crate.Id, out var originalCrate) ||
                        originalCrate.ProjeId != crate.ProjeId || crate.DurumId != (int)SandikDurum.Sevkedildi ||
                        originalCrate.SevkOncesiDurumId != crate.SevkOncesiDurumId ||
                        !crate.SevkiyatDuzeltmeAcikMi) return false;
                    crateIds.Add(crate.Id);
                    rowIds.UnionWith(snapshot.Contents.Values.Where(x => x.SandikId == crate.Id && x.CekiSatiriId.HasValue)
                        .Select(x => x.CekiSatiriId!.Value));
                    break;
                case SandikIcerik content:
                    if (entry.State != EntityState.Added && (!snapshot.Contents.TryGetValue(content.Id, out var originalContent) ||
                        originalContent.SandikId != content.SandikId || originalContent.CekiSatiriId != content.CekiSatiriId)) return false;
                    crateIds.Add(content.SandikId);
                    if (content.CekiSatiriId.HasValue) rowIds.Add(content.CekiSatiriId.Value);
                    break;
                case CekiSatiri row when entry.State == EntityState.Modified:
                    if (!snapshot.Rows.TryGetValue(row.Id, out var originalRow) || originalRow.CekiId != row.CekiId ||
                        originalRow.KaynakCekiSatiriId != row.KaynakCekiSatiriId) return false;
                    rowIds.Add(row.Id);
                    // Aynı satır birden fazla sandığa bölünmüşse yalnız birini açmak yeterli değildir.
                    crateIds.UnionWith(snapshot.Contents.Values.Where(x => x.CekiSatiriId == row.Id).Select(x => x.SandikId));
                    crateIds.UnionWith(entries.Select(e => e.Entity).OfType<SandikIcerik>()
                        .Where(x => x.CekiSatiriId == row.Id).Select(x => x.SandikId));
                    break;
                default:
                    return false; // Proje/çeki ana verisi, yeni/silinen sandık ve yeni/silinen çeki satırı açılmaz.
            }

            if (crateIds.Count == 0 || rowIds.Overlaps(snapshot.GuardedSourceRows) || crateIds.Overlaps(snapshot.GuardedSourceCrates))
                return false;
            return crateIds.All(id => snapshot.Crates.TryGetValue(id, out var persisted) &&
                persisted.DurumId == (int)SandikDurum.Sevkedildi && persisted.SevkiyatDuzeltmeAcikMi &&
                !entries.Any(e => e.Entity is Sandik s && s.Id == id &&
                    (e.State == EntityState.Deleted || s.ProjeId != persisted.ProjeId ||
                     s.DurumId != persisted.DurumId || !s.SevkiyatDuzeltmeAcikMi)));
        }

        private sealed record RowOwner(int Id, int CekiId, int? KaynakCekiSatiriId);
        private sealed record ContentOwner(int Id, int SandikId, int? CekiSatiriId);
        private sealed record CrateOwner(int Id, int ProjeId, int DurumId, bool SevkiyatDuzeltmeAcikMi, int? SevkOncesiDurumId);
        private sealed record PackingListOwner(int Id, int ProjeId);
        private sealed class LockSnapshot
        {
            public Dictionary<int, RowOwner> Rows { get; set; } = new();
            public Dictionary<int, ContentOwner> Contents { get; set; } = new();
            public Dictionary<int, CrateOwner> Crates { get; set; } = new();
            public Dictionary<int, PackingListOwner> PackingLists { get; set; } = new();
            public List<int> RelatedRowIds { get; set; } = new();
            public HashSet<int> GuardedSourceCrates { get; } = new();
            public HashSet<int> GuardedSourceRows { get; } = new();
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
