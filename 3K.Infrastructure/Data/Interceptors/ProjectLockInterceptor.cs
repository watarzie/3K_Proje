using Microsoft.EntityFrameworkCore;
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

            var projeIdsToCheck = new HashSet<int>();

            foreach (var entry in entries)
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
                    // Sadece Proje güncelleniyorsa, ve "Sevk Edildi" durumundaysa ama DurumId değiştiriliyorsa izin ver.
                    // Çünkü kilit açma komutu çalışıyor olabilir.
                    projeIdsToCheck.Add(p.Id);
                }
            }

            foreach (var pid in projeIdsToCheck.Where(p => p > 0))
            {
                // Veritabanındaki güncel (transaction öncesi) durumunu kontrol et
                var durumId = await context.Set<Proje>().Where(p => p.Id == pid).Select(p => p.DurumId).FirstOrDefaultAsync(cancellationToken);
                
                // Eğer veritabanında zaten "SevkEdildi" durumundaysa ve kullanıcı projeyi "KilidiAç" komutuyla "Devam" durumuna çekmiyorsa!
                if (durumId == (int)ProjeDurum.SevkEdildi)
                {
                    // Proje zaten kilitli. Ancak ChangeTracker içindeki Proje nesnesi "Devam" durumuna geçiriliyorsa buna izin ver!
                    var projeEntry = entries.FirstOrDefault(e => e.Entity is Proje pEntry && pEntry.Id == pid);
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
    }
}
