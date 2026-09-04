using System.Globalization;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService : IFinansService, IFinansUretimAktarimService, IFinansAktarimService
    {
        private const decimal Tolerance = 0.000001m;
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public FinansService(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        internal static (int PageNumber, int PageSize, int Skip) NormalizePagination(
            int requestedPageNumber,
            int requestedPageSize,
            int totalCount,
            int maxPageSize)
        {
            var pageSize = Math.Clamp(requestedPageSize, 1, maxPageSize);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var pageNumber = Math.Clamp(requestedPageNumber, 1, totalPages);
            var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
            return (pageNumber, pageSize, skip);
        }

        /// <summary>
        /// Bir finans kaydı ile zorunlu denetim kaydını aynı transaction içinde tutar.
        /// Dışarıdan başlatılmış bir transaction varsa sahiplenmez; böylece üretim ve
        /// dönem üretimi gibi üst seviye akışlarla güvenle birlikte çalışır.
        /// </summary>
        private async Task<T> ExecuteAtomicAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            IDbContextTransaction? ownedTransaction = null;
            if (_context.Database.CurrentTransaction is null)
                ownedTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await operation();
                if (ownedTransaction is not null)
                    await ownedTransaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception exception)
            {
                if (ownedTransaction is not null)
                    await ownedTransaction.RollbackAsync(cancellationToken);

                ThrowIfPersistenceConflict(exception);
                throw;
            }
            finally
            {
                if (ownedTransaction is not null)
                    await ownedTransaction.DisposeAsync();
            }
        }

        private static bool IsUniqueViolation(Exception exception)
            => FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation;

        private static bool IsSerializationFailure(Exception exception)
            => FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure;

        private static void ThrowIfPersistenceConflict(Exception exception)
        {
            if (IsUniqueViolation(exception))
                throw new InvalidOperationException(
                    "Aynı benzersiz değer başka bir işlem tarafından daha önce kaydedildi. Verileri yenileyip tekrar deneyin.",
                    exception);
            if (IsSerializationFailure(exception))
                throw new InvalidOperationException(
                    "Kayıt aynı anda başka bir kullanıcı tarafından değiştirildi. Verileri yenileyip tekrar deneyin.",
                    exception);
        }

        private static PostgresException? FindPostgresException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
                if (current is PostgresException postgresException)
                    return postgresException;
            return null;
        }

        private IQueryable<FinansIsKaydi> IsKaydiDetayQuery(bool tracking = false)
        {
            var query = _context.Set<FinansIsKaydi>()
                .Include(x => x.FinansUrun)
                .Include(x => x.SiparisKalemleri)
                    .ThenInclude(x => x.FinansSiparis)
                .Include(x => x.SiparisKalemleri)
                    .ThenInclude(x => x.FaturaKalemleri)
                        .ThenInclude(x => x.FinansFatura)
                .AsSplitQuery();

            return tracking ? query : query.AsNoTracking();
        }

        private IQueryable<FinansSiparis> SiparisDetayQuery(bool tracking = false)
        {
            var query = _context.Set<FinansSiparis>()
                .Include(x => x.Kalemler)
                    .ThenInclude(x => x.FinansIsKaydi)
                .Include(x => x.Kalemler)
                    .ThenInclude(x => x.FinansUrun)
                .Include(x => x.Kalemler)
                    .ThenInclude(x => x.FaturaKalemleri)
                        .ThenInclude(x => x.FinansFatura)
                .AsSplitQuery();

            return tracking ? query : query.AsNoTracking();
        }

        private IQueryable<FinansFatura> FaturaDetayQuery(bool tracking = false)
        {
            var query = _context.Set<FinansFatura>()
                .Include(x => x.FinansSiparis)
                .Include(x => x.Kalemler)
                    .ThenInclude(x => x.FinansSiparisKalemi)
                        .ThenInclude(x => x.FinansIsKaydi)
                .Include(x => x.Kalemler)
                    .ThenInclude(x => x.FinansSiparisKalemi)
                        .ThenInclude(x => x.FinansUrun)
                .AsSplitQuery();

            return tracking ? query : query.AsNoTracking();
        }

        internal static IQueryable<FinansIsKaydi> ApplyFilter(IQueryable<FinansIsKaydi> query, FinansListeFiltre filtre)
        {
            if (!filtre.IptalEdilenleriDahilEt)
                query = query.Where(x => !x.IptalEdildi && x.KaynakAktif);
            if (filtre.ProjeId.HasValue)
                query = query.Where(x => x.ProjeId == filtre.ProjeId);
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo))
                query = query.Where(x => x.ProjeNo == filtre.ProjeNo.Trim());
            if (filtre.IsTuru.HasValue)
                query = query.Where(x => x.IsTuru == filtre.IsTuru);
            if (filtre.Durum.HasValue)
                query = query.Where(x => x.Durum == filtre.Durum);
            if (filtre.FaturaBekleyen)
                query = query.Where(x =>
                    x.Durum == FinansIsDurumu.SiparisAcildi ||
                    x.Durum == FinansIsDurumu.KismiFaturalandi);
            if (filtre.SiparisDurumu.HasValue)
                query = query.Where(x => x.SiparisKalemleri.Any(y =>
                    !y.FinansSiparis.IptalEdildi && y.FinansSiparis.Durum == filtre.SiparisDurumu));
            if (filtre.FaturaDurumu.HasValue)
            {
                var faturaDurumu = filtre.FaturaDurumu.Value;
                query = faturaDurumu == FinansFaturaDurumu.IptalEdildi
                    ? query.Where(x => x.SiparisKalemleri.Any(y => y.FaturaKalemleri.Any(z =>
                        z.FinansFatura.IptalEdildi &&
                        z.FinansFatura.Durum == FinansFaturaDurumu.IptalEdildi)))
                    : query.Where(x => x.SiparisKalemleri.Any(y => y.FaturaKalemleri.Any(z =>
                        !z.FinansFatura.IptalEdildi && z.FinansFatura.Durum == faturaDurumu)));
            }
            if (filtre.Baslangic.HasValue)
                query = query.Where(x => x.FinansDonemi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var bitisExclusive = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.FinansDonemi < bitisExclusive);
            }
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
                query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim().ToLower();
                query = query.Where(x =>
                    x.ProjeNo.ToLower().Contains(arama) ||
                    x.Musteri.ToLower().Contains(arama) ||
                    x.IsAdi.ToLower().Contains(arama) ||
                    (x.SandikNo != null && x.SandikNo.ToLower().Contains(arama)));
            }
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
            {
                var po = filtre.PoNumarasi.Trim().ToLower();
                query = query.Where(x => x.SiparisKalemleri.Any(y => !y.FinansSiparis.IptalEdildi && y.FinansSiparis.PoNumarasi.ToLower().Contains(po)));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var talepEden = filtre.TalepEden.Trim().ToLower();
                query = query.Where(x =>
                    (x.TalepEdenKisi != null && x.TalepEdenKisi.ToLower().Contains(talepEden)) ||
                    (x.TalepEdenBolum != null && x.TalepEdenBolum.ToLower().Contains(talepEden)));
            }

            return query;
        }

        private static (decimal Net, decimal Kdv, decimal Toplam) CalculateMoney(
            decimal miktar,
            decimal birimFiyat,
            decimal kdvOrani)
        {
            var net = decimal.Round(miktar * birimFiyat, 2, MidpointRounding.AwayFromZero);
            var kdv = decimal.Round(net * kdvOrani / 100m, 2, MidpointRounding.AwayFromZero);
            return (net, kdv, net + kdv);
        }

        private static decimal PricingQuantity(
            FinansFiyatlandirmaBirimi birim,
            decimal adet,
            decimal m3)
            => birim switch
            {
                FinansFiyatlandirmaBirimi.Adet => adet,
                FinansFiyatlandirmaBirimi.Metrekup => m3,
                FinansFiyatlandirmaBirimi.SabitTutar => 1m,
                _ => 0m
            };

        private async Task<FinansFiyatTarifesi?> FindTariffAsync(
            int finansUrunId,
            DateTime tarih,
            CancellationToken cancellationToken)
            => await _context.Set<FinansFiyatTarifesi>()
                .AsNoTracking()
                .Where(x => x.FinansUrunId == finansUrunId &&
                            x.Aktif &&
                            x.GecerlilikBaslangici.Date <= tarih.Date &&
                            x.GecerlilikBitisi.Date >= tarih.Date)
                .OrderByDescending(x => x.GecerlilikBaslangici)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

        private async Task ApplyPriceSnapshotAsync(
            FinansIsKaydi entity,
            int? finansUrunId,
            decimal? manuelBirimFiyat,
            string? paraBirimi,
            decimal? kdvOrani,
            DateTime tarih,
            CancellationToken cancellationToken)
        {
            entity.FinansUrunId = finansUrunId;
            if (finansUrunId.HasValue)
            {
                var urun = await _context.Set<FinansUrun>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == finansUrunId && x.Aktif, cancellationToken)
                    ?? throw new InvalidOperationException("Seçilen finans ürünü bulunamadı veya pasif.");
                var tarife = await FindTariffAsync(urun.Id, tarih, cancellationToken)
                    ?? throw new InvalidOperationException($"{urun.Ad} için {tarih:yyyy} yılına ait geçerli fiyat tarifesi bulunamadı.");

                entity.FiyatlandirmaBirimiSnapshot = urun.FiyatlandirmaBirimi;
                // Ürün seçimi fiyatlandırma birimini sabitler. Yetkili kullanıcı açıkça
                // override gönderdiyse yalnız sağlanan alanlar tarifeyi ezer; sessizce
                // yok sayılması UI ile kaydedilen snapshot'ın farklılaşmasına yol açardı.
                entity.BirimFiyatSnapshot = manuelBirimFiyat ?? tarife.BirimFiyat;
                entity.ParaBirimiSnapshot = string.IsNullOrWhiteSpace(paraBirimi)
                    ? tarife.ParaBirimi
                    : NormalizeCurrency(paraBirimi);
                entity.KdvOraniSnapshot = kdvOrani ?? tarife.KdvOrani;
                entity.TarifeYiliSnapshot = tarife.Yil;
                return;
            }

            if (!manuelBirimFiyat.HasValue)
            {
                entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
                    ? FinansFiyatlandirmaBirimi.Metrekup
                    : FinansFiyatlandirmaBirimi.Adet;
                entity.BirimFiyatSnapshot = 0;
                entity.ParaBirimiSnapshot = NormalizeCurrency(paraBirimi ?? "EUR");
                entity.KdvOraniSnapshot = kdvOrani ?? 0;
                entity.TarifeYiliSnapshot = null;
                return;
            }

            entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
                ? FinansFiyatlandirmaBirimi.Metrekup
                : FinansFiyatlandirmaBirimi.Adet;
            entity.BirimFiyatSnapshot = manuelBirimFiyat.Value;
            entity.ParaBirimiSnapshot = NormalizeCurrency(paraBirimi ?? "EUR");
            entity.KdvOraniSnapshot = kdvOrani ?? 0;
            entity.TarifeYiliSnapshot = null;
        }

        private static string NormalizeCurrency(string value)
        {
            var normalized = value.Trim().ToUpperInvariant();
            if (normalized.Length != 3)
                throw new InvalidOperationException("Para birimi ISO-4217 biçiminde üç karakter olmalıdır.");
            return normalized;
        }

        private async Task<(int? ProjeId, string ProjeNo, string Musteri, bool Manuel)> ResolveProjectAsync(
            int? projeId,
            string? manuelProjeNo,
            string? manuelProjeAdi,
            string? musteri,
            CancellationToken cancellationToken)
        {
            if (projeId.HasValue && !string.IsNullOrWhiteSpace(manuelProjeNo))
                throw new InvalidOperationException("Sistem projesi ile manuel proje aynı anda seçilemez.");
            if (projeId.HasValue)
            {
                var proje = await _context.Set<Proje>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == projeId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Proje bulunamadı.");
                return (proje.Id, proje.ProjeNo, proje.Musteri, false);
            }

            if (string.IsNullOrWhiteSpace(manuelProjeNo))
                return (null, "BAĞIMSIZ", string.IsNullOrWhiteSpace(musteri) ? manuelProjeAdi?.Trim() ?? string.Empty : musteri.Trim(), true);

            return (null, manuelProjeNo.Trim(), string.IsNullOrWhiteSpace(musteri) ? manuelProjeAdi?.Trim() ?? string.Empty : musteri.Trim(), true);
        }

        private void AddAudit(
            string varlikTuru,
            int varlikId,
            string islem,
            string alanAdi,
            object? eskiDeger,
            object? yeniDeger,
            string? aciklama = null)
        {
            _context.Set<FinansDegisiklikGecmisi>().Add(new FinansDegisiklikGecmisi
            {
                VarlikTuru = varlikTuru,
                VarlikId = varlikId,
                Islem = islem,
                AlanAdi = alanAdi,
                EskiDeger = ToAuditString(eskiDeger),
                YeniDeger = ToAuditString(yeniDeger),
                Aciklama = aciklama,
                IslemYapan = _currentUser.UserId?.ToString(CultureInfo.InvariantCulture) ?? "SYSTEM",
                CreatedBy = _currentUser.UserId?.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string? ToAuditString(object? value)
            => value switch
            {
                null => null,
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                decimal number => number.ToString(CultureInfo.InvariantCulture),
                bool flag => flag ? "true" : "false",
                Enum enumValue => $"{Convert.ToInt32(enumValue, CultureInfo.InvariantCulture)}:{enumValue}",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };

        private void AddChangedAudit<T>(string varlikTuru, int varlikId, string alan, T oldValue, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return;
            AddAudit(varlikTuru, varlikId, "Güncelleme", alan, oldValue, newValue);
        }

        private static IReadOnlyDictionary<string, object?> CaptureAuditState(BaseEntity entity)
            => entity.GetType()
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.CanRead && IsAuditScalar(property.PropertyType))
                .Where(property => property.Name is not nameof(BaseEntity.Id)
                    and not nameof(BaseEntity.CreatedDate)
                    and not nameof(BaseEntity.UpdatedDate)
                    and not nameof(BaseEntity.CreatedBy)
                    and not nameof(BaseEntity.UpdatedBy))
                .ToDictionary(property => property.Name, property => property.GetValue(entity));

        private void AddAuditChanges(string varlikTuru, BaseEntity entity, IReadOnlyDictionary<string, object?> before)
        {
            var type = entity.GetType();
            foreach (var pair in before)
            {
                var current = type.GetProperty(pair.Key)?.GetValue(entity);
                if (Equals(pair.Value, current))
                    continue;
                AddAudit(varlikTuru, entity.Id, "Güncelleme", pair.Key, pair.Value, current);
            }
        }

        private static bool IsAuditScalar(Type type)
        {
            var actual = Nullable.GetUnderlyingType(type) ?? type;
            return actual.IsEnum || actual == typeof(string) || actual == typeof(bool) ||
                   actual == typeof(byte) || actual == typeof(short) || actual == typeof(int) ||
                   actual == typeof(long) || actual == typeof(decimal) || actual == typeof(float) ||
                   actual == typeof(double) || actual == typeof(DateTime) || actual == typeof(Guid);
        }

        private static string NewDocumentNo(string prefix)
            => $"{prefix}-{TurkeyTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..Math.Min(31, prefix.Length + 1 + 14 + 1 + 32)];

        private static FinansIsKaydiModel MapIsKaydi(FinansIsKaydi entity)
        {
            var activeOrderLines = entity.SiparisKalemleri
                .Where(x => !x.FinansSiparis.IptalEdildi)
                .ToList();
            var activeInvoiceLines = activeOrderLines
                .SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi)
                .ToList();
            var orderedAdet = activeOrderLines.Sum(x => x.Adet);
            var orderedM3 = activeOrderLines.Sum(x => x.M3);
            var invoicedAdet = activeInvoiceLines.Sum(x => x.Adet);
            var invoicedM3 = activeInvoiceLines.Sum(x => x.M3);
            var priceQuantity = PricingQuantity(entity.FiyatlandirmaBirimiSnapshot, entity.Adet, entity.ToplamM3);
            var money = CalculateMoney(priceQuantity, entity.BirimFiyatSnapshot, entity.KdvOraniSnapshot);

            return new FinansIsKaydiModel
            {
                Id = entity.Id,
                ProjeId = entity.ProjeId,
                ProjeNo = entity.ProjeNo,
                Musteri = entity.Musteri,
                ManuelProjeMi = entity.ManuelProjeMi,
                IsAdi = entity.IsAdi,
                OzelIsTuru = entity.OzelIsTuru,
                HesaplamaYontemi = entity.HesaplamaYontemi,
                RaporGrubu = entity.RaporGrubu,
                Aciklama = entity.Aciklama,
                TalepEdenKisi = entity.TalepEdenKisi,
                TalepEdenBolum = entity.TalepEdenBolum,
                IsTuru = entity.IsTuru,
                SandikNo = entity.SandikNo,
                SandikAdi = entity.SandikAdi,
                SandikTipi = entity.SandikTipi,
                Boy = entity.Boy,
                En = entity.En,
                Yukseklik = entity.Yukseklik,
                IcSandikSablonId = entity.IcSandikSablonId,
                Adet = entity.Adet,
                Birim = entity.Birim,
                BirimM3 = entity.BirimM3,
                ToplamM3 = entity.ToplamM3,
                FinansUrunId = entity.FinansUrunId,
                FiyatlandirmaBirimi = entity.FiyatlandirmaBirimiSnapshot,
                BirimFiyat = entity.BirimFiyatSnapshot,
                ParaBirimi = entity.ParaBirimiSnapshot,
                KdvOrani = entity.KdvOraniSnapshot,
                TarifeYili = entity.TarifeYiliSnapshot,
                NetTutar = money.Net,
                KdvTutari = money.Kdv,
                ToplamTutar = money.Toplam,
                UretimTarihi = entity.UretimTarihi,
                FinansDonemi = entity.FinansDonemi,
                KayitTarihi = entity.KayitTarihi,
                Durum = entity.Durum,
                SiparisAdedi = orderedAdet,
                SiparisM3 = orderedM3,
                SiparisBekleyenAdet = Math.Max(0, entity.Adet - orderedAdet),
                SiparisBekleyenM3 = Math.Max(0, entity.ToplamM3 - orderedM3),
                FaturalananAdet = invoicedAdet,
                FaturalananM3 = invoicedM3,
                PoNumaralari = activeOrderLines.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                FaturaNumaralari = activeInvoiceLines.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                KaynakTuru = entity.KaynakTuru,
                KaynakKayitId = entity.KaynakKayitId,
                KaynakAktif = entity.KaynakAktif,
                IptalEdildi = entity.IptalEdildi,
                IptalAciklamasi = entity.IptalAciklamasi,
                CreatedDate = entity.CreatedDate,
                CreatedBy = entity.CreatedBy
            };
        }

        private static FinansIsDurumu DetermineWorkStatus(FinansIsKaydi entity)
        {
            if (entity.IptalEdildi || !entity.KaynakAktif)
                return FinansIsDurumu.IptalEdildi;

            var activeOrderLines = entity.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
            if (activeOrderLines.Count == 0)
                return FinansIsDurumu.SiparisBekliyor;

            var pricingUnit = activeOrderLines.Select(x => x.FiyatlandirmaBirimiSnapshot).Distinct().Count() == 1
                ? activeOrderLines[0].FiyatlandirmaBirimiSnapshot
                : entity.FiyatlandirmaBirimiSnapshot;
            var orderedAdet = activeOrderLines.Sum(x => x.Adet);
            var orderedM3 = activeOrderLines.Sum(x => x.M3);
            if (!FinansMiktarKurallari.DagitimVar(pricingUnit, orderedAdet, orderedM3, activeOrderLines.Count > 0))
                return FinansIsDurumu.SiparisBekliyor;

            var activeInvoiceLines = activeOrderLines.SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var invoicedAdet = activeInvoiceLines.Sum(x => x.Adet);
            var invoicedM3 = activeInvoiceLines.Sum(x => x.M3);
            var fullyOrdered = FinansMiktarKurallari.TamamiDagitildi(
                pricingUnit, entity.Adet, entity.ToplamM3, orderedAdet, orderedM3, activeOrderLines.Count > 0);
            var fullyInvoiced = FinansMiktarKurallari.TamamiDagitildi(
                pricingUnit, orderedAdet, orderedM3, invoicedAdet, invoicedM3, activeInvoiceLines.Count > 0);

            if (fullyOrdered && fullyInvoiced)
                return FinansIsDurumu.Faturalandi;
            if (FinansMiktarKurallari.DagitimVar(pricingUnit, invoicedAdet, invoicedM3, activeInvoiceLines.Count > 0))
                return FinansIsDurumu.KismiFaturalandi;
            return fullyOrdered ? FinansIsDurumu.SiparisAcildi : FinansIsDurumu.KismiSiparis;
        }

        private async Task RefreshWorkStatusesAsync(IEnumerable<int> workIds, CancellationToken cancellationToken)
        {
            var ids = workIds.Distinct().ToArray();
            if (ids.Length == 0)
                return;
            var works = await IsKaydiDetayQuery(true).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
            foreach (var work in works)
            {
                var oldStatus = work.Durum;
                work.Durum = DetermineWorkStatus(work);
                AddChangedAudit(nameof(FinansIsKaydi), work.Id, nameof(FinansIsKaydi.Durum), oldStatus, work.Durum);
            }
        }
    }
}
