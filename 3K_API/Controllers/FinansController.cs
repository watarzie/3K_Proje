using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Services;
using _3K.Infrastructure.Data;

namespace _3K_API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FinansController : ControllerBase
    {
        private const decimal Tolerans = 0.000001m;
        private readonly AppDbContext _context;
        private readonly IFinansSenkronService _senkronService;
        private readonly IFinansRaporService _raporService;
        private readonly IFinansBelgeService _belgeService;
        private readonly IFinansDonemService _donemService;
        private readonly IFinansAylikService _aylikService;

        public FinansController(
            AppDbContext context,
            IFinansSenkronService senkronService,
            IFinansRaporService raporService,
            IFinansBelgeService belgeService,
            IFinansDonemService donemService,
            IFinansAylikService aylikService)
        {
            _context = context;
            _senkronService = senkronService;
            _raporService = raporService;
            _belgeService = belgeService;
            _donemService = donemService;
            _aylikService = aylikService;
        }

        [HttpGet("aylik-isler")]
        public async Task<ActionResult<IReadOnlyList<FinansAylikIsDto>>> AylikIsler([FromQuery] int yil, [FromQuery] int ay, CancellationToken cancellationToken)
        {
            try { return Ok(await _aylikService.ListeleAsync(yil, ay, cancellationToken)); }
            catch (ArgumentOutOfRangeException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<FinansDashboardDto>> Dashboard(CancellationToken cancellationToken)
        {
            var isler = _context.FinansIsKayitlari.AsNoTracking().Where(k => k.KaynakAktif);
            var siparisler = _context.FinansSiparisleri.AsNoTracking();
            var faturalar = _context.FinansFaturalari.AsNoTracking();
            var ayBasi = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var sonrakiAy = ayBasi.AddMonths(1);

            return Ok(new FinansDashboardDto(
                await isler.CountAsync(cancellationToken),
                await isler.Where(k => k.IsTuru != FinansIsTuru.SarfKereste)
                    .SumAsync(k => (decimal?)k.Adet, cancellationToken) ?? 0,
                await isler.SumAsync(k => (decimal?)(k.Adet * k.BirimM3), cancellationToken) ?? 0,
                await isler.CountAsync(k => !k.SiparisKalemleri.Any(s => s.Siparis.Durum != FinansSiparisDurumu.IptalEdildi), cancellationToken),
                await siparisler.CountAsync(s => s.Durum == FinansSiparisDurumu.Acildi, cancellationToken),
                await siparisler.CountAsync(s => s.Durum == FinansSiparisDurumu.KismiAcildi, cancellationToken),
                await siparisler.CountAsync(s => s.Durum != FinansSiparisDurumu.IptalEdildi &&
                    s.Kalemler.Sum(k => k.M3) > s.Kalemler.SelectMany(k => k.FaturaKalemleri)
                        .Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(k => k.M3), cancellationToken),
                await faturalar.CountAsync(f => f.Durum != FinansFaturaDurumu.IptalEdildi, cancellationToken),
                await _context.FinansOzelIsleri.AsNoTracking().CountAsync(o => !o.IptalEdildi && o.IsTarihi >= ayBasi && o.IsTarihi < sonrakiAy, cancellationToken),
                await _context.FinansGiderleri.AsNoTracking().Where(g => !g.IptalEdildi && g.Tarih >= ayBasi && g.Tarih < sonrakiAy)
                    .SumAsync(g => (decimal?)g.ToplamTutar, cancellationToken) ?? 0));
        }

        [HttpGet("projeler")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansProjeOzetDto>>> Projeler([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansIsKayitlari.AsNoTracking().Where(k => k.KaynakAktif);
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim();
                query = query.Where(k => k.ProjeNo.Contains(arama) || k.Musteri.Contains(arama));
            }
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(k => k.ProjeNo.Contains(filtre.ProjeNo));
            if (!string.IsNullOrWhiteSpace(filtre.Musteri)) query = query.Where(k => k.Musteri.Contains(filtre.Musteri));

            var grouped = query.GroupBy(k => new { k.ProjeId, k.ProjeNo, k.Musteri });
            var totalCount = await grouped.CountAsync(cancellationToken);
            var rows = await grouped
                .OrderByDescending(g => g.Max(k => k.UretimeAlinmaTarihi))
                .ThenBy(g => g.Key.ProjeNo)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(g => new
                {
                    g.Key.ProjeId, g.Key.ProjeNo, g.Key.Musteri,
                    IsAdedi = g.Count(k => k.IsTuru == FinansIsTuru.NormalSandik),
                    Sandik = g.Where(k => k.IsTuru == FinansIsTuru.NormalSandik).Sum(k => k.Adet),
                    Toplam = g.Where(k => k.IsTuru == FinansIsTuru.NormalSandik).Sum(k => k.Adet * k.BirimM3),
                    Siparis = g.Where(k => k.IsTuru == FinansIsTuru.NormalSandik)
                        .Sum(k => k.SiparisKalemleri.Where(s => s.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).Sum(s => s.M3)),
                    Fatura = g.Where(k => k.IsTuru == FinansIsTuru.NormalSandik)
                        .Sum(k => k.SiparisKalemleri.SelectMany(s => s.FaturaKalemleri)
                        .Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.M3)),
                    SonTarih = g.Max(k => k.UretimeAlinmaTarihi)
                }).ToListAsync(cancellationToken);

            var normalTarife = await _context.FinansUrunleri.AsNoTracking()
                .Where(u => u.Aktif && u.Eslesmeler.Any(e => e.Aktif && e.IsTuru == FinansIsTuru.NormalSandik && e.SandikAdi == null))
                .OrderBy(u => u.Sira).ThenBy(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var projeIds = rows.Where(x => x.ProjeId.HasValue).Select(x => x.ProjeId!.Value).Distinct().ToArray();
            var projeSiparisleri = await SiparisSorgusu()
                .Where(s => s.Durum != FinansSiparisDurumu.IptalEdildi && s.ProjeId.HasValue && projeIds.Contains(s.ProjeId.Value))
                .ToListAsync(cancellationToken);

            var items = rows.Select(x =>
            {
                var siparisBekleyen = Math.Max(0, x.Toplam - x.Siparis);
                var faturaBekleyen = Math.Max(0, x.Siparis - x.Fatura);
                var durum = siparisBekleyen > Tolerans ? "Sipariş Bekliyor" : faturaBekleyen > Tolerans ? "Fatura Bekliyor" : "Tamamlandı";
                var siparisler = projeSiparisleri.Where(s => s.ProjeId == x.ProjeId).ToList();
                var fiyatlandirmaM3 = Math.Round(x.Toplam, 2, MidpointRounding.AwayFromZero);
                var netTutar = normalTarife == null ? 0 : ParaYuvarla(fiyatlandirmaM3 * normalTarife.BirimFiyat);
                var kdvTutari = normalTarife == null ? 0 : ParaYuvarla(netTutar * normalTarife.KdvOrani / 100);
                var faturaBekleyenSiparis = siparisler
                    .Where(s => s.Kalemler.Sum(k => k.M3) - s.Kalemler.SelectMany(k => k.FaturaKalemleri)
                        .Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.M3) > Tolerans)
                    .OrderByDescending(s => s.SiparisTarihi).ThenByDescending(s => s.Id).FirstOrDefault();
                return new FinansProjeOzetDto(x.ProjeId, x.ProjeNo, x.Musteri, x.IsAdedi, x.Sandik, x.Toplam,
                    x.Siparis, siparisBekleyen, x.Fatura, faturaBekleyen, x.SonTarih, durum,
                    normalTarife?.BirimFiyat ?? 0, normalTarife?.ParaBirimi ?? "EUR", normalTarife?.KdvOrani ?? 0,
                    netTutar, kdvTutari, netTutar + kdvTutari, normalTarife == null,
                    siparisler.Select(s => s.PoNumarasi).Distinct().ToArray(),
                    siparisler.SelectMany(s => s.Kalemler).SelectMany(k => k.FaturaKalemleri)
                        .Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi)
                        .Select(f => f.Fatura.FaturaNumarasi).Distinct().ToArray(),
                    faturaBekleyenSiparis?.Id);
            }).ToList();
            return Ok(Sayfali(items, page, pageSize, totalCount));
        }

        [HttpGet("is-kayitlari")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansIsKaydiDto>>> IsKayitlari([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansIsKayitlari.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim();
                query = query.Where(k => k.ProjeNo.Contains(arama) || k.Musteri.Contains(arama) || k.SandikNo.Contains(arama) || k.SandikAdi.Contains(arama));
            }
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(k => k.ProjeNo.Contains(filtre.ProjeNo));
            if (!string.IsNullOrWhiteSpace(filtre.Musteri)) query = query.Where(k => k.Musteri.Contains(filtre.Musteri));
            if (filtre.Baslangic.HasValue) query = query.Where(k => k.UretimeAlinmaTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue) query = query.Where(k => k.UretimeAlinmaTarihi < filtre.Bitis.Value.Date.AddDays(1));
            if (Enum.TryParse<FinansIsTuru>(filtre.IsTuru, true, out var isTuru)) query = query.Where(k => k.IsTuru == isTuru);

            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(k => k.UretimeAlinmaTarihi).ThenByDescending(k => k.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().ToListAsync(cancellationToken);
            return Ok(Sayfali(entities.Select(IsKaydiDto).ToList(), page, pageSize, totalCount));
        }

        [HttpGet("projeler/{projeId:int}")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansIsKaydiDto>>> ProjeDetay(int projeId, [FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansIsKayitlari.AsNoTracking().Where(k => k.ProjeId == projeId);
            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderBy(k => k.IsTuru).ThenBy(k => k.SandikNo)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().ToListAsync(cancellationToken);
            return Ok(Sayfali(entities.Select(IsKaydiDto).ToList(), page, pageSize, totalCount));
        }

        [HttpGet("is-kayitlari/{id:int}")]
        public async Task<ActionResult<FinansIsKaydiDto>> IsKaydiDetay(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansIsKayitlari.AsNoTracking()
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
            return entity == null ? NotFound() : Ok(IsKaydiDto(entity));
        }

        [HttpGet("siparisler")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansSiparisDto>>> Siparisler([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansSiparisleri.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim();
                query = query.Where(s => s.PoNumarasi.Contains(arama) || s.AnaProjeNo.Contains(arama) || s.KayitNo.Contains(arama));
            }
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(s => s.AnaProjeNo.Contains(filtre.ProjeNo));
            if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi)) query = query.Where(s => s.PoNumarasi.Contains(filtre.PoNumarasi));
            if (filtre.SiparisDurumu.HasValue) query = query.Where(s => (int)s.Durum == filtre.SiparisDurumu.Value);
            if (filtre.Baslangic.HasValue) query = query.Where(s => s.SiparisTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue) query = query.Where(s => s.SiparisTarihi < filtre.Bitis.Value.Date.AddDays(1));
            if (filtre.Belgeli.HasValue) query = query.Where(s => s.Belgeler.Any() == filtre.Belgeli.Value);
            var totalCount = await query.CountAsync(cancellationToken);
            var ids = await query.OrderByDescending(s => s.SiparisTarihi).ThenByDescending(s => s.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(s => s.Id).ToListAsync(cancellationToken);
            var entities = await SiparisSorgusu().Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
            var items = ids.Select(id => SiparisDto(entities.Single(s => s.Id == id))).ToList();
            return Ok(Sayfali(items, page, pageSize, totalCount));
        }

        [HttpGet("siparisler/{id:int}")]
        public async Task<ActionResult<FinansSiparisDetayDto>> SiparisDetay(int id, CancellationToken cancellationToken)
        {
            var entity = await SiparisSorgusu().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            var kalemler = entity.Kalemler.Select(k => new FinansSiparisKalemiDto(k.Id, k.IsKaydiId, k.IsKaydi.SandikNo, k.IsKaydi.SandikAdi,
                k.IsKaydi.IsTuru, k.Adet, k.M3,
                k.FaturaKalemleri.Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.Adet),
                k.FaturaKalemleri.Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.M3),
                Math.Max(0, k.Adet - k.FaturaKalemleri.Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.Adet)),
                Math.Max(0, k.M3 - k.FaturaKalemleri.Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.M3)),
                k.UrunId, k.UrunKodu, k.UrunAdi, k.FiyatlandirmaBirimi, k.FiyatlandirmaMiktari, k.BirimFiyat,
                k.ParaBirimi, k.KdvOrani, k.NetTutar, k.KdvTutari, k.ToplamTutar, k.FiyatManuelDegistirildi)).ToList();
            return Ok(new FinansSiparisDetayDto(SiparisDto(entity), kalemler, entity.Belgeler.Select(BelgeDto).ToList(), entity.CreatedDate, entity.CreatedBy));
        }

        [HttpPost("siparisler")]
        public async Task<ActionResult<FinansSiparisDto>> SiparisOlustur([FromBody] FinansSiparisOlusturRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PoNumarasi) || request.Kalemler.Count == 0) return BadRequest("PO numarası ve en az bir kalem zorunludur.");
            if (request.Kalemler.GroupBy(k => k.IsKaydiId).Any(g => g.Count() > 1)) return BadRequest("Aynı iş kaydı bir siparişte bir kez kullanılabilir.");
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var ids = request.Kalemler.Select(k => k.IsKaydiId).ToArray();
            var isler = await _context.FinansIsKayitlari.Include(k => k.OzelIs).Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Where(k => ids.Contains(k.Id) && k.KaynakAktif).ToListAsync(cancellationToken);
            if (isler.Count != ids.Length) return BadRequest("Sipariş kalemlerinden biri bulunamadı veya aktif değil.");
            if (isler.Select(k => k.ProjeId).Distinct().Count() != 1) return BadRequest("Bir sipariş yalnız tek projeye ait kalemler içerebilir.");
            var urunler = await _context.FinansUrunleri.AsNoTracking().Where(u => u.Aktif)
                .Include(u => u.Eslesmeler.Where(e => e.Aktif)).ToListAsync(cancellationToken);

            var siparis = new FinansSiparis
            {
                KayitNo = KayitNo("SIP"), PoNumarasi = request.PoNumarasi.Trim(), SiparisTarihi = request.SiparisTarihi.Date,
                Aciklama = Temizle(request.Aciklama), ProjeId = isler[0].ProjeId, AnaProjeNo = isler[0].ProjeNo
            };
            var kismi = false;
            foreach (var dagitim in request.Kalemler)
            {
                var isKaydi = isler.Single(k => k.Id == dagitim.IsKaydiId);
                if (dagitim.Adet < 0 || dagitim.M3 < 0 || (dagitim.Adet <= 0 && dagitim.M3 <= 0)) return BadRequest("Sipariş miktarı sıfırdan büyük olmalıdır.");
                var kullanilanAdet = isKaydi.SiparisKalemleri.Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).Sum(k => k.Adet);
                var kullanilanM3 = isKaydi.SiparisKalemleri.Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).Sum(k => k.M3);
                if (dagitim.Adet - (isKaydi.Adet - kullanilanAdet) > Tolerans || dagitim.M3 - (isKaydi.ToplamM3 - kullanilanM3) > Tolerans)
                    return BadRequest($"{isKaydi.SandikNo} için sipariş miktarı kalan miktarı aşıyor.");
                if (isKaydi.Adet - kullanilanAdet - dagitim.Adet > Tolerans || isKaydi.ToplamM3 - kullanilanM3 - dagitim.M3 > Tolerans) kismi = true;
                var urun = dagitim.UrunId.HasValue
                    ? urunler.FirstOrDefault(u => u.Id == dagitim.UrunId.Value)
                    : FinansTarifeSecici.Sec(urunler, isKaydi);
                if (dagitim.UrunId.HasValue && urun == null) return BadRequest($"{isKaydi.SandikNo} için seçilen ürün aktif değil veya bulunamadı.");
                var duzenliTarife = isKaydi.OzelIs is { DuzenliIsId: not null, BirimFiyat: > 0 } ? isKaydi.OzelIs : null;
                if (urun == null && duzenliTarife == null) return BadRequest($"{isKaydi.SandikNo} için aktif fiyat tarifesi tanımlı değil.");
                var birim = urun?.FiyatlandirmaBirimi ?? FinansFiyatlandirmaBirimi.Adet;
                var miktar = birim == FinansFiyatlandirmaBirimi.M3 ? dagitim.M3 : dagitim.Adet;
                var birimFiyat = urun?.BirimFiyat ?? duzenliTarife!.BirimFiyat;
                var paraBirimi = urun?.ParaBirimi ?? duzenliTarife!.ParaBirimi;
                var kdvOrani = urun?.KdvOrani ?? duzenliTarife!.KdvOrani;
                if (birimFiyat < 0 || kdvOrani is < 0 or > 100 || paraBirimi.Length != 3)
                    return BadRequest($"{isKaydi.SandikNo} için fiyat, KDV veya para birimi geçersiz.");
                var netTutar = ParaYuvarla(miktar * birimFiyat);
                var kdvTutari = ParaYuvarla(netTutar * kdvOrani / 100);
                siparis.Kalemler.Add(new FinansSiparisKalemi
                {
                    IsKaydiId = isKaydi.Id, UrunId = urun?.Id, Adet = dagitim.Adet, M3 = dagitim.M3,
                    UrunKodu = urun?.Kod ?? $"DUZ-{duzenliTarife!.DuzenliIsId}", UrunAdi = urun?.Ad ?? duzenliTarife!.IsAdi,
                    FiyatlandirmaBirimi = birim, FiyatlandirmaMiktari = miktar, BirimFiyat = birimFiyat,
                    ParaBirimi = paraBirimi, KdvOrani = kdvOrani, NetTutar = netTutar, KdvTutari = kdvTutari,
                    ToplamTutar = netTutar + kdvTutari,
                    FiyatManuelDegistirildi = false
                });
            }
            SiparisTutarlariniDengele(siparis.Kalemler);
            siparis.Durum = kismi ? FinansSiparisDurumu.KismiAcildi : FinansSiparisDurumu.Acildi;
            _context.FinansSiparisleri.Add(siparis);
            await _context.SaveChangesAsync(cancellationToken);
            Gecmis("Sipariş", siparis.Id, "Oluşturuldu", null, siparis.PoNumarasi, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var created = await SiparisSorgusu().SingleAsync(s => s.Id == siparis.Id, cancellationToken);
            return CreatedAtAction(nameof(SiparisDetay), new { id = siparis.Id }, SiparisDto(created));
        }

        [HttpPut("siparisler/{id:int}")]
        public async Task<ActionResult<FinansSiparisDto>> SiparisGuncelle(int id, [FromBody] FinansSiparisGuncelleRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansSiparisleri.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            if (entity.Durum == FinansSiparisDurumu.IptalEdildi) return BadRequest("İptal edilmiş sipariş güncellenemez.");
            if (string.IsNullOrWhiteSpace(request.PoNumarasi)) return BadRequest("PO numarası zorunludur.");
            var eski = entity.PoNumarasi;
            entity.PoNumarasi = request.PoNumarasi.Trim(); entity.SiparisTarihi = request.SiparisTarihi.Date; entity.Aciklama = Temizle(request.Aciklama);
            Gecmis("Sipariş", id, "Güncellendi", eski, entity.PoNumarasi, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(SiparisDto(await SiparisSorgusu().SingleAsync(s => s.Id == id, cancellationToken)));
        }

        [HttpPost("siparisler/{id:int}/iptal")]
        public async Task<IActionResult> SiparisIptal(int id, [FromBody] FinansIptalRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansSiparisleri.Include(s => s.Kalemler).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            if (entity.Kalemler.SelectMany(k => k.FaturaKalemleri).Any(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi))
                return BadRequest("Aktif faturası bulunan sipariş iptal edilemez.");
            entity.Durum = FinansSiparisDurumu.IptalEdildi; entity.IptalTarihi = DateTime.Now; entity.IptalAciklamasi = Temizle(request.Aciklama);
            Gecmis("Sipariş", id, "İptal Edildi", null, null, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("faturalar")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansFaturaDto>>> Faturalar([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansFaturalari.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim();
                query = query.Where(f => f.FaturaNumarasi.Contains(arama) || f.KayitNo.Contains(arama) || f.Siparis.PoNumarasi.Contains(arama) || f.Siparis.AnaProjeNo.Contains(arama));
            }
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(f => f.Siparis.AnaProjeNo.Contains(filtre.ProjeNo));
            if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(f => f.FaturaNumarasi.Contains(filtre.FaturaNumarasi));
            if (filtre.FaturaDurumu.HasValue) query = query.Where(f => (int)f.Durum == filtre.FaturaDurumu.Value);
            if (filtre.Baslangic.HasValue) query = query.Where(f => f.FaturaTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue) query = query.Where(f => f.FaturaTarihi < filtre.Bitis.Value.Date.AddDays(1));
            if (filtre.Belgeli.HasValue) query = query.Where(f => f.Belgeler.Any() == filtre.Belgeli.Value);
            var totalCount = await query.CountAsync(cancellationToken);
            var ids = await query.OrderByDescending(f => f.FaturaTarihi).ThenByDescending(f => f.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(f => f.Id).ToListAsync(cancellationToken);
            var entities = await FaturaSorgusu().Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);
            return Ok(Sayfali(ids.Select(id => FaturaDto(entities.Single(f => f.Id == id))).ToList(), page, pageSize, totalCount));
        }

        [HttpGet("faturalar/{id:int}")]
        public async Task<ActionResult<FinansFaturaDetayDto>> FaturaDetay(int id, CancellationToken cancellationToken)
        {
            var entity = await FaturaSorgusu().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            var kalemler = entity.Kalemler.Select(k => new FinansFaturaKalemiDto(k.Id, k.SiparisKalemiId, k.SiparisKalemi.IsKaydi.SandikNo,
                k.SiparisKalemi.IsKaydi.SandikAdi, k.Adet, k.M3)).ToList();
            return Ok(new FinansFaturaDetayDto(FaturaDto(entity), kalemler, entity.Belgeler.Select(BelgeDto).ToList(), entity.CreatedDate, entity.CreatedBy));
        }

        [HttpPost("faturalar")]
        public async Task<ActionResult<FinansFaturaDto>> FaturaOlustur([FromBody] FinansFaturaOlusturRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FaturaNumarasi) || request.Kalemler.Count == 0) return BadRequest("Fatura numarası ve en az bir kalem zorunludur.");
            if (request.Kalemler.GroupBy(k => k.SiparisKalemiId).Any(g => g.Count() > 1)) return BadRequest("Aynı sipariş kalemi bir faturada bir kez kullanılabilir.");
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var siparis = await SiparisSorgusu(false).FirstOrDefaultAsync(s => s.Id == request.SiparisId, cancellationToken);
            if (siparis == null || siparis.Durum == FinansSiparisDurumu.IptalEdildi) return BadRequest("Sipariş bulunamadı veya iptal edilmiş.");
            var ids = request.Kalemler.Select(k => k.SiparisKalemiId).ToArray();
            if (ids.Any(id => siparis.Kalemler.All(k => k.Id != id))) return BadRequest("Fatura kalemi seçilen siparişe ait değil.");
            var fatura = new FinansFatura
            {
                KayitNo = KayitNo("FAT"), SiparisId = siparis.Id, FaturaNumarasi = request.FaturaNumarasi.Trim(),
                FaturaTarihi = request.FaturaTarihi.Date, Aciklama = Temizle(request.Aciklama)
            };
            foreach (var dagitim in request.Kalemler)
            {
                var kalem = siparis.Kalemler.Single(k => k.Id == dagitim.SiparisKalemiId);
                if (dagitim.Adet < 0 || dagitim.M3 < 0 || (dagitim.Adet <= 0 && dagitim.M3 <= 0)) return BadRequest("Fatura miktarı sıfırdan büyük olmalıdır.");
                var faturalananAdet = kalem.FaturaKalemleri.Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(k => k.Adet);
                var faturalananM3 = kalem.FaturaKalemleri.Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(k => k.M3);
                if (dagitim.Adet - (kalem.Adet - faturalananAdet) > Tolerans || dagitim.M3 - (kalem.M3 - faturalananM3) > Tolerans)
                    return BadRequest($"{kalem.IsKaydi.SandikNo} için fatura miktarı kalan sipariş miktarını aşıyor.");
                fatura.Kalemler.Add(new FinansFaturaKalemi { SiparisKalemiId = kalem.Id, Adet = dagitim.Adet, M3 = dagitim.M3 });
            }
            var yeniAdet = request.Kalemler.Sum(k => k.Adet);
            var yeniM3 = request.Kalemler.Sum(k => k.M3);
            var kalanAdet = siparis.Kalemler.Sum(k => k.Adet) - siparis.Kalemler.SelectMany(k => k.FaturaKalemleri)
                .Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(k => k.Adet) - yeniAdet;
            var kalanM3 = siparis.Kalemler.Sum(k => k.M3) - siparis.Kalemler.SelectMany(k => k.FaturaKalemleri)
                .Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(k => k.M3) - yeniM3;
            fatura.Durum = kalanAdet > Tolerans || kalanM3 > Tolerans ? FinansFaturaDurumu.KismiFaturalandi : FinansFaturaDurumu.Faturalandi;
            _context.FinansFaturalari.Add(fatura);
            await _context.SaveChangesAsync(cancellationToken);
            Gecmis("Fatura", fatura.Id, "Oluşturuldu", null, fatura.FaturaNumarasi, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CreatedAtAction(nameof(FaturaDetay), new { id = fatura.Id }, FaturaDto(await FaturaSorgusu().SingleAsync(f => f.Id == fatura.Id, cancellationToken)));
        }

        [HttpPut("faturalar/{id:int}")]
        public async Task<ActionResult<FinansFaturaDto>> FaturaGuncelle(int id, [FromBody] FinansFaturaGuncelleRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansFaturalari.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            if (entity.Durum == FinansFaturaDurumu.IptalEdildi) return BadRequest("İptal edilmiş fatura güncellenemez.");
            if (string.IsNullOrWhiteSpace(request.FaturaNumarasi)) return BadRequest("Fatura numarası zorunludur.");
            var eski = entity.FaturaNumarasi;
            entity.FaturaNumarasi = request.FaturaNumarasi.Trim(); entity.FaturaTarihi = request.FaturaTarihi.Date; entity.Aciklama = Temizle(request.Aciklama);
            Gecmis("Fatura", id, "Güncellendi", eski, entity.FaturaNumarasi, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(FaturaDto(await FaturaSorgusu().SingleAsync(f => f.Id == id, cancellationToken)));
        }

        [HttpPost("faturalar/{id:int}/iptal")]
        public async Task<IActionResult> FaturaIptal(int id, [FromBody] FinansIptalRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansFaturalari.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            entity.Durum = FinansFaturaDurumu.IptalEdildi; entity.IptalTarihi = DateTime.Now; entity.IptalAciklamasi = Temizle(request.Aciklama);
            Gecmis("Fatura", id, "İptal Edildi", null, null, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("ozel-isler")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansOzelIsDto>>> OzelIsler([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize);
            var query = _context.FinansOzelIsleri.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama)) { var arama = filtre.Arama.Trim(); query = query.Where(o => o.IsAdi.Contains(arama) || o.Musteri.Contains(arama) || o.KayitNo.Contains(arama)); }
            if (!string.IsNullOrWhiteSpace(filtre.Musteri)) query = query.Where(o => o.Musteri.Contains(filtre.Musteri));
            if (filtre.Baslangic.HasValue) query = query.Where(o => o.IsTarihi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue) query = query.Where(o => o.IsTarihi < filtre.Bitis.Value.Date.AddDays(1));
            if (filtre.Belgeli.HasValue) query = query.Where(o => o.Belgeler.Any() == filtre.Belgeli.Value);
            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(o => o.IsTarihi).ThenByDescending(o => o.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Include(o => o.Proje).Include(o => o.Belgeler).Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().ToListAsync(cancellationToken);
            return Ok(Sayfali(entities.Select(OzelIsDto).ToList(), page, pageSize, totalCount));
        }

        [HttpPost("ozel-isler")]
        public async Task<ActionResult<FinansOzelIsDto>> OzelIsOlustur([FromBody] FinansOzelIsKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = OzelIsDogrula(request.IsTuru, request.Musteri, request.IsAdi, request.Miktar, request.Birim,
                request.HesaplamaYontemi, request.RaporGrubu, request.BirimFiyat, request.ParaBirimi, request.KdvOrani);
            if (hata != null) return BadRequest(hata);
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var entity = new FinansOzelIs { KayitNo = KayitNo("OZL") };
            OzelIsDoldur(entity, request.IsTuru, request.Musteri, request.ProjeId, request.IsAdi, request.Aciklama, request.Miktar,
                request.Birim, request.IsTarihi, request.HesaplamaYontemi, request.RaporGrubu, request.BirimFiyat, request.ParaBirimi, request.KdvOrani);
            _context.FinansOzelIsleri.Add(entity); await _context.SaveChangesAsync(cancellationToken);
            var proje = request.ProjeId.HasValue ? await _context.Projeler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProjeId, cancellationToken) : null;
            _context.FinansIsKayitlari.Add(new FinansIsKaydi { OzelIsId = entity.Id, ProjeId = entity.ProjeId, KaynakModul = "FinansOzelIs", KaynakKayitId = entity.Id,
                ProjeNo = proje?.ProjeNo ?? string.Empty, Musteri = entity.Musteri, SandikNo = entity.KayitNo, SandikAdi = entity.IsAdi, IsTuru = FinansIsTuru.OzelIs,
                Adet = entity.Miktar, BirimM3 = 0, UretimeAlinmaTarihi = entity.IsTarihi, UretimDurumu = "Özel İş", AktarimTarihi = DateTime.Now });
            Gecmis("Özel İş", entity.Id, "Oluşturuldu", null, entity.IsAdi, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return CreatedAtAction(nameof(OzelIsler), new { id = entity.Id }, OzelIsDto(entity));
        }

        [HttpPut("ozel-isler/{id:int}")]
        public async Task<ActionResult<FinansOzelIsDto>> OzelIsGuncelle(int id, [FromBody] FinansOzelIsGuncelleRequest request, CancellationToken cancellationToken)
        {
            var hata = OzelIsDogrula(request.IsTuru, request.Musteri, request.IsAdi, request.Miktar, request.Birim,
                request.HesaplamaYontemi, request.RaporGrubu, request.BirimFiyat, request.ParaBirimi, request.KdvOrani); if (hata != null) return BadRequest(hata);
            var entity = await _context.FinansOzelIsleri.Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (entity == null) return NotFound(); if (entity.IptalEdildi) return BadRequest("İptal edilmiş özel iş güncellenemez.");
            if (AktifSiparisiVar(entity)) return BadRequest("Aktif PO bulunan özel iş güncellenemez.");
            var eski = $"{entity.Miktar} {entity.Birim}; {entity.BirimFiyat} {entity.ParaBirimi}";
            OzelIsDoldur(entity, request.IsTuru, request.Musteri, request.ProjeId, request.IsAdi, request.Aciklama, request.Miktar,
                request.Birim, request.IsTarihi, request.HesaplamaYontemi, request.RaporGrubu, request.BirimFiyat, request.ParaBirimi, request.KdvOrani);
            if (entity.FinansKaydi != null) { entity.FinansKaydi.ProjeId = entity.ProjeId; entity.FinansKaydi.Musteri = entity.Musteri; entity.FinansKaydi.SandikAdi = entity.IsAdi; entity.FinansKaydi.Adet = entity.Miktar; entity.FinansKaydi.UretimeAlinmaTarihi = entity.IsTarihi; }
            Gecmis("Özel İş", id, "Güncellendi", eski, $"{entity.Miktar} {entity.Birim}; {entity.BirimFiyat} {entity.ParaBirimi}", request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken); return Ok(OzelIsDto(entity));
        }

        [HttpPut("ozel-isler/{id:int}/aylik-deger")]
        public async Task<IActionResult> OzelIsAylikDegerGuncelle(int id, [FromBody] FinansAylikIsGuncelleRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansOzelIsleri.Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            if (entity.IptalEdildi) return BadRequest("İptal edilmiş özel iş güncellenemez.");
            if (AktifSiparisiVar(entity)) return BadRequest("Aktif PO bulunan özel iş güncellenemez.");
            if (!request.Miktar.HasValue && !request.NetBirimFiyat.HasValue) return BadRequest("Miktar veya net birim fiyat gönderilmelidir.");
            if (request.Miktar is <= 0 || request.NetBirimFiyat is < 0) return BadRequest("Miktar pozitif, net birim fiyat sıfır veya daha büyük olmalıdır.");
            if (request.Miktar.HasValue && entity.HesaplamaYontemi != FinansHesaplamaYontemi.DegiskenAdet) return BadRequest("Bu işin miktarı düzenlenemez.");
            if (request.NetBirimFiyat.HasValue && entity.HesaplamaYontemi != FinansHesaplamaYontemi.DegiskenTutar) return BadRequest("Bu işin tutarı düzenlenemez.");
            var eski = $"Miktar={entity.Miktar};BirimFiyat={entity.BirimFiyat}";
            if (request.Miktar.HasValue) { entity.Miktar = request.Miktar.Value; if (entity.FinansKaydi != null) entity.FinansKaydi.Adet = entity.Miktar; }
            if (request.NetBirimFiyat.HasValue) entity.BirimFiyat = request.NetBirimFiyat.Value;
            Gecmis("Özel İş", id, "Aylık Değer Güncellendi", eski, $"Miktar={entity.Miktar};BirimFiyat={entity.BirimFiyat}", null);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("ozel-isler/{id:int}/iptal")]
        public async Task<IActionResult> OzelIsIptal(int id, [FromBody] FinansIptalRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Aciklama)) return BadRequest("İptal gerekçesi zorunludur.");
            var entity = await _context.FinansOzelIsleri.Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken); if (entity == null) return NotFound();
            if (AktifSiparisiVar(entity)) return BadRequest("Aktif PO bulunan özel iş iptal edilemez.");
            entity.IptalEdildi = true; entity.IptalTarihi = DateTime.Now; entity.IptalAciklamasi = Temizle(request.Aciklama); if (entity.FinansKaydi != null) entity.FinansKaydi.KaynakAktif = false;
            Gecmis("Özel İş", id, "İptal Edildi", null, null, request.Aciklama);
            await _context.SaveChangesAsync(cancellationToken); return NoContent();
        }

        [HttpPost("ozel-isler/{id:int}/geri-al")]
        public async Task<IActionResult> OzelIsGeriAl(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansOzelIsleri.Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            if (!entity.IptalEdildi) return BadRequest("Özel iş iptal edilmiş değil.");
            if (AktifSiparisiVar(entity) || entity.FinansKaydi?.SiparisKalemleri.SelectMany(k => k.FaturaKalemleri).Any(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi) == true)
                return BadRequest("Aktif PO veya faturası bulunan özel iş geri alınamaz.");
            var gerekce = entity.IptalAciklamasi;
            entity.IptalEdildi = false; entity.IptalTarihi = null; entity.IptalAciklamasi = null;
            if (entity.FinansKaydi != null) entity.FinansKaydi.KaynakAktif = true;
            Gecmis("Özel İş", id, "Geri Alındı", gerekce, null, null);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("duzenli-isler")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansDuzenliIsDto>>> DuzenliIsler([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize); var query = _context.FinansDuzenliIsleri.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama)) { var arama = filtre.Arama.Trim(); query = query.Where(d => d.IsAdi.Contains(arama) || d.Musteri.Contains(arama)); }
            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(d => d.Aktif).ThenBy(d => d.IsAdi).Skip((page - 1) * pageSize).Take(pageSize).Include(d => d.Proje).ToListAsync(cancellationToken);
            return Ok(Sayfali(entities.Select(DuzenliIsDto).ToList(), page, pageSize, totalCount));
        }

        [HttpPost("duzenli-isler")]
        public async Task<ActionResult<FinansDuzenliIsDto>> DuzenliIsOlustur([FromBody] FinansDuzenliIsKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = DuzenliIsDogrula(request); if (hata != null) return BadRequest(hata);
            var entity = new FinansDuzenliIs(); DuzenliIsDoldur(entity, request); _context.FinansDuzenliIsleri.Add(entity); await _context.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(DuzenliIsler), new { id = entity.Id }, DuzenliIsDto(entity));
        }

        [HttpPut("duzenli-isler/{id:int}")]
        public async Task<ActionResult<FinansDuzenliIsDto>> DuzenliIsGuncelle(int id, [FromBody] FinansDuzenliIsKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = DuzenliIsDogrula(request); if (hata != null) return BadRequest(hata);
            var entity = await _context.FinansDuzenliIsleri.FirstOrDefaultAsync(d => d.Id == id, cancellationToken); if (entity == null) return NotFound();
            DuzenliIsDoldur(entity, request); await _context.SaveChangesAsync(cancellationToken); return Ok(DuzenliIsDto(entity));
        }

        [HttpPost("duzenli-isler/donem-olustur")]
        public async Task<ActionResult<FinansDonemOlusturSonuc>> DonemOlustur([FromQuery] DateTime? referansTarihi, CancellationToken cancellationToken) =>
            Ok(await _donemService.OlusturAsync((referansTarihi ?? DateTime.Today).Date, cancellationToken));

        [HttpGet("giderler")]
        public async Task<ActionResult<FinansSayfaliSonuc<FinansGiderDto>>> Giderler([FromQuery] FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Sayfa(filtre.Page, filtre.PageSize); var query = _context.FinansGiderleri.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtre.Arama)) { var arama = filtre.Arama.Trim(); query = query.Where(g => g.Aciklama.Contains(arama) || (g.FirmaVeyaKisi != null && g.FirmaVeyaKisi.Contains(arama))); }
            if (filtre.Baslangic.HasValue) query = query.Where(g => g.Tarih >= filtre.Baslangic.Value.Date); if (filtre.Bitis.HasValue) query = query.Where(g => g.Tarih < filtre.Bitis.Value.Date.AddDays(1));
            if (filtre.Belgeli.HasValue) query = query.Where(g => g.Belgeler.Any() == filtre.Belgeli.Value);
            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(g => g.Tarih).ThenByDescending(g => g.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Include(g => g.Kategori).Include(g => g.Proje).Include(g => g.Belgeler).ToListAsync(cancellationToken);
            return Ok(Sayfali(entities.Select(GiderDto).ToList(), page, pageSize, totalCount));
        }

        [HttpPost("giderler")]
        public async Task<ActionResult<FinansGiderDto>> GiderOlustur([FromBody] FinansGiderKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = await GiderDogrula(request, cancellationToken); if (hata != null) return BadRequest(hata);
            var entity = new FinansGider(); GiderDoldur(entity, request); _context.FinansGiderleri.Add(entity); await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(entity).Reference(g => g.Kategori).LoadAsync(cancellationToken); return CreatedAtAction(nameof(Giderler), new { id = entity.Id }, GiderDto(entity));
        }

        [HttpPut("giderler/{id:int}")]
        public async Task<ActionResult<FinansGiderDto>> GiderGuncelle(int id, [FromBody] FinansGiderGuncelleRequest request, CancellationToken cancellationToken)
        {
            var kaydet = new FinansGiderKaydetRequest(request.Tarih, request.KategoriId, request.AltKategori, request.FirmaVeyaKisi, request.Aciklama, request.Tutar, request.ParaBirimi, request.KdvDahil, request.KdvOrani, request.ProjeId, request.IsTuru);
            var hata = await GiderDogrula(kaydet, cancellationToken); if (hata != null) return BadRequest(hata);
            var entity = await _context.FinansGiderleri.FirstOrDefaultAsync(g => g.Id == id, cancellationToken); if (entity == null) return NotFound(); if (entity.IptalEdildi) return BadRequest("İptal edilmiş gider güncellenemez.");
            GiderDoldur(entity, kaydet); await _context.SaveChangesAsync(cancellationToken); await _context.Entry(entity).Reference(g => g.Kategori).LoadAsync(cancellationToken); return Ok(GiderDto(entity));
        }

        [HttpPost("giderler/{id:int}/iptal")]
        public async Task<IActionResult> GiderIptal(int id, [FromBody] FinansIptalRequest request, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansGiderleri.FirstOrDefaultAsync(g => g.Id == id, cancellationToken); if (entity == null) return NotFound();
            entity.IptalEdildi = true; entity.IptalTarihi = DateTime.Now; entity.IptalAciklamasi = Temizle(request.Aciklama); await _context.SaveChangesAsync(cancellationToken); return NoContent();
        }

        [HttpGet("gider-kategorileri")]
        public async Task<ActionResult<IReadOnlyList<FinansGiderKategoriDto>>> GiderKategorileri(CancellationToken cancellationToken) =>
            Ok(await _context.FinansGiderKategorileri.AsNoTracking().OrderBy(k => k.Ad).Select(k => new FinansGiderKategoriDto(k.Id, k.Ad, k.Aktif)).ToListAsync(cancellationToken));

        [HttpGet("is-turleri")]
        public async Task<ActionResult<IReadOnlyList<FinansIsTuruTanimiDto>>> IsTurleri(CancellationToken cancellationToken) =>
            Ok(await _context.FinansIsTuruTanimlari.AsNoTracking().OrderBy(k => k.Sira).Select(k => new FinansIsTuruTanimiDto(k.Id, k.Ad, k.Aktif, k.Sira)).ToListAsync(cancellationToken));

        [HttpGet("urunler")]
        public async Task<ActionResult<IReadOnlyList<FinansUrunDto>>> Urunler(CancellationToken cancellationToken) =>
            Ok((await _context.FinansUrunleri.AsNoTracking().Include(u => u.Eslesmeler).OrderBy(u => u.Sira).ThenBy(u => u.Ad)
                .ToListAsync(cancellationToken)).Select(UrunDto));

        [HttpPost("urunler")]
        public async Task<ActionResult<FinansUrunDto>> UrunOlustur([FromBody] FinansUrunKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = UrunDogrula(request); if (hata != null) return BadRequest(hata);
            if (await _context.FinansUrunleri.AnyAsync(u => u.Kod == request.Kod.Trim(), cancellationToken)) return Conflict("Ürün kodu zaten kullanılıyor.");
            var eslesmeHatasi = await EslesmeleriDogrula(request.Eslesmeler, null, cancellationToken); if (eslesmeHatasi != null) return Conflict(eslesmeHatasi);
            var entity = new FinansUrun(); UrunDoldur(entity, request); _context.FinansUrunleri.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(Urunler), new { id = entity.Id }, UrunDto(entity));
        }

        [HttpPut("urunler/{id:int}")]
        public async Task<ActionResult<FinansUrunDto>> UrunGuncelle(int id, [FromBody] FinansUrunKaydetRequest request, CancellationToken cancellationToken)
        {
            var hata = UrunDogrula(request); if (hata != null) return BadRequest(hata);
            if (await _context.FinansUrunleri.AnyAsync(u => u.Id != id && u.Kod == request.Kod.Trim(), cancellationToken)) return Conflict("Ürün kodu zaten kullanılıyor.");
            var eslesmeHatasi = await EslesmeleriDogrula(request.Eslesmeler, id, cancellationToken); if (eslesmeHatasi != null) return Conflict(eslesmeHatasi);
            var entity = await _context.FinansUrunleri.Include(u => u.Eslesmeler).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (entity == null) return NotFound();
            _context.FinansUrunEslesmeleri.RemoveRange(entity.Eslesmeler); UrunDoldur(entity, request);
            await _context.SaveChangesAsync(cancellationToken); return Ok(UrunDto(entity));
        }

        [HttpDelete("urunler/{id:int}")]
        public async Task<IActionResult> UrunSil(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.FinansUrunleri.Include(u => u.Eslesmeler)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (entity == null) return NotFound(new { message = "Tarife bulunamadı." });
            if (await _context.FinansSiparisKalemleri.AnyAsync(k => k.UrunId == id, cancellationToken))
                return Conflict(new { message = "Bu tarife sipariş geçmişinde kullanıldığı için silinemez. Düzenleyerek pasif duruma getirebilirsiniz." });

            _context.FinansUrunEslesmeleri.RemoveRange(entity.Eslesmeler);
            _context.FinansUrunleri.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("belgeler/{tur:int}/{kayitId:int}")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<ActionResult<FinansBelgeDto>> BelgeYukle(int tur, int kayitId, IFormFile dosya, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(typeof(FinansBelgeTuru), tur)) return BadRequest("Geçersiz belge türü.");
            try
            {
                await using var stream = dosya.OpenReadStream();
                return Ok(await _belgeService.YukleAsync((FinansBelgeTuru)tur, kayitId, dosya.FileName, dosya.ContentType, dosya.Length, stream, Kullanici(), cancellationToken));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("belgeler/{tur:int}/{kayitId:int}")]
        public async Task<ActionResult<IReadOnlyList<FinansBelgeDto>>> BelgeListele(int tur, int kayitId, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(typeof(FinansBelgeTuru), tur)) return BadRequest("Geçersiz belge türü.");
            return Ok(await _belgeService.ListeleAsync((FinansBelgeTuru)tur, kayitId, cancellationToken));
        }

        [HttpGet("belgeler/{belgeId:int}/indir")]
        public async Task<IActionResult> BelgeIndir(int belgeId, CancellationToken cancellationToken)
        {
            var belge = await _belgeService.AcAsync(belgeId, cancellationToken); return belge == null ? NotFound() : File(belge.Stream, belge.IcerikTuru, belge.DosyaAdi);
        }

        [HttpDelete("belgeler/{belgeId:int}")]
        public async Task<IActionResult> BelgeSil(int belgeId, CancellationToken cancellationToken) =>
            await _belgeService.SilAsync(belgeId, cancellationToken) ? NoContent() : NotFound();

        [HttpPost("senkron/proje/{projeId:int}")]
        public async Task<IActionResult> ProjeSenkron(int projeId, CancellationToken cancellationToken)
        {
            await _senkronService.ProjeyiSenkronizeEtAsync(projeId, cancellationToken);
            return NoContent();
        }

        [HttpPost("senkron")]
        public async Task<IActionResult> TumunuSenkronizeEt(CancellationToken cancellationToken)
        {
            await _senkronService.TumunuSenkronizeEtAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("senkron/bagimsiz-sandik/{sandikId:int}")]
        public async Task<IActionResult> BagimsizSandikSenkron(int sandikId, CancellationToken cancellationToken)
        {
            await _senkronService.BagimsizSandigiSenkronizeEtAsync(sandikId, cancellationToken);
            return NoContent();
        }

        [HttpGet("rapor/pdf")]
        [HttpGet("raporlar/isler/pdf")]
        public async Task<IActionResult> PdfRapor([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, [FromQuery] string? projeNo,
            [FromQuery] string? musteri, [FromQuery] int[]? isTurleri, CancellationToken cancellationToken) =>
            File(await _raporService.IsRaporuPdfAsync(baslangic, bitis, projeNo, musteri, isTurleri, Kullanici(), cancellationToken), "application/pdf", "FinansIsRaporu.pdf");

        [HttpGet("rapor/excel")]
        [HttpGet("raporlar/isler/excel")]
        public async Task<IActionResult> ExcelRapor([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, [FromQuery] string? projeNo,
            [FromQuery] string? musteri, [FromQuery] int[]? isTurleri, CancellationToken cancellationToken) =>
            File(await _raporService.IsRaporuExcelAsync(baslangic, bitis, projeNo, musteri, isTurleri, Kullanici(), cancellationToken),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FinansIsRaporu.xlsx");

        [HttpGet("raporlar/aylik/pdf")]
        public async Task<IActionResult> AylikPdfRapor([FromQuery] int yil, [FromQuery] int ay, [FromQuery] string[]? gruplar, CancellationToken cancellationToken) =>
            File(await _raporService.AylikRaporPdfAsync(yil, ay, gruplar, Kullanici(), cancellationToken), "application/pdf", $"FinansAylikRapor-{yil}-{ay:00}.pdf");

        [HttpGet("raporlar/aylik/excel")]
        public async Task<IActionResult> AylikExcelRapor([FromQuery] int yil, [FromQuery] int ay, [FromQuery] string[]? gruplar, CancellationToken cancellationToken) =>
            File(await _raporService.AylikRaporExcelAsync(yil, ay, gruplar, Kullanici(), cancellationToken),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FinansAylikRapor-{yil}-{ay:00}.xlsx");

        [HttpGet("raporlar/aylik/ayri")]
        public async Task<IActionResult> AylikAyriRapor([FromQuery] int yil, [FromQuery] int ay, [FromQuery] string[]? gruplar, CancellationToken cancellationToken) =>
            File(await _raporService.AylikRaporZipAsync(yil, ay, gruplar, Kullanici(), cancellationToken), "application/zip", $"FinansAylikRaporlar-{yil}-{ay:00}.zip");

        [HttpGet("raporlar/giderler/pdf")]
        public async Task<IActionResult> GiderPdfRapor(CancellationToken cancellationToken) =>
            File(await _raporService.GiderRaporuPdfAsync(Kullanici(), cancellationToken), "application/pdf", "FinansGiderRaporu.pdf");

        [HttpGet("raporlar/giderler/excel")]
        public async Task<IActionResult> GiderExcelRapor(CancellationToken cancellationToken) =>
            File(await _raporService.GiderRaporuExcelAsync(Kullanici(), cancellationToken),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FinansGiderRaporu.xlsx");

        [HttpGet("raporlar/siparis-durumu/pdf")]
        public async Task<IActionResult> SiparisDurumPdfRapor([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
            [FromQuery] string? projeNo, [FromQuery] string? isGrubu, [FromQuery] string? durum, CancellationToken cancellationToken) =>
            File(await _raporService.SiparisDurumRaporuPdfAsync(baslangic, bitis, projeNo, isGrubu, durum, Kullanici(), cancellationToken),
                "application/pdf", "FinansSiparisDurumRaporu.pdf");

        [HttpGet("raporlar/siparis-durumu/excel")]
        public async Task<IActionResult> SiparisDurumExcelRapor([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
            [FromQuery] string? projeNo, [FromQuery] string? isGrubu, [FromQuery] string? durum, CancellationToken cancellationToken) =>
            File(await _raporService.SiparisDurumRaporuExcelAsync(baslangic, bitis, projeNo, isGrubu, durum, Kullanici(), cancellationToken),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FinansSiparisDurumRaporu.xlsx");

        private IQueryable<FinansSiparis> SiparisSorgusu(bool noTracking = true)
        {
            var query = _context.FinansSiparisleri
                .Include(s => s.Kalemler).ThenInclude(k => k.IsKaydi)
                .Include(s => s.Kalemler).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .Include(s => s.Belgeler).AsSplitQuery();
            return noTracking ? query.AsNoTracking() : query;
        }

        private IQueryable<FinansFatura> FaturaSorgusu() => _context.FinansFaturalari.AsNoTracking()
            .Include(f => f.Siparis).ThenInclude(s => s.Kalemler).ThenInclude(k => k.IsKaydi)
            .Include(f => f.Kalemler).ThenInclude(k => k.SiparisKalemi).ThenInclude(k => k.IsKaydi)
            .Include(f => f.Belgeler).AsSplitQuery();

        private static FinansIsKaydiDto IsKaydiDto(FinansIsKaydi entity)
        {
            var siparisler = entity.SiparisKalemleri.Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).ToList();
            var faturalar = siparisler.SelectMany(k => k.FaturaKalemleri).Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).ToList();
            return new FinansIsKaydiDto(entity.Id, entity.ProjeId, entity.ProjeNo, entity.Musteri, entity.SandikNo, entity.SandikAdi,
                entity.SandikTipi, entity.Boy, entity.En, entity.Yukseklik, entity.IcSandikSablonId, entity.IsTuru,
                entity.Adet, entity.BirimM3, entity.ToplamM3, entity.UretimeAlinmaTarihi, siparisler.Sum(k => k.Adet), siparisler.Sum(k => k.M3),
                Math.Max(0, entity.Adet - siparisler.Sum(k => k.Adet)), Math.Max(0, entity.ToplamM3 - siparisler.Sum(k => k.M3)),
                faturalar.Sum(k => k.Adet), faturalar.Sum(k => k.M3), siparisler.Select(k => k.Siparis.PoNumarasi).Distinct().ToArray(),
                faturalar.Select(k => k.Fatura.FaturaNumarasi).Distinct().ToArray(), entity.KaynakAktif);
        }

        private static FinansSiparisDto SiparisDto(FinansSiparis entity)
        {
            var aktifFaturalar = entity.Kalemler.SelectMany(k => k.FaturaKalemleri).Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).ToList();
            var toplamM3 = entity.Kalemler.Sum(k => k.M3); var faturalananM3 = aktifFaturalar.Sum(k => k.M3);
            var faturaDurumu = faturalananM3 <= Tolerans ? FinansFaturaDurumu.Bekliyor : toplamM3 - faturalananM3 > Tolerans ? FinansFaturaDurumu.KismiFaturalandi : FinansFaturaDurumu.Faturalandi;
            return new FinansSiparisDto(entity.Id, entity.KayitNo, entity.PoNumarasi, entity.AnaProjeNo,
                entity.Kalemler.Select(k => k.IsKaydi.Musteri).FirstOrDefault() ?? string.Empty,
                entity.Kalemler.Select(k => k.IsKaydi.IsTuru.ToString()).Distinct().ToArray(), entity.SiparisTarihi,
                entity.Kalemler.Sum(k => k.Adet), toplamM3, faturalananM3, Math.Max(0, toplamM3 - faturalananM3), entity.Durum, faturaDurumu,
                entity.Belgeler.Count > 0, entity.Aciklama, ParaToplamlari(entity.Kalemler));
        }

        private static IReadOnlyList<FinansParaToplamiDto> ParaToplamlari(IEnumerable<FinansSiparisKalemi> kalemler) => kalemler
            .GroupBy(k => k.ParaBirimi).Select(g => new FinansParaToplamiDto(g.Key, g.Sum(k => k.NetTutar), g.Sum(k => k.KdvTutari), g.Sum(k => k.ToplamTutar))).ToList();

        private static void SiparisTutarlariniDengele(IEnumerable<FinansSiparisKalemi> kalemler)
        {
            foreach (var grup in kalemler.GroupBy(k => new { k.UrunId, k.FiyatlandirmaBirimi, k.BirimFiyat, k.ParaBirimi, k.KdvOrani }))
            {
                var sonKalem = grup.Last();
                var miktar = grup.Key.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.M3
                    ? Math.Round(grup.Sum(k => k.FiyatlandirmaMiktari), 2, MidpointRounding.AwayFromZero)
                    : grup.Sum(k => k.FiyatlandirmaMiktari);
                var hedefNet = ParaYuvarla(miktar * grup.Key.BirimFiyat);
                var hedefKdv = ParaYuvarla(hedefNet * grup.Key.KdvOrani / 100);
                sonKalem.NetTutar += hedefNet - grup.Sum(k => k.NetTutar);
                sonKalem.KdvTutari += hedefKdv - grup.Sum(k => k.KdvTutari);
                sonKalem.ToplamTutar = sonKalem.NetTutar + sonKalem.KdvTutari;
            }
        }

        private static FinansUrunDto UrunDto(FinansUrun entity) => new(entity.Id, entity.Kod, entity.Ad, entity.FiyatlandirmaBirimi,
            entity.BirimFiyat, entity.ParaBirimi, entity.KdvOrani, entity.Aktif, entity.Sira,
            entity.Eslesmeler.Select(e => new FinansUrunEslesmesiDto(e.Id, e.IsTuru, e.SandikAdi, e.SandikTipi,
                e.Boy, e.En, e.Yukseklik, e.IcSandikSablonId, e.Aktif)).ToList());

        private static string? UrunDogrula(FinansUrunKaydetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Kod) || string.IsNullOrWhiteSpace(request.Ad)) return "Ürün kodu ve adı zorunludur.";
            if (!Enum.IsDefined(request.FiyatlandirmaBirimi)) return "Fiyatlandırma birimi geçersizdir.";
            if (request.BirimFiyat < 0 || request.KdvOrani is < 0 or > 100) return "Fiyat sıfırdan küçük, KDV ise 0-100 aralığı dışında olamaz.";
            if (request.ParaBirimi?.Trim().Length != 3) return "Para birimi üç harfli olmalıdır.";
            if (request.Eslesmeler.Any(e => e.IsTuru == FinansIsTuru.IcSandik && !e.IcSandikSablonId.HasValue)) return "İç sandık tarifesinde kayıtlı iç sandık tipi zorunludur.";
            if (request.Eslesmeler.Any(e => e.IsTuru != FinansIsTuru.IcSandik && e.IcSandikSablonId.HasValue)) return "Kayıtlı iç sandık tipi yalnız İç Sandık tarifesinde kullanılabilir.";
            var ozelSandikEslesmeleri = request.Eslesmeler.Where(e => e.IsTuru is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik).ToList();
            if (ozelSandikEslesmeleri.Any(e => !GecerliSandikTipleri.Contains(e.SandikTipi))) return "Saha ve yedek sandık tarifesinde sandık tipi zorunludur.";
            var katlanirEslesmeler = ozelSandikEslesmeleri.Where(e => e.SandikTipi == "Katlanır Sandık").ToList();
            if (katlanirEslesmeler.Any(e => e.Boy is null or <= 0 || e.En is null or <= 0 || e.Yukseklik is null or <= 0)) return "Katlanır sandık tarifesinde boy, en ve yükseklik zorunludur.";
            if (katlanirEslesmeler.Count > 0 && request.FiyatlandirmaBirimi != FinansFiyatlandirmaBirimi.Adet) return "Katlanır sandık adet üzerinden fiyatlandırılmalıdır.";
            if (ozelSandikEslesmeleri.Any(e => e.SandikTipi != "Katlanır Sandık") && request.FiyatlandirmaBirimi != FinansFiyatlandirmaBirimi.M3) return "Ahşap, kafes ve kontrplak sandık m³ üzerinden fiyatlandırılmalıdır.";
            if (request.Eslesmeler.Any(e => e.IsTuru is not (FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik)
                && (!string.IsNullOrWhiteSpace(e.SandikTipi) || e.Boy.HasValue || e.En.HasValue || e.Yukseklik.HasValue))) return "Sandık tipi ve ölçü eşlemesi yalnız Saha veya Yedek Sandık tarifesinde kullanılabilir.";
            if (request.Eslesmeler.GroupBy(EslesmeAnahtari).Any(g => g.Count() > 1)) return "Aynı ürün içinde yinelenen eşleme bulunuyor.";
            return null;
        }

        private async Task<string?> EslesmeleriDogrula(IReadOnlyList<FinansUrunEslesmesiKaydetRequest> eslesmeler, int? urunId, CancellationToken cancellationToken)
        {
            var sablonIds = eslesmeler.Where(e => e.IcSandikSablonId.HasValue).Select(e => e.IcSandikSablonId!.Value).Distinct().ToArray();
            if (sablonIds.Length > 0 && await _context.AmbalajIcSandikSablonlari.AsNoTracking().CountAsync(s => sablonIds.Contains(s.Id), cancellationToken) != sablonIds.Length)
                return "Seçilen kayıtlı iç sandık tiplerinden biri bulunamadı.";
            var mevcut = await _context.FinansUrunEslesmeleri.AsNoTracking().Where(e => !urunId.HasValue || e.UrunId != urunId.Value).ToListAsync(cancellationToken);
            return eslesmeler.Any(yeni => mevcut.Any(e => EslesmeAnahtari(e) == EslesmeAnahtari(yeni)))
                ? "Bu iş veya kayıtlı iç sandık tipi başka bir tarifede kullanılıyor." : null;
        }

        private static void UrunDoldur(FinansUrun entity, FinansUrunKaydetRequest request)
        {
            entity.Kod = request.Kod.Trim(); entity.Ad = request.Ad.Trim(); entity.FiyatlandirmaBirimi = request.FiyatlandirmaBirimi;
            entity.BirimFiyat = request.BirimFiyat; entity.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant(); entity.KdvOrani = request.KdvOrani;
            entity.Aktif = request.Aktif; entity.Sira = request.Sira;
            entity.Eslesmeler = request.Eslesmeler.Select(e => new FinansUrunEslesmesi
            {
                IsTuru = e.IsTuru,
                SandikAdi = e.IsTuru == FinansIsTuru.IcSandik ? null : Temizle(e.SandikAdi),
                SandikTipi = e.IsTuru is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik ? Temizle(e.SandikTipi) : null,
                Boy = e.SandikTipi == "Katlanır Sandık" ? e.Boy : null,
                En = e.SandikTipi == "Katlanır Sandık" ? e.En : null,
                Yukseklik = e.SandikTipi == "Katlanır Sandık" ? e.Yukseklik : null,
                IcSandikSablonId = e.IsTuru == FinansIsTuru.IcSandik ? e.IcSandikSablonId : null,
                Aktif = true
            }).ToList();
        }

        private static readonly string[] GecerliSandikTipleri = ["Ahşap Kapalı", "Kafes Sandık", "Kontrplak Sandık", "Katlanır Sandık"];

        private static string EslesmeAnahtari(FinansUrunEslesmesiKaydetRequest eslesme) =>
            $"{(int)eslesme.IsTuru}|{eslesme.IcSandikSablonId}|{Temizle(eslesme.SandikAdi)?.ToUpperInvariant()}|{Temizle(eslesme.SandikTipi)?.ToUpperInvariant()}|{eslesme.Boy}|{eslesme.En}|{eslesme.Yukseklik}";

        private static string EslesmeAnahtari(FinansUrunEslesmesi eslesme) =>
            $"{(int)eslesme.IsTuru}|{eslesme.IcSandikSablonId}|{eslesme.SandikAdi?.ToUpperInvariant()}|{eslesme.SandikTipi?.ToUpperInvariant()}|{eslesme.Boy}|{eslesme.En}|{eslesme.Yukseklik}";

        private static decimal ParaYuvarla(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static FinansFaturaDto FaturaDto(FinansFatura entity) => new(entity.Id, entity.KayitNo, entity.FaturaNumarasi, entity.FaturaTarihi,
            entity.Siparis.PoNumarasi, entity.Siparis.AnaProjeNo, entity.Kalemler.Select(k => k.SiparisKalemi.IsKaydi.IsTuru.ToString()).Distinct().ToArray(),
            entity.Kalemler.Sum(k => k.Adet), entity.Kalemler.Sum(k => k.M3), entity.Durum, entity.Belgeler.Count > 0, entity.Aciklama);

        private static FinansOzelIsDto OzelIsDto(FinansOzelIs entity)
        {
            var siparisler = entity.FinansKaydi?.SiparisKalemleri.Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).ToList() ?? [];
            var faturalar = siparisler.SelectMany(k => k.FaturaKalemleri).Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).ToList();
            var faturaBekleyenSiparisId = siparisler.Where(k => k.Adet - k.FaturaKalemleri.Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.Adet) > Tolerans)
                .Select(k => (int?)k.SiparisId).FirstOrDefault();
            return new FinansOzelIsDto(entity.Id, entity.KayitNo, entity.IsTuru, entity.Musteri, entity.ProjeId, entity.Proje?.ProjeNo ?? string.Empty,
                entity.IsAdi, entity.Aciklama, entity.Miktar, entity.Birim, entity.BirimFiyat, entity.ParaBirimi, entity.KdvOrani,
                entity.FinansKaydi?.Id, entity.IsTarihi, entity.DuzenliIsId, entity.DonemAnahtari, entity.IptalEdildi,
                entity.Belgeler.Count, siparisler.Select(k => k.Siparis.PoNumarasi).Distinct().ToArray(), faturalar.Select(k => k.Fatura.FaturaNumarasi).Distinct().ToArray(),
                faturaBekleyenSiparisId, entity.CreatedDate, entity.CreatedBy, entity.HesaplamaYontemi, entity.RaporGrubu);
        }

        private static FinansDuzenliIsDto DuzenliIsDto(FinansDuzenliIs entity)
        {
            var sonraki = entity.SonOlusturulanDonem?.AddMonths(entity.TekrarSikligi == "Üç Aylık" ? 3 : entity.TekrarSikligi == "Yıllık" ? 12 : 1);
            return new FinansDuzenliIsDto(entity.Id, entity.ProjeId, entity.Proje?.ProjeNo ?? string.Empty, entity.IsAdi, entity.IsTuru, entity.Musteri,
                entity.Aciklama, entity.TekrarSikligi, entity.BaslangicTarihi, entity.BitisTarihi, entity.OlusturmaGunu, entity.Miktar, entity.Birim,
                entity.BirimFiyat, entity.ParaBirimi, entity.KdvOrani, entity.Aktif, entity.SonOlusturulanDonem, sonraki, entity.CreatedDate, entity.CreatedBy,
                entity.HesaplamaYontemi, entity.RaporGrubu);
        }

        private static FinansGiderDto GiderDto(FinansGider entity) => new(entity.Id, entity.Tarih, entity.KategoriId, entity.Kategori?.Ad ?? string.Empty,
            entity.AltKategori, entity.FirmaVeyaKisi, entity.Aciklama, entity.Tutar, entity.ParaBirimi, entity.KdvDahil, entity.KdvOrani, entity.Matrah, entity.KdvTutari,
            entity.ToplamTutar, entity.ProjeId, entity.Proje?.ProjeNo ?? string.Empty, entity.IsTuru, entity.IptalEdildi, entity.Belgeler.Count, entity.CreatedDate, entity.CreatedBy);

        private static FinansBelgeDto BelgeDto(FinansBelge entity) => new(entity.Id, entity.BelgeTuru,
            entity.SiparisId ?? entity.FaturaId ?? entity.OzelIsId ?? entity.GiderId ?? 0, entity.DosyaAdi, entity.DosyaUzantisi, entity.IcerikTuru,
            entity.Boyut, entity.YukleyenKullanici, entity.CreatedDate);

        private static FinansSayfaliSonuc<T> Sayfali<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
            new(items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
        private static (int Page, int PageSize) Sayfa(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        private static string KayitNo(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        private static string? Temizle(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private string Kullanici() => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Bilinmiyor";
        private void Gecmis(string tip, int id, string islem, string? eski, string? yeni, string? aciklama) => _context.FinansIslemGecmisleri.Add(new FinansIslemGecmisi
            { ReferansTipi = tip, ReferansId = id, Islem = islem, EskiDeger = eski, YeniDeger = yeni, Aciklama = Temizle(aciklama), IslemTarihi = DateTime.Now });

        private static string? OzelIsDogrula(string isTuru, string musteri, string isAdi, decimal miktar, string birim,
            FinansHesaplamaYontemi hesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet, string raporGrubu = "Özel İş",
            decimal birimFiyat = 0, string paraBirimi = "EUR", decimal kdvOrani = 0)
        {
            if (string.IsNullOrWhiteSpace(isTuru) || string.IsNullOrWhiteSpace(musteri) || string.IsNullOrWhiteSpace(isAdi) || string.IsNullOrWhiteSpace(birim) || miktar <= 0)
                return "İş türü, müşteri, iş adı, birim ve pozitif miktar zorunludur.";
            if (!Enum.IsDefined(hesaplamaYontemi) || string.IsNullOrWhiteSpace(raporGrubu)) return "Hesaplama yöntemi ve rapor grubu zorunludur.";
            if (birimFiyat < 0 || kdvOrani is < 0 or > 100 || paraBirimi?.Trim().Length != 3) return "Fiyat, para birimi veya KDV geçersizdir.";
            return null;
        }
        private static void OzelIsDoldur(FinansOzelIs entity, string isTuru, string musteri, int? projeId, string isAdi, string? aciklama,
            decimal miktar, string birim, DateTime isTarihi, FinansHesaplamaYontemi hesaplamaYontemi, string raporGrubu,
            decimal birimFiyat, string paraBirimi, decimal kdvOrani)
        { entity.IsTuru = isTuru.Trim(); entity.Musteri = musteri.Trim(); entity.ProjeId = projeId; entity.IsAdi = isAdi.Trim(); entity.Aciklama = Temizle(aciklama); entity.Miktar = miktar; entity.Birim = birim.Trim(); entity.IsTarihi = isTarihi.Date; entity.HesaplamaYontemi = hesaplamaYontemi; entity.RaporGrubu = raporGrubu.Trim(); entity.BirimFiyat = birimFiyat; entity.ParaBirimi = paraBirimi.Trim().ToUpperInvariant(); entity.KdvOrani = kdvOrani; }

        private static bool AktifSiparisiVar(FinansOzelIs entity) => entity.FinansKaydi?.SiparisKalemleri
            .Any(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi) == true;

        private static string? DuzenliIsDogrula(FinansDuzenliIsKaydetRequest request)
        {
            if (OzelIsDogrula(request.IsTuru, request.Musteri, request.IsAdi, request.Miktar, request.Birim,
                request.HesaplamaYontemi, request.RaporGrubu, request.BirimFiyat, request.ParaBirimi, request.KdvOrani) != null) return "Düzenli iş alanları eksik veya geçersiz.";
            if (request.TekrarSikligi is not ("Aylık" or "Üç Aylık" or "Yıllık")) return "Tekrar sıklığı Aylık, Üç Aylık veya Yıllık olmalıdır.";
            if (request.OlusturmaGunu is < 1 or > 31) return "Oluşturma günü 1-31 arasında olmalıdır.";
            if (request.BirimFiyat <= 0 || request.KdvOrani is < 0 or > 100 || request.ParaBirimi?.Trim().Length != 3) return "Pozitif birim fiyat, üç harfli para birimi ve 0-100 arası KDV zorunludur.";
            if (request.BitisTarihi.HasValue && request.BitisTarihi.Value.Date < request.BaslangicTarihi.Date) return "Bitiş tarihi başlangıç tarihinden önce olamaz.";
            return null;
        }
        private static void DuzenliIsDoldur(FinansDuzenliIs entity, FinansDuzenliIsKaydetRequest request)
        { entity.ProjeId = request.ProjeId; entity.IsAdi = request.IsAdi.Trim(); entity.IsTuru = request.IsTuru.Trim(); entity.Musteri = request.Musteri.Trim(); entity.Aciklama = Temizle(request.Aciklama); entity.TekrarSikligi = request.TekrarSikligi; entity.BaslangicTarihi = request.BaslangicTarihi.Date; entity.BitisTarihi = request.BitisTarihi?.Date; entity.OlusturmaGunu = request.OlusturmaGunu; entity.Miktar = request.Miktar; entity.Birim = request.Birim.Trim(); entity.BirimFiyat = request.BirimFiyat; entity.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant(); entity.KdvOrani = request.KdvOrani; entity.HesaplamaYontemi = request.HesaplamaYontemi; entity.RaporGrubu = request.RaporGrubu.Trim(); entity.Aktif = request.Aktif; }

        private async Task<string?> GiderDogrula(FinansGiderKaydetRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Aciklama) || request.Tutar <= 0) return "Açıklama ve pozitif tutar zorunludur.";
            if (request.KdvOrani is < 0 or > 100) return "KDV oranı 0-100 arasında olmalıdır.";
            if (request.ParaBirimi?.Trim().ToUpperInvariant() is not ("TRY" or "EUR" or "USD")) return "Para birimi TRY, EUR veya USD olmalıdır.";
            return await _context.FinansGiderKategorileri.AnyAsync(k => k.Id == request.KategoriId && k.Aktif, cancellationToken) ? null : "Aktif gider kategorisi bulunamadı.";
        }
        private static void GiderDoldur(FinansGider entity, FinansGiderKaydetRequest request)
        {
            var (matrah, kdv, toplam) = KdvHesapla(request.Tutar, request.KdvDahil, request.KdvOrani);
            entity.Tarih = request.Tarih.Date; entity.KategoriId = request.KategoriId; entity.AltKategori = Temizle(request.AltKategori); entity.FirmaVeyaKisi = Temizle(request.FirmaVeyaKisi);
            entity.Aciklama = request.Aciklama.Trim(); entity.Tutar = Para(request.Tutar); entity.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant(); entity.KdvDahil = request.KdvDahil; entity.KdvOrani = request.KdvOrani;
            entity.Matrah = matrah; entity.KdvTutari = kdv; entity.ToplamTutar = toplam; entity.ProjeId = request.ProjeId; entity.IsTuru = request.IsTuru;
        }
        private static (decimal Matrah, decimal Kdv, decimal Toplam) KdvHesapla(decimal tutar, bool kdvDahil, decimal kdvOrani)
        {
            var oran = kdvOrani / 100m;
            if (kdvDahil) { var toplam = Para(tutar); var matrah = Para(toplam / (1m + oran)); return (matrah, Para(toplam - matrah), toplam); }
            var haricMatrah = Para(tutar); var kdv = Para(haricMatrah * oran); return (haricMatrah, kdv, Para(haricMatrah + kdv));
        }
        private static decimal Para(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}