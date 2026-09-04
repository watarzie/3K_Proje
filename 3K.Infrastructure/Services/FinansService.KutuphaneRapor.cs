using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<IReadOnlyList<FinansUrunModel>> UrunlerAsync(
            bool sadeceAktif,
            DateTime? tarifeTarihi,
            CancellationToken cancellationToken)
        {
            var tariffDate = (tarifeTarihi ?? TurkeyTime.Now).Date;
            var query = _context.Set<FinansUrun>()
                .AsNoTracking()
                .Include(x => x.Eslesmeler)
                .Include(x => x.FiyatTarifeleri)
                .AsQueryable();
            if (sadeceAktif) query = query.Where(x => x.Aktif);
            var entities = await query.OrderBy(x => x.Sira).ThenBy(x => x.Ad).ToListAsync(cancellationToken);
            return entities.Select(x => MapProduct(x, tariffDate)).ToArray();
        }

        public async Task<IReadOnlyList<FinansUrunSecenekModel>> UrunSecenekleriAsync(
            CancellationToken cancellationToken)
            => await BuildProductOptionsQuery(_context.Set<FinansUrun>().AsNoTracking())
                .ToListAsync(cancellationToken);

        internal static IQueryable<FinansUrunSecenekModel> BuildProductOptionsQuery(
            IQueryable<FinansUrun> query)
            => query
                .Where(x => x.Aktif)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Id)
                .Select(x => new FinansUrunSecenekModel(
                    x.Id, x.Kod, x.Ad, x.FiyatlandirmaBirimi));

        public async Task<FinansSayfaliSonuc<FinansUrunModel>> UrunlerSayfaliAsync(
            bool sadeceAktif,
            DateTime? tarifeTarihi,
            string? arama,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var tariffDate = (tarifeTarihi ?? TurkeyTime.Now).Date;
            var query = ApplyProductFilter(
                _context.Set<FinansUrun>().AsNoTracking(), sadeceAktif, arama);
            var count = await query.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(pageNumber, pageSize, count, 100);
            var entities = await WithProductPageDetails(query, tariffDate)
                .AsSplitQuery()
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Id)
                .Skip(skip)
                .Take(size)
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansUrunModel>
            {
                Items = entities.Select(x => MapProduct(x, tariffDate)).ToArray(),
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        internal static IQueryable<FinansUrun> WithProductPageDetails(
            IQueryable<FinansUrun> query,
            DateTime tariffDate)
            => query
                .Include(x => x.Eslesmeler)
                .Include(x => x.FiyatTarifeleri.Where(tariff =>
                    tariff.Aktif &&
                    tariff.GecerlilikBaslangici < tariffDate.AddDays(1) &&
                    tariff.GecerlilikBitisi >= tariffDate));

        internal static IQueryable<FinansUrun> ApplyProductFilter(
            IQueryable<FinansUrun> query,
            bool sadeceAktif,
            string? arama)
        {
            if (sadeceAktif)
                query = query.Where(x => x.Aktif);
            if (string.IsNullOrWhiteSpace(arama))
                return query;

            var search = arama.Trim().ToLower();
            return query.Where(x =>
                x.Kod.ToLower().Contains(search) ||
                x.Ad.ToLower().Contains(search) ||
                x.Eslesmeler.Any(y =>
                    (y.SandikAdi != null && y.SandikAdi.ToLower().Contains(search)) ||
                    (y.SandikTipi != null && y.SandikTipi.ToLower().Contains(search))));
        }

        public Task<FinansUrunModel> UrunOlusturAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => UrunOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansUrunModel> UrunOlusturCoreAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken)
        {
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansUrun>().AnyAsync(x => x.Kod == code, cancellationToken))
                throw new InvalidOperationException("Finans ürün kodu benzersiz olmalıdır.");
            var entity = new FinansUrun
            {
                Kod = code,
                Ad = model.Ad.Trim(),
                FiyatlandirmaBirimi = model.FiyatlandirmaBirimi,
                Aktif = model.Aktif,
                Sira = model.Sira
            };
            foreach (var match in model.Eslesmeler)
                entity.Eslesmeler.Add(NewMatch(match));
            ApplyDirectPrice(entity, model);
            _context.Set<FinansUrun>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansUrun), entity.Id, "Oluşturma", "*", null, entity.Kod);
            await _context.SaveChangesAsync(cancellationToken);
            await BackfillUnpricedWorksAsync(cancellationToken);
            return await ProductByIdAsync(entity.Id, TurkeyTime.Now.Date, cancellationToken)
                ?? throw new InvalidOperationException("Kaydedilen finans ürünü tekrar okunamadı.");
        }

        public Task<FinansUrunModel?> UrunGuncelleAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => UrunGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansUrunModel?> UrunGuncelleCoreAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken)
        {
            var tariffDate = TurkeyTime.Now.Date;
            var entity = await WithProductPageDetails(
                    _context.Set<FinansUrun>().Where(x => x.Id == id), tariffDate)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansUrun>().AnyAsync(x => x.Id != id && x.Kod == code, cancellationToken))
                throw new InvalidOperationException("Finans ürün kodu benzersiz olmalıdır.");
            var requestedExistingIds = model.Eslesmeler.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
            var actualIds = entity.Eslesmeler.Select(x => x.Id).ToHashSet();
            if (requestedExistingIds.Except(actualIds).Any())
                throw new InvalidOperationException("Ürün eşleşmelerinden en az biri bu ürüne ait değil veya bulunamadı.");
            var auditBefore = CaptureAuditState(entity);
            entity.Kod = code;
            entity.Ad = model.Ad.Trim();
            entity.FiyatlandirmaBirimi = model.FiyatlandirmaBirimi;
            entity.Aktif = model.Aktif;
            entity.Sira = model.Sira;
            ApplyDirectPrice(entity, model);
            AddAuditChanges(nameof(FinansUrun), entity, auditBefore);

            var inputIds = model.Eslesmeler.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
            foreach (var existing in entity.Eslesmeler)
            {
                var input = model.Eslesmeler.FirstOrDefault(x => x.Id == existing.Id);
                if (input is null)
                {
                    AddChangedAudit(nameof(FinansUrunEslesmesi), existing.Id, nameof(existing.Aktif), existing.Aktif, false);
                    existing.Aktif = false;
                    continue;
                }
                var matchAuditBefore = CaptureAuditState(existing);
                ApplyMatch(existing, input);
                AddAuditChanges(nameof(FinansUrunEslesmesi), existing, matchAuditBefore);
            }
            var newMatches = model.Eslesmeler.Where(x => !x.Id.HasValue || !inputIds.Contains(x.Id.Value))
                .Select(NewMatch)
                .ToList();
            foreach (var newMatch in newMatches)
                entity.Eslesmeler.Add(newMatch);

            await _context.SaveChangesAsync(cancellationToken);
            foreach (var newMatch in newMatches)
                AddAudit(nameof(FinansUrunEslesmesi), newMatch.Id, "Oluşturma", "*", null, $"FinansUrunId={entity.Id}");
            if (newMatches.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);
            await BackfillUnpricedWorksAsync(cancellationToken);
            return await ProductByIdAsync(id, tariffDate, cancellationToken);
        }

        private async Task<FinansUrunModel?> ProductByIdAsync(
            int id,
            DateTime tariffDate,
            CancellationToken cancellationToken)
        {
            var entity = await BuildProductByIdQuery(
                    _context.Set<FinansUrun>().AsNoTracking(), id, tariffDate)
                .AsSplitQuery()
                .SingleOrDefaultAsync(cancellationToken);
            return entity is null ? null : MapProduct(entity, tariffDate);
        }

        internal static IQueryable<FinansUrun> BuildProductByIdQuery(
            IQueryable<FinansUrun> query,
            int id,
            DateTime tariffDate)
            => WithProductPageDetails(query.Where(x => x.Id == id), tariffDate);

        public async Task<bool> UrunPasiflestirAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansUrun>().Include(x => x.Eslesmeler)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return false;
            if (!entity.Aktif) return true;
            entity.Aktif = false;
            foreach (var match in entity.Eslesmeler) match.Aktif = false;
            AddAudit(nameof(FinansUrun), id, "Pasifleştirme", nameof(entity.Aktif), true, false);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static FinansUrunEslesmesi NewMatch(FinansUrunEslesmesiModel model)
        {
            var entity = new FinansUrunEslesmesi();
            ApplyMatch(entity, model);
            return entity;
        }

        private static void ApplyDirectPrice(FinansUrun entity, FinansUrunKaydetModel model)
        {
            if (!model.BirimFiyat.HasValue)
                return;
            if (model.BirimFiyat.Value < 0)
                throw new InvalidOperationException("Birim fiyat sıfırdan küçük olamaz.");
            if (model.KdvOrani is < 0 or > 100)
                throw new InvalidOperationException("KDV oranı 0-100 aralığında olmalıdır.");

            var today = TurkeyTime.Now.Date;
            var tariff = entity.FiyatTarifeleri
                .Where(x => x.Aktif && x.GecerlilikBaslangici.Date <= today && x.GecerlilikBitisi.Date >= today)
                .OrderByDescending(x => x.GecerlilikBaslangici)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
            if (tariff is null)
            {
                tariff = new FinansFiyatTarifesi
                {
                    Yil = today.Year,
                    GecerlilikBaslangici = today,
                    GecerlilikBitisi = new DateTime(2200, 12, 31),
                    Aktif = true
                };
                entity.FiyatTarifeleri.Add(tariff);
            }

            tariff.BirimFiyat = model.BirimFiyat.Value;
            tariff.ParaBirimi = NormalizeCurrency(model.ParaBirimi ?? "EUR");
            tariff.KdvOrani = model.KdvOrani ?? 0m;
        }

        private static void ApplyMatch(FinansUrunEslesmesi entity, FinansUrunEslesmesiModel model)
        {
            entity.IsTuru = model.IsTuru;
            entity.SandikAdi = model.SandikAdi?.Trim();
            entity.SandikTipi = model.SandikTipi?.Trim();
            entity.Boy = model.Boy;
            entity.En = model.En;
            entity.Yukseklik = model.Yukseklik;
            entity.IcSandikSablonId = model.IcSandikSablonId;
            entity.Aktif = model.Aktif;
        }

        private static FinansUrunModel MapProduct(FinansUrun entity, DateTime tariffDate)
        {
            var tariff = entity.FiyatTarifeleri.Where(x => x.Aktif && x.GecerlilikBaslangici.Date <= tariffDate && x.GecerlilikBitisi.Date >= tariffDate)
                .OrderByDescending(x => x.GecerlilikBaslangici).ThenByDescending(x => x.Id).FirstOrDefault();
            return new FinansUrunModel
            {
                Id = entity.Id,
                Kod = entity.Kod,
                Ad = entity.Ad,
                FiyatlandirmaBirimi = entity.FiyatlandirmaBirimi,
                Aktif = entity.Aktif,
                Sira = entity.Sira,
                GuncelBirimFiyat = tariff?.BirimFiyat,
                GuncelParaBirimi = tariff?.ParaBirimi,
                GuncelKdvOrani = tariff?.KdvOrani,
                Eslesmeler = entity.Eslesmeler.OrderBy(x => x.Id).Select(x => new FinansUrunEslesmesiModel(
                    x.Id, x.IsTuru, x.SandikAdi, x.SandikTipi, x.Boy, x.En, x.Yukseklik, x.IcSandikSablonId, x.Aktif)).ToArray()
            };
        }

        public async Task<IReadOnlyList<FinansFiyatTarifesiModel>> FiyatTarifeleriAsync(
            int? urunId,
            int? yil,
            bool sadeceAktif,
            CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansFiyatTarifesi>().AsNoTracking().Include(x => x.FinansUrun).AsQueryable();
            if (urunId.HasValue) query = query.Where(x => x.FinansUrunId == urunId);
            if (yil.HasValue) query = query.Where(x => x.Yil == yil);
            if (sadeceAktif) query = query.Where(x => x.Aktif);
            return await query.OrderByDescending(x => x.Yil).ThenBy(x => x.FinansUrun.Ad).ThenBy(x => x.GecerlilikBaslangici)
                .Select(x => new FinansFiyatTarifesiModel(x.Id, x.FinansUrunId, x.FinansUrun.Kod, x.FinansUrun.Ad, x.Yil,
                    x.GecerlilikBaslangici, x.GecerlilikBitisi, x.BirimFiyat, x.ParaBirimi, x.KdvOrani, x.Aktif))
                .ToListAsync(cancellationToken);
        }

        public async Task<FinansSayfaliSonuc<FinansFiyatTarifesiModel>> FiyatTarifeleriSayfaliAsync(
            int? urunId,
            int? yil,
            bool sadeceAktif,
            string? arama,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = ApplyTariffFilter(
                _context.Set<FinansFiyatTarifesi>().AsNoTracking(),
                urunId, yil, sadeceAktif, arama);
            var count = await query.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(pageNumber, pageSize, count, 100);
            var items = await query
                .OrderByDescending(x => x.Yil)
                .ThenBy(x => x.FinansUrun.Ad)
                .ThenBy(x => x.GecerlilikBaslangici)
                .ThenBy(x => x.Id)
                .Skip(skip)
                .Take(size)
                .Select(x => new FinansFiyatTarifesiModel(
                    x.Id, x.FinansUrunId, x.FinansUrun.Kod, x.FinansUrun.Ad, x.Yil,
                    x.GecerlilikBaslangici, x.GecerlilikBitisi, x.BirimFiyat,
                    x.ParaBirimi, x.KdvOrani, x.Aktif))
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansFiyatTarifesiModel>
            {
                Items = items,
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        internal static IQueryable<FinansFiyatTarifesi> ApplyTariffFilter(
            IQueryable<FinansFiyatTarifesi> query,
            int? urunId,
            int? yil,
            bool sadeceAktif,
            string? arama)
        {
            if (urunId.HasValue)
                query = query.Where(x => x.FinansUrunId == urunId.Value);
            if (yil.HasValue)
                query = query.Where(x => x.Yil == yil.Value);
            if (sadeceAktif)
                query = query.Where(x => x.Aktif);
            if (string.IsNullOrWhiteSpace(arama))
                return query;

            var search = arama.Trim().ToLower();
            return query.Where(x =>
                x.FinansUrun.Kod.ToLower().Contains(search) ||
                x.FinansUrun.Ad.ToLower().Contains(search) ||
                x.ParaBirimi.ToLower().Contains(search));
        }

        public Task<FinansFiyatTarifesiModel> FiyatTarifesiOlusturAsync(
            FinansFiyatTarifesiKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => FiyatTarifesiOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansFiyatTarifesiModel> FiyatTarifesiOlusturCoreAsync(
            FinansFiyatTarifesiKaydetModel model,
            CancellationToken cancellationToken)
        {
            await ValidateTariffAsync(null, model, cancellationToken);
            var entity = new FinansFiyatTarifesi();
            ApplyTariff(entity, model);
            _context.Set<FinansFiyatTarifesi>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansFiyatTarifesi), entity.Id, "Oluşturma", "*", null, $"{model.Yil}/{model.BirimFiyat}");
            await _context.SaveChangesAsync(cancellationToken);
            await BackfillUnpricedWorksAsync(cancellationToken);
            return (await FiyatTarifeleriAsync(model.FinansUrunId, null, false, cancellationToken)).Single(x => x.Id == entity.Id);
        }

        public Task<FinansFiyatTarifesiModel?> FiyatTarifesiGuncelleAsync(
            int id,
            FinansFiyatTarifesiKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => FiyatTarifesiGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansFiyatTarifesiModel?> FiyatTarifesiGuncelleCoreAsync(
            int id,
            FinansFiyatTarifesiKaydetModel model,
            CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansFiyatTarifesi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            await ValidateTariffAsync(id, model, cancellationToken);
            var auditBefore = CaptureAuditState(entity);
            ApplyTariff(entity, model);
            AddAuditChanges(nameof(FinansFiyatTarifesi), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            await BackfillUnpricedWorksAsync(cancellationToken);
            return (await FiyatTarifeleriAsync(model.FinansUrunId, null, false, cancellationToken)).Single(x => x.Id == id);
        }

        private async Task BackfillUnpricedWorksAsync(CancellationToken cancellationToken)
        {
            IDbContextTransaction? transaction = null;
            if (_context.Database.CurrentTransaction is null)
                transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var works = await IsKaydiDetayQuery(true)
                    .Where(x => x.KaynakAktif && !x.IptalEdildi &&
                                x.BirimFiyatSnapshot <= 0 && !x.TarifeYiliSnapshot.HasValue &&
                                !x.SiparisKalemleri.Any())
                    .OrderBy(x => x.Id)
                    .ToListAsync(cancellationToken);
                foreach (var work in works)
                {
                    var oldProduct = work.FinansUrunId;
                    var oldUnit = work.FiyatlandirmaBirimiSnapshot;
                    var oldPrice = work.BirimFiyatSnapshot;
                    var oldCurrency = work.ParaBirimiSnapshot;
                    var oldVat = work.KdvOraniSnapshot;
                    var oldYear = work.TarifeYiliSnapshot;
                    await TryApplyAutomaticPriceAsync(work, cancellationToken);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.FinansUrunId), oldProduct, work.FinansUrunId);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.FiyatlandirmaBirimiSnapshot), oldUnit, work.FiyatlandirmaBirimiSnapshot);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.BirimFiyatSnapshot), oldPrice, work.BirimFiyatSnapshot);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.ParaBirimiSnapshot), oldCurrency, work.ParaBirimiSnapshot);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.KdvOraniSnapshot), oldVat, work.KdvOraniSnapshot);
                    AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(work.TarifeYiliSnapshot), oldYear, work.TarifeYiliSnapshot);
                }
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
            finally
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
        }

        private async Task ValidateTariffAsync(int? id, FinansFiyatTarifesiKaydetModel model, CancellationToken cancellationToken)
        {
            if (!await _context.Set<FinansUrun>().AnyAsync(x => x.Id == model.FinansUrunId, cancellationToken))
                throw new InvalidOperationException("Finans ürünü bulunamadı.");
            if (model.GecerlilikBaslangici.Date > model.GecerlilikBitisi.Date)
                throw new InvalidOperationException("Tarife başlangıcı bitişinden sonra olamaz.");
            if (model.GecerlilikBaslangici.Year != model.Yil || model.GecerlilikBitisi.Year != model.Yil)
                throw new InvalidOperationException("Tarife tarihleri seçilen yılın içinde olmalıdır.");
            var overlaps = await _context.Set<FinansFiyatTarifesi>().AnyAsync(x =>
                x.Id != id && x.FinansUrunId == model.FinansUrunId && x.Aktif && model.Aktif &&
                x.GecerlilikBaslangici <= model.GecerlilikBitisi.Date &&
                x.GecerlilikBitisi >= model.GecerlilikBaslangici.Date,
                cancellationToken);
            if (overlaps)
                throw new InvalidOperationException("Aynı ürün için tarih aralığı çakışan aktif bir tarife zaten var.");
        }

        private static void ApplyTariff(FinansFiyatTarifesi entity, FinansFiyatTarifesiKaydetModel model)
        {
            entity.FinansUrunId = model.FinansUrunId;
            entity.Yil = model.Yil;
            entity.GecerlilikBaslangici = model.GecerlilikBaslangici.Date;
            entity.GecerlilikBitisi = model.GecerlilikBitisi.Date;
            entity.BirimFiyat = model.BirimFiyat;
            entity.ParaBirimi = NormalizeCurrency(model.ParaBirimi);
            entity.KdvOrani = model.KdvOrani;
            entity.Aktif = model.Aktif;
        }

        public async Task<FinansRaporModel> RaporVerisiAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var workEntities = await ApplyFilter(IsKaydiDetayQuery(), filtre).OrderBy(x => x.FinansDonemi).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            var works = workEntities.Select(MapIsKaydi).ToArray();
            var expenseQuery = _context.Set<FinansGider>().AsNoTracking().Include(x => x.Kategori).Include(x => x.GiderKalemi).Include(x => x.Proje).AsQueryable();
            if (!filtre.IptalEdilenleriDahilEt) expenseQuery = expenseQuery.Where(x => !x.IptalEdildi);
            if (filtre.ProjeId.HasValue) expenseQuery = expenseQuery.Where(x => x.ProjeId == filtre.ProjeId);
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo))
            {
                var projectNo = filtre.ProjeNo.Trim();
                expenseQuery = expenseQuery.Where(x => x.ManuelProjeNo == projectNo || (x.Proje != null && x.Proje.ProjeNo == projectNo));
            }
            if (filtre.IsTuru.HasValue) expenseQuery = expenseQuery.Where(x => x.IsTuru == filtre.IsTuru);
            if (filtre.Baslangic.HasValue) expenseQuery = expenseQuery.Where(x => x.FinansDonemi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var end = filtre.Bitis.Value.Date.AddDays(1);
                expenseQuery = expenseQuery.Where(x => x.FinansDonemi < end);
            }
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) expenseQuery = expenseQuery.Where(x => x.ParaBirimi == filtre.ParaBirimi.Trim().ToUpper());
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim().ToLower();
                expenseQuery = expenseQuery.Where(x => x.Aciklama.ToLower().Contains(search) ||
                    (x.FirmaVeyaKisi != null && x.FirmaVeyaKisi.ToLower().Contains(search)) ||
                    x.Kategori.Ad.ToLower().Contains(search));
            }
            var expenseTotals = await BuildExpenseTotalsQuery(expenseQuery)
                .OrderBy(x => x.ParaBirimi)
                .ToArrayAsync(cancellationToken);
            var expenses = (await expenseQuery
                    .OrderBy(x => x.FinansDonemi)
                    .ThenBy(x => x.Id)
                    .ToListAsync(cancellationToken))
                .Select(MapExpense)
                .ToArray();

            // Fatura başlığını filtreleyip MapFatura ile tüm kalemleri toplamak proje/iş türü/
            // para birimi filtrelerinde ilgisiz kalemleri gelire katardı. Gelir hesabı doğrudan
            // filtreye uyan fatura kalemleri üzerinden yapılır.
            var invoiceLineQuery = ApplyInvoiceLineFilter(
                _context.Set<FinansFaturaKalemi>().AsNoTracking(),
                filtre);
            var incomes = await BuildInvoiceTotalsQuery(invoiceLineQuery)
                .OrderBy(x => x.ParaBirimi)
                .ToArrayAsync(cancellationToken);
            var currencies = incomes.Select(x => x.ParaBirimi).Union(expenseTotals.Select(x => x.ParaBirimi)).Order().ToArray();
            var net = currencies.Select(currency =>
            {
                var income = incomes.FirstOrDefault(x => x.ParaBirimi == currency);
                var expense = expenseTotals.FirstOrDefault(x => x.ParaBirimi == currency);
                return new FinansParaToplamiModel(currency,
                    (income?.NetTutar ?? 0) - (expense?.NetTutar ?? 0),
                    (income?.KdvTutari ?? 0) - (expense?.KdvTutari ?? 0),
                    (income?.ToplamTutar ?? 0) - (expense?.ToplamTutar ?? 0));
            }).ToArray();
            return new FinansRaporModel
            {
                Filtre = filtre,
                Isler = works,
                Giderler = expenses,
                GelirToplamlari = incomes,
                GiderToplamlari = expenseTotals,
                NetToplamlari = net
            };
        }

        internal static IQueryable<FinansFaturaKalemi> ApplyInvoiceLineFilter(
            IQueryable<FinansFaturaKalemi> query,
            FinansListeFiltre filtre)
        {
            if (filtre.FaturaDurumu == FinansFaturaDurumu.IptalEdildi)
                query = query.Where(x =>
                    x.FinansFatura.IptalEdildi &&
                    x.FinansFatura.Durum == FinansFaturaDurumu.IptalEdildi);
            else
            {
                if (!filtre.IptalEdilenleriDahilEt || filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
                    query = query.Where(x => !x.FinansFatura.IptalEdildi);
                if (filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
                    query = query.Where(x => x.FinansFatura.Durum == FinansFaturaDurumu.Aktif);
            }
            if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansFatura.FaturaTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var end = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.FinansFatura.FaturaTarihi < end);
            }
            if (filtre.ProjeId.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.ProjeId == filtre.ProjeId);
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo == filtre.ProjeNo.Trim());
            if (filtre.IsTuru.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.IsTuru == filtre.IsTuru);
            if (filtre.Durum.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.Durum == filtre.Durum);
            if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.FinansFatura.FinansSiparis.Durum == filtre.SiparisDurumu);
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
            {
                var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
                query = query.Where(x => x.FinansSiparisKalemi.ParaBirimiSnapshot == currency);
            }
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
            {
                var po = filtre.PoNumarasi.Trim().ToLower();
                query = query.Where(x => x.FinansFatura.FinansSiparis.PoNumarasi.ToLower().Contains(po));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var requester = filtre.TalepEden.Trim().ToLower();
                query = query.Where(x =>
                    (x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi != null && x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                    (x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum != null && x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester)));
            }
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim().ToLower();
                query = query.Where(x =>
                    x.FinansFatura.FaturaNumarasi.ToLower().Contains(search) ||
                    x.FinansFatura.FinansSiparis.PoNumarasi.ToLower().Contains(search) ||
                    x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo.ToLower().Contains(search));
            }
            return query;
        }

        public async Task<FinansSayfaliSonuc<FinansDegisiklikModel>> DegisiklikGecmisiAsync(
            string? varlikTuru,
            int? varlikId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansDegisiklikGecmisi>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(varlikTuru)) query = query.Where(x => x.VarlikTuru == varlikTuru.Trim());
            if (varlikId.HasValue) query = query.Where(x => x.VarlikId == varlikId);
            var page = Math.Max(1, pageNumber);
            var size = Math.Clamp(pageSize, 1, 250);
            var count = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id)
                .Skip((page - 1) * size).Take(size)
                .Select(x => new FinansDegisiklikModel(x.Id, x.VarlikTuru, x.VarlikId, x.Islem, x.AlanAdi,
                    x.EskiDeger, x.YeniDeger, x.Aciklama, x.CreatedDate, x.IslemYapan))
                .ToListAsync(cancellationToken);
            return new FinansSayfaliSonuc<FinansDegisiklikModel>
            {
                Items = items,
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

    }
}
