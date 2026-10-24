using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Sandık açılış/kapanış olaylarının tek yazım noktası. Sandık durumunu ~20 farklı yol değiştirir (tekil/toplu
    /// kapatma, Grid'de otomatik kapanma, ürün güncelleme ve taşımada otomatik geri açılma, saha aktarımı…); hepsi
    /// kaydetme anında <see cref="SandikDurumOlayKurali"/> ile değerlendirilir: kapanma tarihi güncellenir ve olay
    /// yazılır. Elle ve toplu işlemler olayı sebebiyle kendisi ekler; interceptor o sandık için ikinci olay yazmaz,
    /// kendi yazdığı olaylar "Otomatik" kaynaklıdır.
    /// </summary>
    public sealed class SandikDurumOlayInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } context)
            {
                var adaylar = Adaylar(context);
                if (adaylar.Count > 0)
                    Isle(context, adaylar, await KayitliDurumlarAsync(context, adaylar, cancellationToken));
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is { } context)
            {
                var adaylar = Adaylar(context);
                if (adaylar.Count > 0)
                    Isle(context, adaylar, KayitliDurumlar(context, adaylar));
            }
            return base.SavingChanges(eventData, result);
        }

        /// <summary>Yeni sandıklar ve durumu yazılacak sandıklar (izlenmeyen Update'te bütün kolonlar yazılır).</summary>
        private static List<EntityEntry<Sandik>> Adaylar(DbContext context) =>
            context.ChangeTracker.Entries<Sandik>()
                .Where(e => e.State == EntityState.Added ||
                            (e.State == EntityState.Modified && e.Property(s => s.DurumId).IsModified))
                .ToList();

        // İzlenmeyen nesneyle Update'te OriginalValues güncel değerin kopyasıdır; eski durum veritabanından okunur.
        private static async Task<Dictionary<int, KayitliDurum>> KayitliDurumlarAsync(
            DbContext context, List<EntityEntry<Sandik>> adaylar, CancellationToken cancellationToken)
        {
            var idler = KayitliIdler(adaylar);
            return idler.Count == 0
                ? new Dictionary<int, KayitliDurum>()
                : await KayitliDurumSorgusu(context, idler).ToDictionaryAsync(x => x.Id, cancellationToken);
        }

        private static Dictionary<int, KayitliDurum> KayitliDurumlar(DbContext context, List<EntityEntry<Sandik>> adaylar)
        {
            var idler = KayitliIdler(adaylar);
            return idler.Count == 0
                ? new Dictionary<int, KayitliDurum>()
                : KayitliDurumSorgusu(context, idler).ToDictionary(x => x.Id);
        }

        private static List<int> KayitliIdler(List<EntityEntry<Sandik>> adaylar) =>
            adaylar.Where(e => e.State == EntityState.Modified).Select(e => e.Entity.Id).Distinct().ToList();

        private static IQueryable<KayitliDurum> KayitliDurumSorgusu(DbContext context, List<int> idler) =>
            context.Set<Sandik>().AsNoTracking()
                .Where(s => idler.Contains(s.Id))
                .Select(s => new KayitliDurum(s.Id, s.DurumId, s.KapanmaTarihi));

        private void Isle(DbContext context, List<EntityEntry<Sandik>> adaylar, Dictionary<int, KayitliDurum> kayitli)
        {
            var bekleyenOlaylar = context.ChangeTracker.Entries<SandikDurumOlayi>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();
            var simdi = TurkeyTime.Now;

            foreach (var entry in adaylar)
            {
                var sandik = entry.Entity;
                int? eskiDurumId = null;
                DateTime? mevcutKapanma = null;
                if (entry.State == EntityState.Modified)
                {
                    if (!kayitli.TryGetValue(sandik.Id, out var onceki))
                        continue;
                    eskiDurumId = onceki.DurumId;
                    mevcutKapanma = onceki.KapanmaTarihi;
                }

                var acikOlay = bekleyenOlaylar.FirstOrDefault(o =>
                    ReferenceEquals(o.Sandik, sandik) || (sandik.Id > 0 && o.SandikId == sandik.Id));
                var tur = SandikDurumOlayKurali.OlayTuru(eskiDurumId, sandik.DurumId);
                var kapanma = SandikDurumOlayKurali.KapanmaTarihi(
                    mevcutKapanma, eskiDurumId, sandik.DurumId, acikOlay?.Tarih ?? simdi);
                // CurrentValue değişikliği izleyiciye hemen işlenir; eski nesneden gelen bayat tarih de böylece ezilmez.
                entry.Property(s => s.KapanmaTarihi).CurrentValue = kapanma;

                if (tur is null || acikOlay is not null)
                    continue;

                var olay = new SandikDurumOlayi
                {
                    ProjeId = sandik.ProjeId,
                    SandikNo = sandik.SandikNo,
                    Tur = tur.Value,
                    Kaynak = SandikDurumOlayKaynagi.Otomatik,
                    EskiDurumId = eskiDurumId,
                    YeniDurumId = sandik.DurumId,
                    KullaniciId = currentUser.IslemKullaniciId,
                    Tarih = simdi
                };
                if (entry.State == EntityState.Added)
                {
                    olay.Sandik = sandik;
                    if (sandik.ProjeId == 0)
                        olay.Proje = sandik.Proje;
                }
                else
                    olay.SandikId = sandik.Id;
                context.Set<SandikDurumOlayi>().Add(olay);
            }
        }

        private sealed record KayitliDurum(int Id, int DurumId, DateTime? KapanmaTarihi);
    }
}
