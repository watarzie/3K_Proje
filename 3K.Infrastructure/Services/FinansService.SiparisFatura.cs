using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var query = SiparisDetayQuery();
            var scopedLines = ScopeOrderLines(_context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
            query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.Id)));
            if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
            query = ApplyFaturalamaBekleyenFilter(query, filtre.FaturalamaBekleyen);
            if (filtre.Baslangic.HasValue) query = query.Where(x => x.SiparisTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var end = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.SiparisTarihi < end);
            }
            if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeId == filtre.ProjeId));
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
            if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.IsTuru == filtre.IsTuru));
            if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.Durum == filtre.Durum));
            if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.Durum == filtre.SiparisDurumu);
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
            {
                var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
                query = query.Where(x => x.Kalemler.Any(y => y.ParaBirimiSnapshot == currency));
            }
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
            {
                var po = filtre.PoNumarasi.Trim().ToLower();
                query = query.Where(x => x.PoNumarasi.ToLower().Contains(po));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var requester = filtre.TalepEden.Trim().ToLower();
                query = query.Where(x => x.Kalemler.Any(y =>
                    (y.FinansIsKaydi.TalepEdenKisi != null && y.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                    (y.FinansIsKaydi.TalepEdenBolum != null && y.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
            }
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim().ToLower();
                query = query.Where(x => x.PoNumarasi.ToLower().Contains(search) ||
                                         x.KayitNo.ToLower().Contains(search) ||
                                         x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo.ToLower().Contains(search)));
            }

            var page = Math.Max(1, filtre.PageNumber);
            var size = Math.Clamp(filtre.PageSize, 1, 250);
            var count = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(x => x.SiparisTarihi).ThenByDescending(x => x.Id)
                .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            return new FinansSayfaliSonuc<FinansSiparisModel>
            {
                Items = entities.Select(x => MapSiparis(x, filtre)).ToArray(),
                Toplamlar = await query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                    .Where(x => scopedLines.Select(s => s.Id).Contains(x.Id)).GroupBy(x => x.ParaBirimiSnapshot)
                    .Select(g => new FinansParaToplamiModel(g.Key, g.Sum(x => x.NetTutarSnapshot), g.Sum(x => x.KdvTutariSnapshot), g.Sum(x => x.ToplamTutarSnapshot))).ToArrayAsync(cancellationToken),
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        internal static IQueryable<FinansSiparis> ApplyFaturalamaBekleyenFilter(
            IQueryable<FinansSiparis> query,
            bool faturalamaBekleyen)
            => !faturalamaBekleyen
                ? query
                : query.Where(x =>
                    !x.IptalEdildi &&
                    (x.Durum == FinansSiparisDurumu.Acik ||
                     x.Durum == FinansSiparisDurumu.KismiFaturalandi));

        public async Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await SiparisDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return entity is null ? null : MapSiparis(entity);
        }

        public async Task<FinansSiparisModel?> SiparisGuncelleAsync(
            int id,
            FinansSiparisGuncelleModel model,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (entity is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
                if (entity.IptalEdildi)
                    throw new InvalidOperationException("İptal edilmiş sipariş güncellenemez.");

                var po = model.PoNumarasi.Trim().ToUpperInvariant();
                if (await _context.Set<FinansSiparis>().AnyAsync(x => x.Id != id && x.PoNumarasi == po, cancellationToken))
                    throw new InvalidOperationException("Bu PO numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");

                var auditBefore = CaptureAuditState(entity);
                entity.PoNumarasi = po;
                entity.SiparisTarihi = model.SiparisTarihi;
                entity.Aciklama = model.Aciklama?.Trim();
                if (model.Kalemler is not null)
                {
                    Gerekce(model.Gerekce);
                    if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.IsKaydiId).Distinct().Count() != model.Kalemler.Count)
                        throw new InvalidOperationException("Tekil mevcut PO kalemlerini seçin.");
                    foreach (var requested in model.Kalemler)
                    {
                        var line = entity.Kalemler.SingleOrDefault(x => x.FinansIsKaydiId == requested.IsKaydiId)
                            ?? throw new InvalidOperationException("Revizyon yalnız bu PO'nun mevcut kalemlerinde yapılabilir.");
                        if (!requested.NetTutar.HasValue) throw new InvalidOperationException("Revizyon net tutarı zorunludur.");
                        var work = await IsKaydiDetayQuery(true).FirstAsync(x => x.Id == requested.IsKaydiId, cancellationToken);
                        var used = work.SiparisKalemleri.Where(x => x.Id != line.Id && !x.FinansSiparis.IptalEdildi).Sum(x => x.NetTutarSnapshot);
                        FinansTutarKurallari.Kapasite(requested.NetTutar.Value, used, FinansTutarKurallari.IsNet(work), "PO revizyonu");
                        var money = CalculateMoney(1, requested.NetTutar.Value, line.KdvOraniSnapshot);
                        var billed = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToArray();
                        if (billed.Sum(x => x.NetTutarSnapshot) > money.Net || billed.Sum(x => x.KdvTutariSnapshot) > money.Kdv || billed.Sum(x => x.ToplamTutarSnapshot) > money.Toplam)
                            throw new InvalidOperationException("PO tutarı aktif faturalanan net/KDV/brüt tutarının altına indirilemez.");
                        var beforeLine = CaptureAuditState(line);
                        line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam;
                        line.TutarBazli = true;
                        AddAuditChanges(nameof(FinansSiparisKalemi), line, beforeLine);
                    }
                    AddAudit(nameof(FinansSiparis), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
                    await _context.SaveChangesAsync(cancellationToken);
                    await RefreshOrderStatusAsync(entity, cancellationToken);
                    await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                }
                AddAuditChanges(nameof(FinansSiparis), entity, auditBefore);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return await SiparisGetirAsync(id, cancellationToken);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<FinansSiparisModel> SiparisOlusturAsync(
            FinansSiparisOlusturModel model,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var po = model.PoNumarasi.Trim().ToUpperInvariant();
                if (await _context.Set<FinansSiparis>().AnyAsync(x => x.PoNumarasi == po, cancellationToken))
                    throw new InvalidOperationException("Bu PO numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");
                var requestedIds = model.Kalemler.Select(x => x.IsKaydiId).Distinct().ToArray();
                if (requestedIds.Length == 0) throw new InvalidOperationException("Sipariş en az bir iş kalemi içermelidir.");
                if (requestedIds.Length != model.Kalemler.Count)
                    throw new InvalidOperationException("Aynı iş kaydı bir siparişte birden fazla kez dağıtılamaz.");
                var works = await IsKaydiDetayQuery(true).Where(x => requestedIds.Contains(x.Id)).ToListAsync(cancellationToken);
                if (works.Count != requestedIds.Length)
                    throw new InvalidOperationException("Sipariş kalemlerinden en az biri bulunamadı.");

                var order = new FinansSiparis
                {
                    KayitNo = NewDocumentNo("SIP"),
                    PoNumarasi = po,
                    SiparisTarihi = model.SiparisTarihi,
                    Aciklama = model.Aciklama?.Trim(),
                    ParaBirimi = string.IsNullOrWhiteSpace(model.ParaBirimi) ? null : NormalizeCurrency(model.ParaBirimi)
                };
                foreach (var requested in model.Kalemler)
                {
                    var work = works.Single(x => x.Id == requested.IsKaydiId);
                    if (work.IptalEdildi || !work.KaynakAktif)
                        throw new InvalidOperationException($"{work.IsAdi} aktif olmadığı için siparişe eklenemez.");
                    var productId = requested.FinansUrunId ?? work.FinansUrunId;
                    var pricingUnit = work.FiyatlandirmaBirimiSnapshot;
                    var unitPrice = requested.BirimFiyat ?? work.BirimFiyatSnapshot;
                    var currency = string.IsNullOrWhiteSpace(requested.ParaBirimi)
                        ? work.ParaBirimiSnapshot
                        : NormalizeCurrency(requested.ParaBirimi);
                    var vat = requested.KdvOrani ?? work.KdvOraniSnapshot;
                    if (productId != work.FinansUrunId || unitPrice != work.BirimFiyatSnapshot || currency != work.ParaBirimiSnapshot || vat != work.KdvOraniSnapshot)
                    {
                        throw new InvalidOperationException("PO işin fiyat snapshot'ını kullanır. Önce ayrı fiyatlandırma işlemiyle iş bedelini düzeltin.");
                    }
                    if (FinansTutarKurallari.IsNet(work) <= 0)
                        throw new InvalidOperationException($"{work.IsAdi} kalemi için geçerli fiyat bulunamadı.");
                    order.ParaBirimi ??= currency;
                    if (order.ParaBirimi != currency)
                        throw new InvalidOperationException("Tek PO yalnız aynı para birimindeki işleri içerebilir.");

                    var activeLines = work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
                    if (activeLines.Any(x => x.FiyatlandirmaBirimiSnapshot != pricingUnit))
                        throw new InvalidOperationException("Kısmi siparişte fiyatlandırma birimi değiştirilemez.");
                    var distribution = requested.NetTutar.HasValue
                        ? new FinansDagitimMiktari(0, 0)
                        : FinansMiktarKurallari.DagitimiNormalizeEt(
                        pricingUnit,
                        requested.Adet,
                        requested.M3,
                        work.Adet,
                        work.ToplamM3,
                        activeLines.Sum(x => x.Adet),
                        activeLines.Sum(x => x.M3),
                        activeLines.Count > 0,
                        "Sipariş");
                    var pricingQuantity = PricingQuantity(pricingUnit, distribution.Adet, distribution.M3);
                    var requestedNet = requested.NetTutar ?? CalculateMoney(pricingQuantity, unitPrice, vat).Net;
                    FinansTutarKurallari.Kapasite(requestedNet, activeLines.Sum(x => x.NetTutarSnapshot), FinansTutarKurallari.IsNet(work), "Sipariş");
                    var money = CalculateMoney(1, requestedNet, vat);
                    order.Kalemler.Add(new FinansSiparisKalemi
                    {
                        FinansIsKaydiId = work.Id,
                        FinansIsKaydi = work,
                        FinansSiparis = order,
                        Adet = distribution.Adet,
                        M3 = distribution.M3,
                        TutarBazli = requested.NetTutar.HasValue,
                        FinansUrunId = productId,
                        FiyatlandirmaBirimiSnapshot = pricingUnit,
                        BirimFiyatSnapshot = unitPrice,
                        ParaBirimiSnapshot = currency,
                        KdvOraniSnapshot = vat,
                        NetTutarSnapshot = money.Net,
                        KdvTutariSnapshot = money.Kdv,
                        ToplamTutarSnapshot = money.Toplam
                    });
                }

                if (model.BelgeNetTutar.HasValue && model.BelgeNetTutar != order.Kalemler.Sum(x => x.NetTutarSnapshot))
                    throw new InvalidOperationException("PO belge net tutarı satırların net tutar toplamına eşit olmalıdır.");
                _context.Set<FinansSiparis>().Add(order);
                await _context.SaveChangesAsync(cancellationToken);
                AddAudit(nameof(FinansSiparis), order.Id, "Oluşturma", "*", null, $"PO: {order.PoNumarasi}");
                await RefreshWorkStatusesAsync(requestedIds, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (await SiparisGetirAsync(order.Id, cancellationToken))!;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        {
            Gerekce(aciklama);
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (entity is null) return false;
                if (entity.IptalEdildi) return true;
                if (entity.Kalemler.SelectMany(x => x.FaturaKalemleri).Any(x => !x.FinansFatura.IptalEdildi))
                    throw new InvalidOperationException("Aktif faturası bulunan sipariş iptal edilemez. Önce faturaları iptal edin.");
                entity.IptalEdildi = true;
                entity.IptalTarihi = TurkeyTime.Now;
                entity.IptalAciklamasi = aciklama.Trim();
                entity.Durum = FinansSiparisDurumu.IptalEdildi;
                AddAudit(nameof(FinansSiparis), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
                await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<bool> SiparisGeriAlAsync(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (entity is null) return false;
                if (!entity.IptalEdildi) return true;
                foreach (var line in entity.Kalemler)
                {
                    var work = await IsKaydiDetayQuery(true).FirstAsync(x => x.Id == line.FinansIsKaydiId, cancellationToken);
                    if (work.IptalEdildi || !work.KaynakAktif)
                        throw new InvalidOperationException("Kaynağı pasif olan işin siparişi aktifleştirilemez.");
                    var otherLines = work.SiparisKalemleri.Where(x => x.FinansSiparisId != entity.Id && !x.FinansSiparis.IptalEdildi).ToList();
                    var allLines = otherLines.Append(line).ToList();
                    if (allLines.Sum(x => x.NetTutarSnapshot) > FinansTutarKurallari.IsNet(work) ||
                        allLines.Any(x => x.ParaBirimiSnapshot != work.ParaBirimiSnapshot))
                        throw new InvalidOperationException("Sipariş geri alındığında iş kaydının kalan miktarı aşılacağı için işlem yapılamadı.");
                }
                entity.IptalEdildi = false;
                entity.IptalTarihi = null;
                entity.IptalAciklamasi = null;
                await RefreshOrderStatusAsync(entity, cancellationToken);
                AddAudit(nameof(FinansSiparis), id, "Aktifleştirme", nameof(entity.IptalEdildi), true, false);
                await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var query = ApplyInvoiceStatusFilter(FaturaDetayQuery(), filtre);
            var scopedLines = ScopeOrderLines(_context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
            query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.FinansSiparisKalemiId)));
            if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower()));
            if (filtre.Baslangic.HasValue) query = query.Where(x => x.FaturaTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var end = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.FaturaTarihi < end);
            }
            if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeId == filtre.ProjeId));
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
            if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.IsTuru == filtre.IsTuru));
            if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.Durum == filtre.Durum));
            if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.FinansSiparis.Durum == filtre.SiparisDurumu);
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
            {
                var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
                query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.ParaBirimiSnapshot == currency));
            }
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
            {
                var po = filtre.PoNumarasi.Trim().ToLower();
                query = query.Where(x => x.FinansSiparis.PoNumarasi.ToLower().Contains(po));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var requester = filtre.TalepEden.Trim().ToLower();
                query = query.Where(x => x.Kalemler.Any(y =>
                    (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                    (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
            }
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim().ToLower();
                query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(search) ||
                                         x.KayitNo.ToLower().Contains(search) ||
                                         x.FinansSiparis.PoNumarasi.ToLower().Contains(search));
            }
            var page = Math.Max(1, filtre.PageNumber);
            var size = Math.Clamp(filtre.PageSize, 1, 250);
            var count = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(x => x.FaturaTarihi).ThenByDescending(x => x.Id)
                .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            return new FinansSayfaliSonuc<FinansFaturaModel>
            {
                Items = entities.Select(x => MapFatura(x, filtre)).ToArray(),
                Toplamlar = await BuildInvoiceTotalsQuery(query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                    .Where(x => scopedLines.Select(l => l.Id).Contains(x.FinansSiparisKalemiId)))
                    .ToArrayAsync(cancellationToken),
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        internal static IQueryable<FinansFatura> ApplyInvoiceStatusFilter(
            IQueryable<FinansFatura> query,
            FinansListeFiltre filtre)
        {
            if (filtre.FaturaDurumu == FinansFaturaDurumu.IptalEdildi)
                return query.Where(x => x.IptalEdildi && x.Durum == FinansFaturaDurumu.IptalEdildi);
            if (filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
                return query.Where(x => !x.IptalEdildi && x.Durum == FinansFaturaDurumu.Aktif);
            return filtre.IptalEdilenleriDahilEt ? query : query.Where(x => !x.IptalEdildi);
        }

        public async Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await FaturaDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return entity is null ? null : MapFatura(entity);
        }

        public async Task<FinansFaturaModel?> FaturaGuncelleAsync(
            int id,
            FinansFaturaGuncelleModel model,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var entity = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (entity is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
                if (entity.IptalEdildi)
                    throw new InvalidOperationException("İptal edilmiş fatura güncellenemez.");

                var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
                if (await _context.Set<FinansFatura>().AnyAsync(x => x.Id != id && x.FaturaNumarasi == invoiceNo, cancellationToken))
                    throw new InvalidOperationException("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");

                var auditBefore = CaptureAuditState(entity);
                entity.FaturaNumarasi = invoiceNo;
                entity.FaturaTarihi = model.FaturaTarihi;
                entity.Aciklama = model.Aciklama?.Trim();
                if (model.Kalemler is not null)
                {
                    Gerekce(model.Gerekce);
                    if (model.BelgeMutabakatiniKoru) throw new InvalidOperationException("Kalem revizyonunda eski belge tutarları korunamaz; gerçek belge toplamlarını yeniden girin.");
                    if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.SiparisKalemiId).Distinct().Count() != model.Kalemler.Count)
                        throw new InvalidOperationException("Tekil mevcut fatura kalemlerini seçin.");
                    var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == entity.FinansSiparisId, cancellationToken);
                    if (order.IptalEdildi) throw new InvalidOperationException("Pasif PO faturası değiştirilemez.");
                    foreach (var requested in model.Kalemler)
                    {
                        var line = entity.Kalemler.SingleOrDefault(x => x.FinansSiparisKalemiId == requested.SiparisKalemiId)
                            ?? throw new InvalidOperationException("Revizyon yalnız bu faturanın mevcut kalemlerinde yapılabilir.");
                        var poLine = order.Kalemler.Single(x => x.Id == line.FinansSiparisKalemiId);
                        var others = poLine.FaturaKalemleri.Where(x => x.Id != line.Id && !x.FinansFatura.IptalEdildi).ToArray();
                        if (!requested.NetTutar.HasValue) throw new InvalidOperationException("Revizyon net tutarı zorunludur.");
                        var net = requested.NetTutar.Value;
                        FinansTutarKurallari.Kapasite(net, others.Sum(x => x.NetTutarSnapshot), poLine.NetTutarSnapshot, "Fatura revizyonu");
                        var money = CalculateMoney(1, net, poLine.KdvOraniSnapshot);
                        if (net + others.Sum(x => x.NetTutarSnapshot) == poLine.NetTutarSnapshot)
                            money = (net, poLine.KdvTutariSnapshot - others.Sum(x => x.KdvTutariSnapshot), net + poLine.KdvTutariSnapshot - others.Sum(x => x.KdvTutariSnapshot));
                        if (money.Kdv < 0 || money.Toplam + others.Sum(x => x.ToplamTutarSnapshot) > poLine.ToplamTutarSnapshot)
                            throw new InvalidOperationException("Fatura revizyonu PO KDV/brüt bakiyesini aşamaz.");
                        var beforeLine = CaptureAuditState(line);
                        line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam; line.TutarBazli = true;
                        AddAuditChanges(nameof(FinansFaturaKalemi), line, beforeLine);
                    }
                    AddAudit(nameof(FinansFatura), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
                    await _context.SaveChangesAsync(cancellationToken);
                    await RefreshOrderStatusAsync(order, cancellationToken);
                    await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                }
                ApplyInvoiceDocumentReconciliationForUpdate(entity, model);
                AddAuditChanges(nameof(FinansFatura), entity, auditBefore);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return await FaturaGetirAsync(id, cancellationToken);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<FinansFaturaModel> FaturaOlusturAsync(
            FinansFaturaOlusturModel model,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
                if (await _context.Set<FinansFatura>().AnyAsync(x => x.FaturaNumarasi == invoiceNo, cancellationToken))
                    throw new InvalidOperationException("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");
                var order = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == model.SiparisId, cancellationToken)
                    ?? throw new InvalidOperationException("Sipariş bulunamadı.");
                if (order.IptalEdildi) throw new InvalidOperationException("İptal edilmiş sipariş faturalanamaz.");
                if (order.Kalemler.Select(x => x.ParaBirimiSnapshot).Distinct().Count() != 1)
                    throw new InvalidOperationException("Fatura oluşturmak için PO satırlarının para birimleri aynı olmalıdır.");
                var requestedIds = model.Kalemler.Select(x => x.SiparisKalemiId).Distinct().ToArray();
                if (requestedIds.Length != model.Kalemler.Count)
                    throw new InvalidOperationException("Aynı sipariş kalemi faturada birden fazla kez kullanılamaz.");
                if (model.Kalemler.Count == 0) throw new InvalidOperationException("En az bir fatura kalemi seçilmelidir.");

                var invoice = new FinansFatura
                {
                    FinansSiparisId = order.Id,
                    KayitNo = NewDocumentNo("FAT"),
                    FaturaNumarasi = invoiceNo,
                    FaturaTarihi = model.FaturaTarihi,
                    Aciklama = model.Aciklama?.Trim()
                };
                foreach (var requested in model.Kalemler)
                {
                    var line = order.Kalemler.FirstOrDefault(x => x.Id == requested.SiparisKalemiId)
                        ?? throw new InvalidOperationException("Fatura kalemi siparişe ait değil.");
                    var activeInvoiceLines = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToList();
                    var distribution = requested.NetTutar.HasValue
                        ? new FinansDagitimMiktari(0, 0)
                        : FinansMiktarKurallari.DagitimiNormalizeEt(
                        line.FiyatlandirmaBirimiSnapshot,
                        requested.Adet,
                        requested.M3,
                        line.Adet,
                        line.M3,
                        activeInvoiceLines.Sum(x => x.Adet),
                        activeInvoiceLines.Sum(x => x.M3),
                        activeInvoiceLines.Count > 0,
                        "Fatura");
                    var pricingQuantity = PricingQuantity(line.FiyatlandirmaBirimiSnapshot, distribution.Adet, distribution.M3);
                    var requestedNet = requested.NetTutar ?? CalculateMoney(pricingQuantity, line.BirimFiyatSnapshot, line.KdvOraniSnapshot).Net;
                    FinansTutarKurallari.Kapasite(requestedNet, activeInvoiceLines.Sum(x => x.NetTutarSnapshot), line.NetTutarSnapshot, "Fatura");
                    var money = CalculateMoney(1, requestedNet, line.KdvOraniSnapshot);
                    if (requestedNet + activeInvoiceLines.Sum(x => x.NetTutarSnapshot) == line.NetTutarSnapshot)
                    {
                        var remainingVat = line.KdvTutariSnapshot - activeInvoiceLines.Sum(x => x.KdvTutariSnapshot);
                        money = (requestedNet, remainingVat, requestedNet + remainingVat);
                    }
                    if (money.Kdv < 0 || money.Toplam + activeInvoiceLines.Sum(x => x.ToplamTutarSnapshot) > line.ToplamTutarSnapshot)
                        throw new InvalidOperationException("Fatura KDV/brüt tutarı PO kaleminin kalanını aşamaz.");
                    invoice.Kalemler.Add(new FinansFaturaKalemi
                    {
                        FinansSiparisKalemiId = line.Id,
                        FinansSiparisKalemi = line,
                        FinansFatura = invoice,
                        TutarBazli = requested.NetTutar.HasValue,
                        Adet = distribution.Adet,
                        M3 = distribution.M3,
                        NetTutarSnapshot = money.Net,
                        KdvTutariSnapshot = money.Kdv,
                        ToplamTutarSnapshot = money.Toplam
                    });
                }

                ApplyInvoiceDocumentReconciliation(
                    invoice,
                    model.BelgeParaBirimi,
                    model.BelgeNetTutar,
                    model.BelgeKdvTutari,
                    model.BelgeToplamTutar,
                    model.MutabakatAciklamasi);

                _context.Set<FinansFatura>().Add(invoice);
                await _context.SaveChangesAsync(cancellationToken);
                AddAudit(nameof(FinansFatura), invoice.Id, "Oluşturma", "*", null, $"Fatura: {invoice.FaturaNumarasi}");
                await RefreshOrderStatusAsync(order, cancellationToken);
                await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (await FaturaDetayQuery().Where(x => x.Id == invoice.Id).Select(x => x).FirstAsync(cancellationToken)) is { } loaded
                    ? MapFatura(loaded)
                    : throw new InvalidOperationException("Fatura oluşturuldu ancak okunamadı.");
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        {
            Gerekce(aciklama);
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var invoice = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (invoice is null) return false;
                if (invoice.IptalEdildi) return true;
                invoice.IptalEdildi = true;
                invoice.IptalTarihi = TurkeyTime.Now;
                invoice.IptalAciklamasi = aciklama.Trim();
                invoice.Durum = FinansFaturaDurumu.IptalEdildi;
                AddAudit(nameof(FinansFatura), id, "İptal", nameof(invoice.IptalEdildi), false, true, aciklama);
                var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == invoice.FinansSiparisId, cancellationToken);
                await RefreshOrderStatusAsync(order, cancellationToken);
                await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        public async Task<bool> FaturaGeriAlAsync(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var invoice = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (invoice is null) return false;
                if (!invoice.IptalEdildi) return true;
                var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == invoice.FinansSiparisId, cancellationToken);
                if (order.IptalEdildi)
                    throw new InvalidOperationException("İptal edilmiş siparişin faturası aktifleştirilemez.");
                foreach (var invoiceLine in invoice.Kalemler)
                {
                    var orderLine = order.Kalemler.Single(x => x.Id == invoiceLine.FinansSiparisKalemiId);
                    var otherActive = orderLine.FaturaKalemleri
                        .Where(x => x.FinansFaturaId != invoice.Id && !x.FinansFatura.IptalEdildi).ToList();
                    var allLines = otherActive.Append(invoiceLine).ToList();
                    if (allLines.Sum(x => x.NetTutarSnapshot) > orderLine.NetTutarSnapshot ||
                        allLines.Sum(x => x.ToplamTutarSnapshot) > orderLine.ToplamTutarSnapshot)
                        throw new InvalidOperationException("Fatura geri alındığında sipariş kaleminin kalan miktarı aşılacağı için işlem yapılamadı.");
                }
                invoice.IptalEdildi = false;
                invoice.IptalTarihi = null;
                invoice.IptalAciklamasi = null;
                invoice.Durum = FinansFaturaDurumu.Aktif;
                AddAudit(nameof(FinansFatura), id, "Aktifleştirme", nameof(invoice.IptalEdildi), true, false);
                await RefreshOrderStatusAsync(order, cancellationToken);
                await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        private Task RefreshOrderStatusAsync(FinansSiparis order, CancellationToken cancellationToken)
        {
            var oldStatus = order.Durum;
            if (order.IptalEdildi)
            {
                order.Durum = FinansSiparisDurumu.IptalEdildi;
            }
            else
            {
                var invoiceLines = order.Kalemler.SelectMany(x => x.FaturaKalemleri)
                    .Where(x => !x.FinansFatura.IptalEdildi).ToList();
                var anyInvoice = order.Kalemler.Any(line =>
                {
                    var lines = invoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
                    return lines.Sum(x => x.NetTutarSnapshot) > 0;
                });
                var fullyInvoiced = order.Kalemler.Count > 0 && order.Kalemler.All(line =>
                {
                    var lines = invoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
                    return line.NetTutarSnapshot > 0 && lines.Sum(x => x.NetTutarSnapshot) >= line.NetTutarSnapshot;
                });
                order.Durum = fullyInvoiced
                    ? FinansSiparisDurumu.Faturalandi
                    : anyInvoice ? FinansSiparisDurumu.KismiFaturalandi : FinansSiparisDurumu.Acik;
            }
            AddChangedAudit(nameof(FinansSiparis), order.Id, nameof(order.Durum), oldStatus, order.Durum);
            return Task.CompletedTask;
        }

        private static FinansSiparisModel MapSiparis(FinansSiparis entity, FinansListeFiltre? filtre = null)
        {
            var selectedOrderLines = filtre is null
                ? entity.Kalemler.ToList()
                : entity.Kalemler.Where(x => SiparisKalemiFiltreyleEslesir(x, filtre, entity)).ToList();
            var activeInvoiceLines = selectedOrderLines.SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var lineModels = selectedOrderLines.Select(line =>
            {
                var invoiceLines = activeInvoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
                var invoicedAdet = invoiceLines.Sum(x => x.Adet);
                var invoicedM3 = invoiceLines.Sum(x => x.M3);
                return new FinansSiparisKalemiModel
                {
                    Id = line.Id,
                    IsKaydiId = line.FinansIsKaydiId,
                    SandikNo = line.FinansIsKaydi.SandikNo ?? string.Empty,
                    SandikAdi = line.FinansIsKaydi.SandikAdi ?? line.FinansIsKaydi.IsAdi,
                    IsTuru = line.FinansIsKaydi.IsTuru,
                    Adet = line.Adet,
                    M3 = line.M3,
                    FaturalananAdet = invoicedAdet,
                    FaturalananM3 = invoicedM3,
                    KalanAdet = Math.Max(0, line.Adet - invoicedAdet),
                    KalanM3 = Math.Max(0, line.M3 - invoicedM3),
                    FinansUrunId = line.FinansUrunId,
                    UrunKodu = line.FinansUrun?.Kod ?? string.Empty,
                    UrunAdi = line.FinansUrun?.Ad ?? string.Empty,
                    FiyatlandirmaBirimi = line.FiyatlandirmaBirimiSnapshot,
                    FiyatlandirmaMiktari = PricingQuantity(line.FiyatlandirmaBirimiSnapshot, line.Adet, line.M3),
                    BirimFiyat = line.BirimFiyatSnapshot,
                    ParaBirimi = line.ParaBirimiSnapshot,
                    KdvOrani = line.KdvOraniSnapshot,
                    NetTutar = line.NetTutarSnapshot,
                    KdvTutari = line.KdvTutariSnapshot,
                    ToplamTutar = line.ToplamTutarSnapshot,
                    FaturalananNetTutar = invoiceLines.Sum(x => x.NetTutarSnapshot),
                    KalanFaturaNetTutar = Math.Max(0, line.NetTutarSnapshot - invoiceLines.Sum(x => x.NetTutarSnapshot)),
                    TutarBazli = line.TutarBazli,
                    FiyatManuelDegistirildi = line.FinansUrunId != line.FinansIsKaydi.FinansUrunId ||
                                             line.BirimFiyatSnapshot != line.FinansIsKaydi.BirimFiyatSnapshot
                };
            }).ToArray();
            var works = selectedOrderLines.Select(x => x.FinansIsKaydi).DistinctBy(x => x.Id).ToList();
            var totals = selectedOrderLines.GroupBy(x => x.ParaBirimiSnapshot)
                .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.NetTutarSnapshot), x.Sum(y => y.KdvTutariSnapshot), x.Sum(y => y.ToplamTutarSnapshot)))
                .OrderBy(x => x.ParaBirimi).ToArray();
            return new FinansSiparisModel
            {
                Id = entity.Id,
                KayitNo = entity.KayitNo,
                PoNumarasi = entity.PoNumarasi,
                ProjeNo = string.Join(", ", works.Select(x => x.ProjeNo).Distinct().Order()),
                Musteri = string.Join(", ", works.Select(x => x.Musteri).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Order()),
                IsTurleri = works.Select(x => x.IsTuru.ToString()).Distinct().Order().ToArray(),
                SiparisTarihi = entity.SiparisTarihi,
                SandikAdedi = selectedOrderLines.Sum(x => x.Adet),
                ToplamM3 = selectedOrderLines.Sum(x => x.M3),
                FaturalananAdet = activeInvoiceLines.Sum(x => x.Adet),
                FaturalananM3 = activeInvoiceLines.Sum(x => x.M3),
                KalanAdet = Math.Max(0, selectedOrderLines.Sum(x => x.Adet) - activeInvoiceLines.Sum(x => x.Adet)),
                KalanM3 = Math.Max(0, selectedOrderLines.Sum(x => x.M3) - activeInvoiceLines.Sum(x => x.M3)),
                Durum = entity.Durum,
                Aciklama = entity.Aciklama,
                Tutarlar = totals,
                Kalemler = lineModels,
                IptalEdildi = entity.IptalEdildi,
                CreatedDate = entity.CreatedDate,
                CreatedBy = entity.CreatedBy
            };
        }

        private static FinansFaturaModel MapFatura(FinansFatura entity, FinansListeFiltre? filtre = null)
        {
            var selectedInvoiceLines = filtre is null
                ? entity.Kalemler.ToList()
                : entity.Kalemler.Where(x => FaturaKalemiFiltreyleEslesir(x, filtre, entity)).ToList();
            var works = selectedInvoiceLines.Select(x => x.FinansSiparisKalemi.FinansIsKaydi).DistinctBy(x => x.Id).ToList();
            var calculatedTotals = selectedInvoiceLines.GroupBy(x => x.FinansSiparisKalemi.ParaBirimiSnapshot)
                .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.NetTutarSnapshot), x.Sum(y => y.KdvTutariSnapshot), x.Sum(y => y.ToplamTutarSnapshot)))
                .OrderBy(x => x.ParaBirimi).ToArray();
            var totals = selectedInvoiceLines.Count == entity.Kalemler.Count &&
                         entity.BelgeNetTutarSnapshot.HasValue &&
                         entity.BelgeKdvTutariSnapshot.HasValue &&
                         entity.BelgeToplamTutarSnapshot.HasValue &&
                         !string.IsNullOrWhiteSpace(entity.BelgeParaBirimiSnapshot)
                ?
                [
                    new FinansParaToplamiModel(
                        entity.BelgeParaBirimiSnapshot,
                        entity.BelgeNetTutarSnapshot.Value,
                        entity.BelgeKdvTutariSnapshot.Value,
                        entity.BelgeToplamTutarSnapshot.Value)
                ]
                : calculatedTotals;
            return new FinansFaturaModel
            {
                Kalemler = selectedInvoiceLines.Select(x => new FinansFaturaKalemiModel(x.Id, x.FinansSiparisKalemiId,
                    x.FinansSiparisKalemi.FinansIsKaydiId, x.NetTutarSnapshot, x.KdvTutariSnapshot, x.ToplamTutarSnapshot,
                    x.FinansSiparisKalemi.ParaBirimiSnapshot, x.TutarBazli)).ToArray(),
                Id = entity.Id,
                KayitNo = entity.KayitNo,
                FaturaNumarasi = entity.FaturaNumarasi,
                FaturaTarihi = entity.FaturaTarihi,
                SiparisId = entity.FinansSiparisId,
                PoNumarasi = entity.FinansSiparis.PoNumarasi,
                ProjeNo = string.Join(", ", works.Select(x => x.ProjeNo).Distinct().Order()),
                IsTurleri = works.Select(x => x.IsTuru.ToString()).Distinct().Order().ToArray(),
                SandikAdedi = selectedInvoiceLines.Sum(x => x.Adet),
                ToplamM3 = selectedInvoiceLines.Sum(x => x.M3),
                Durum = entity.Durum,
                Aciklama = entity.Aciklama,
                Tutarlar = totals,
                BelgeParaBirimi = entity.BelgeParaBirimiSnapshot,
                BelgeNetTutar = entity.BelgeNetTutarSnapshot,
                BelgeKdvTutari = entity.BelgeKdvTutariSnapshot,
                BelgeToplamTutar = entity.BelgeToplamTutarSnapshot,
                MutabakatFarki = entity.MutabakatFarkiSnapshot,
                MutabakatAciklamasi = entity.MutabakatAciklamasi,
                IptalEdildi = entity.IptalEdildi,
                CreatedDate = entity.CreatedDate,
                CreatedBy = entity.CreatedBy
            };
        }

        internal static void ApplyInvoiceDocumentReconciliationForUpdate(
            FinansFatura invoice,
            FinansFaturaGuncelleModel model)
        {
            if (model.BelgeMutabakatiniKoru)
                return;

            ApplyInvoiceDocumentReconciliation(
                invoice,
                model.BelgeParaBirimi,
                model.BelgeNetTutar,
                model.BelgeKdvTutari,
                model.BelgeToplamTutar,
                model.MutabakatAciklamasi);
        }

        private static void ApplyInvoiceDocumentReconciliation(
            FinansFatura invoice,
            string? documentCurrency,
            decimal? documentNet,
            decimal? documentVat,
            decimal? documentTotal,
            string? reconciliationNote)
        {
            var hasAnyDocumentValue = !string.IsNullOrWhiteSpace(documentCurrency) ||
                                      documentNet.HasValue ||
                                      documentVat.HasValue ||
                                      documentTotal.HasValue;
            if (!hasAnyDocumentValue)
            {
                invoice.BelgeParaBirimiSnapshot = null;
                invoice.BelgeNetTutarSnapshot = null;
                invoice.BelgeKdvTutariSnapshot = null;
                invoice.BelgeToplamTutarSnapshot = null;
                invoice.MutabakatFarkiSnapshot = 0;
                invoice.MutabakatAciklamasi = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(documentCurrency) ||
                !documentNet.HasValue ||
                !documentVat.HasValue ||
                !documentTotal.HasValue)
                throw new InvalidOperationException("Belge para birimi, net, KDV ve brüt toplam alanları birlikte girilmelidir.");

            if (documentNet.Value < 0 || documentVat.Value < 0 || documentTotal.Value < 0)
                throw new InvalidOperationException("Belge tutarları negatif olamaz.");
            if (documentNet.Value + documentVat.Value != documentTotal.Value)
                throw new InvalidOperationException("Belge net + KDV toplamı brüt toplamla eşleşmelidir.");

            var lineCurrencies = invoice.Kalemler
                .Select(x => NormalizeCurrency(x.FinansSiparisKalemi.ParaBirimiSnapshot))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (lineCurrencies.Length != 1)
                throw new InvalidOperationException("Farklı para birimindeki fatura kalemleri için tek belge toplamı girilemez.");

            var normalizedCurrency = NormalizeCurrency(documentCurrency);
            if (!string.Equals(lineCurrencies[0], normalizedCurrency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Belge para birimi fatura kalemlerinin para birimiyle eşleşmelidir.");

            var calculatedTotal = invoice.Kalemler.Sum(x => x.ToplamTutarSnapshot);
            if (documentNet.Value != invoice.Kalemler.Sum(x => x.NetTutarSnapshot) ||
                documentVat.Value != invoice.Kalemler.Sum(x => x.KdvTutariSnapshot) ||
                documentTotal.Value != calculatedTotal)
                throw new InvalidOperationException("Belge net/KDV/brüt tutarları satır toplamlarıyla eşleşmelidir. Farkı PO kapasitesini aşmadan satırlara dağıtın.");
            var difference = decimal.Round(documentTotal.Value - calculatedTotal, 2, MidpointRounding.AwayFromZero);
            var normalizedNote = reconciliationNote?.Trim();
            if (Math.Abs(difference) > 0.02m && string.IsNullOrWhiteSpace(normalizedNote))
                throw new InvalidOperationException("Belge ile hesaplanan brüt toplam arasındaki fark için mutabakat açıklaması zorunludur.");

            invoice.BelgeParaBirimiSnapshot = normalizedCurrency;
            invoice.BelgeNetTutarSnapshot = decimal.Round(documentNet.Value, 2, MidpointRounding.AwayFromZero);
            invoice.BelgeKdvTutariSnapshot = decimal.Round(documentVat.Value, 2, MidpointRounding.AwayFromZero);
            invoice.BelgeToplamTutarSnapshot = decimal.Round(documentTotal.Value, 2, MidpointRounding.AwayFromZero);
            invoice.MutabakatFarkiSnapshot = difference;
            invoice.MutabakatAciklamasi = string.IsNullOrWhiteSpace(normalizedNote) ? null : normalizedNote;
        }

        private static IQueryable<FinansSiparisKalemi> ScopeOrderLines(IQueryable<FinansSiparisKalemi> query, FinansListeFiltre filtre)
        {
            if (filtre.ProjeId.HasValue) query = query.Where(x => x.FinansIsKaydi.ProjeId == filtre.ProjeId);
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.FinansIsKaydi.ProjeNo == filtre.ProjeNo);
            if (filtre.IsTuru.HasValue) query = query.Where(x => x.FinansIsKaydi.IsTuru == filtre.IsTuru);
            if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.FinansIsKaydi.SandikCinsi == filtre.SandikCinsi);
            if (!string.IsNullOrWhiteSpace(filtre.Firma)) query = query.Where(x => x.FinansIsKaydi.Musteri.ToLower().Contains(filtre.Firma.ToLower()));
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpper());
            if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaKalemleri.Any(f => f.FinansFatura.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower())));
            return query;
        }

        internal static bool SiparisKalemiFiltreyleEslesir(
            FinansSiparisKalemi line,
            FinansListeFiltre filtre,
            FinansSiparis order)
        {
            var work = line.FinansIsKaydi;
            if (filtre.SandikCinsi.HasValue && work.SandikCinsi != filtre.SandikCinsi) return false;
            if (!string.IsNullOrWhiteSpace(filtre.Firma) && !work.Musteri.Contains(filtre.Firma, StringComparison.OrdinalIgnoreCase)) return false;
            if (filtre.ProjeId.HasValue && work.ProjeId != filtre.ProjeId) return false;
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo) && !string.Equals(work.ProjeNo, filtre.ProjeNo.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
            if (filtre.IsTuru.HasValue && work.IsTuru != filtre.IsTuru) return false;
            if (filtre.Durum.HasValue && work.Durum != filtre.Durum) return false;
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi) && !string.Equals(line.ParaBirimiSnapshot, filtre.ParaBirimi.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
            {
                var requester = filtre.TalepEden.Trim();
                if (!(work.TalepEdenKisi?.Contains(requester, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(work.TalepEdenBolum?.Contains(requester, StringComparison.OrdinalIgnoreCase) ?? false)) return false;
            }
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim();
                var headerMatches = order.PoNumarasi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                    order.KayitNo.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!headerMatches && !work.ProjeNo.Contains(search, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        internal static bool FaturaKalemiFiltreyleEslesir(
            FinansFaturaKalemi line,
            FinansListeFiltre filtre,
            FinansFatura invoice)
        {
            if (!SiparisKalemiFiltreyleEslesir(
                    line.FinansSiparisKalemi,
                    filtre with { Arama = null },
                    invoice.FinansSiparis))
                return false;
            if (string.IsNullOrWhiteSpace(filtre.Arama))
                return true;

            var search = filtre.Arama.Trim();
            return invoice.FaturaNumarasi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   SiparisKalemiFiltreyleEslesir(
                       line.FinansSiparisKalemi,
                       new FinansListeFiltre(Arama: search),
                       invoice.FinansSiparis);
        }
    }
}
