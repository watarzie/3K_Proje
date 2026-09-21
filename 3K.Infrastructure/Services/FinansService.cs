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
        private readonly Guid _auditGroup = Guid.NewGuid();

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
                query = query.Where(x => x.FinansTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.SandikCinsi == filtre.SandikCinsi);
            if (!string.IsNullOrWhiteSpace(filtre.Firma))
            {
                var firma = LiteralSearchPattern(filtre.Firma);
                query = query.Where(x => EF.Functions.ILike(x.Musteri, firma, "\\"));
            }
            if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi))
            {
                var fatura = LiteralSearchPattern(filtre.FaturaNumarasi);
                query = query.Where(x => x.SiparisKalemleri.Any(p => p.FaturaKalemleri.Any(f =>
                    EF.Functions.ILike(f.FinansFatura.FaturaNumarasi, fatura, "\\"))));
            }
            if (filtre.Bitis.HasValue)
            {
                var bitisExclusive = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.FinansTarihi < bitisExclusive);
            }
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
                query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = LiteralSearchPattern(filtre.Arama);
                query = query.Where(x =>
                    EF.Functions.ILike(x.ProjeNo, arama, "\\") ||
                    EF.Functions.ILike(x.Musteri, arama, "\\") ||
                    EF.Functions.ILike(x.IsAdi, arama, "\\") ||
                    (x.SandikNo != null && EF.Functions.ILike(x.SandikNo, arama, "\\")) ||
                    (x.Aciklama != null && EF.Functions.ILike(x.Aciklama, arama, "\\")) ||
                    (x.TalepEdenKisi != null && EF.Functions.ILike(x.TalepEdenKisi, arama, "\\")) ||
                    x.SiparisKalemleri.Any(y => EF.Functions.ILike(y.FinansSiparis.PoNumarasi, arama, "\\") ||
                        y.FaturaKalemleri.Any(z => EF.Functions.ILike(z.FinansFatura.FaturaNumarasi, arama, "\\"))));
            }
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
            {
                var po = LiteralSearchPattern(filtre.PoNumarasi);
                query = query.Where(x => x.SiparisKalemleri.Any(y => !y.FinansSiparis.IptalEdildi && EF.Functions.ILike(y.FinansSiparis.PoNumarasi, po, "\\")));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var talepEden = LiteralSearchPattern(filtre.TalepEden);
                query = query.Where(x =>
                    (x.TalepEdenKisi != null && EF.Functions.ILike(x.TalepEdenKisi, talepEden, "\\")) ||
                    (x.TalepEdenBolum != null && EF.Functions.ILike(x.TalepEdenBolum, talepEden, "\\")));
            }

            return query;
        }

        // Harf eşlemesini tek yerde (PostgreSQL) yap; tr-TR uygulama kültürü ile DB
        // kültürünün farklı küçültmesi aranan I/İ karakterlerini kaybettirmesin.
        // Contains sözleşmesindeki %, _ ve \ karakterleri joker değil, gerçek metindir.
        private static string LiteralSearchPattern(string value)
            => "%" + value.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal) + "%";

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
                FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
                _ => 0m
            };

        private async Task<FinansFiyatTarifesi?> FindTariffAsync(
            int finansUrunId,
            DateTime tarih,
            CancellationToken cancellationToken)
        {
            var tariffs = await _context.Set<FinansFiyatTarifesi>()
                .AsNoTracking()
                .Where(x => x.FinansUrunId == finansUrunId &&
                            x.Aktif &&
                            x.GecerlilikBaslangici.Date <= tarih.Date &&
                            x.GecerlilikBitisi.Date >= tarih.Date)
                .OrderByDescending(x => x.GecerlilikBaslangici)
                .ThenByDescending(x => x.Id)
                .Take(2).ToListAsync(cancellationToken);
            if (tariffs.Count > 1) throw new InvalidOperationException("Çakışan aktif fiyat tarifeleri var; fiyatlandırmadan önce tarifeleri düzeltin.");
            return tariffs.SingleOrDefault();
        }

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
                entity.TarifeIdSnapshot = tarife.Id;
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
                : entity.IsTuru == FinansIsTuru.OzelIs && entity.HesaplamaYontemi == FinansHesaplamaYontemi.SabitAylik
                    ? FinansFiyatlandirmaBirimi.SabitTutar : FinansFiyatlandirmaBirimi.Adet;
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
            var target = _context.ChangeTracker.Entries<BaseEntity>().Select(x => x.Entity)
                .FirstOrDefault(x => x.Id == varlikId && x.GetType().Name == varlikTuru);
            var reference = target switch { FinansIsKaydi x => $"{x.ProjeNo} / {x.IsAdi}", FinansSiparis x => x.PoNumarasi,
                FinansFatura x => x.FaturaNumarasi, FinansGider x => x.BelgeNo ?? x.Aciklama, _ => $"{varlikTuru}/{varlikId}" };
            _context.Set<FinansDegisiklikGecmisi>().Add(new FinansDegisiklikGecmisi
            {
                VarlikTuru = varlikTuru,
                VarlikId = varlikId,
                Islem = islem,
                AlanAdi = alanAdi,
                EskiDeger = ToAuditString(eskiDeger),
                YeniDeger = ToAuditString(yeniDeger),
                Aciklama = aciklama,
                IslemYapan = _currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture) ?? "SYSTEM",
                IslemGrubu = _auditGroup,
                Referans = reference.Length <= 500 ? reference : reference[..500],
                CreatedBy = _currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture)
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
            var money = CalculateMoney(1, FinansTutarKurallari.IsNet(entity), entity.KdvOraniSnapshot);

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
                FinansTarihi = entity.FinansTarihi == default ? entity.FinansDonemi : entity.FinansTarihi,
                FinansTarihiManuel = entity.FinansTarihiManuel,
                FinansMiktariManuel = entity.FinansMiktariManuel,
                KaynakBileseni = entity.KaynakBileseni,
                FiyatlandirmaHazir = money.Net > 0,
                SiparisNetTutar = activeOrderLines.Sum(x => x.NetTutarSnapshot),
                FaturalananNetTutar = activeInvoiceLines.Sum(x => x.NetTutarSnapshot),
                KalanSiparisNetTutar = Math.Max(0, money.Net - activeOrderLines.Sum(x => x.NetTutarSnapshot)),
                KalanFaturaNetTutar = Math.Max(0, activeOrderLines.Sum(x => x.NetTutarSnapshot) - activeInvoiceLines.Sum(x => x.NetTutarSnapshot)),
                SablonSurumId = entity.SablonSurumId,
                ManuelNetTutar = entity.ManuelNetTutar,
                AlanDegerleri = entity.AlanDegerleriJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(entity.AlanDegerleriJson),
                Bilesenler = entity.FiyatBilesenleriJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<FinansFiyatBileseniModel[]>(entity.FiyatBilesenleriJson),
                KayitTarihi = entity.KayitTarihi,
                Durum = DetermineWorkStatus(entity),
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
            return FinansTutarKurallari.Durum(entity);
        }

        private static async Task<IReadOnlyList<FinansParaToplamiModel>> WorkMoneyTotalsAsync(IQueryable<FinansIsKaydi> query, CancellationToken cancellationToken)
            => await query.Select(x => new { Currency = x.ParaBirimiSnapshot, Vat = x.KdvOraniSnapshot,
                Net = x.ManuelNetTutar ?? decimal.Round(x.BirimFiyatSnapshot * (x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet ? x.Adet : x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Metrekup ? x.ToplamM3 : 1m), 2) })
                .GroupBy(x => x.Currency).Select(g => new FinansParaToplamiModel(g.Key, g.Sum(x => x.Net),
                    g.Sum(x => decimal.Round(x.Net * x.Vat / 100m, 2)), g.Sum(x => x.Net + decimal.Round(x.Net * x.Vat / 100m, 2))))
                .ToArrayAsync(cancellationToken);

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
