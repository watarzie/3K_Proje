using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K_API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AmbalajController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AmbalajController(AppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet("projeler")]
        public async Task<ActionResult<IReadOnlyList<AmbalajProjeOzetDto>>> GetProjeler(CancellationToken cancellationToken)
        {
            var projeler = await _context.Projeler
                .AsNoTracking()
                .Include(p => p.ProjeTipiLookup)
                .Include(p => p.Sandiklar)
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(plan => plan.Kalemler)
                .OrderByDescending(p => p.Id)
                .ToListAsync(cancellationToken);

            var sonuc = projeler.Select(proje =>
            {
                var olculuSandiklar = proje.Sandiklar
                    .Where(s => s.Boy > 0 && s.En > 0 && s.Yukseklik > 0)
                    .ToList();
                var eksikSandiklar = proje.Sandiklar
                    .Where(s => !s.Boy.HasValue || s.Boy <= 0 || !s.En.HasValue || s.En <= 0 || !s.Yukseklik.HasValue || s.Yukseklik <= 0)
                    .Select(s => s.SandikNo)
                    .ToList();
                var toplamHacim = olculuSandiklar.Sum(s =>
                    AmbalajHesaplayici.Hesapla(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value).ToplamHacimM3 * SandikAdediHesapla(s.SandikNo));
                var plan = proje.AmbalajUretimPlani;
                var kaynakKayitlari = plan?.Kalemler
                    .Where(k => k.KaynakSandikId.HasValue)
                    .ToDictionary(k => k.KaynakSandikId!.Value) ?? new Dictionary<int, AmbalajUretimKalemi>();
                var seciliKaynaklar = olculuSandiklar
                    .Where(s => !kaynakKayitlari.TryGetValue(s.Id, out var kayit) || kayit.UretimeAlindi)
                    .ToList();
                var ilaveKaynaklar = proje.Sandiklar
                    .Where(s => KaynakTuru(s, plan, kaynakKayitlari.GetValueOrDefault(s.Id)) == 2)
                    .ToList();
                var projeKaynaklar = proje.Sandiklar.Except(ilaveKaynaklar).ToList();
                var manuelKalemler = plan?.Kalemler.Where(k => !k.KaynakSandikId.HasValue && k.UretimeAlindi).ToList()
                    ?? new List<AmbalajUretimKalemi>();
                var uretimHacmi = seciliKaynaklar.Sum(s =>
                        AmbalajHesaplayici.Hesapla(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value).ToplamHacimM3 * SandikAdediHesapla(s.SandikNo))
                    + manuelKalemler.Where(Olculu).Sum(KalemHacmi);

                return new AmbalajProjeOzetDto(
                    proje.Id,
                    proje.ProjeNo,
                    proje.FBNo,
                    proje.Musteri,
                    proje.ProjeTipiId,
                    proje.ProjeTipiLookup?.Deger ?? "-",
                    proje.Sandiklar.Sum(s => SandikAdediHesapla(s.SandikNo)),
                    olculuSandiklar.Count,
                    eksikSandiklar.Count,
                    eksikSandiklar,
                        toplamHacim,
                        plan?.FirinPartiNo,
                        seciliKaynaklar.Sum(s => SandikAdediHesapla(s.SandikNo)) + manuelKalemler.Sum(k => k.Adet),
                        ilaveKaynaklar.Count + manuelKalemler.Count(k => k.Tur == 2),
                        manuelKalemler.Count(k => k.Tur == 3),
                        uretimHacmi,
                        plan?.ProjeSandiklariDurumId ?? 1,
                        plan?.IlaveSandiklarDurumId ?? 1,
                        plan?.IcSandiklarDurumId ?? 1,
                        plan?.IlaveFirinPartiNo,
                        plan?.IcSandikFirinPartiNo,
                        projeKaynaklar.Sum(s => SandikAdediHesapla(s.SandikNo)) + manuelKalemler.Where(k => k.Tur == 1).Sum(k => k.Adet),
                        KaynakHacmi(projeKaynaklar) + manuelKalemler.Where(k => k.Tur == 1 && Olculu(k)).Sum(KalemHacmi),
                        KaynakHacmi(ilaveKaynaklar) + manuelKalemler.Where(k => k.Tur == 2 && Olculu(k)).Sum(KalemHacmi),
                        manuelKalemler.Where(k => k.Tur == 3 && Olculu(k)).Sum(KalemHacmi));
            }).ToList();

            return Ok(sonuc);
        }

        [HttpGet("projeler/{projeId:int}/plan")]
        public async Task<ActionResult<AmbalajUretimPlanDto>> GetPlan(
            int projeId,
            [FromQuery] int? kaynakProjeTipiId,
            [FromQuery] int? grup,
            CancellationToken cancellationToken)
        {
            var proje = await _context.Projeler
                .AsNoTracking()
                .Include(p => p.ProjeTipiLookup)
                .Include(p => p.Sandiklar)
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(plan => plan.Kalemler)
                .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);

            if (proje == null)
                return NotFound(new { message = "Proje bulunamadı." });
            if (kaynakProjeTipiId.HasValue && proje.ProjeTipiId != kaynakProjeTipiId)
                return BadRequest(new { message = "Proje seçilen yönetim kaynağına ait değil." });
            if (grup.HasValue && grup is not (1 or 2 or 3))
                return BadRequest(new { message = "Üretim grubu geçersiz." });

            return Ok(PlanDtoOlustur(proje, grup));
        }

        [HttpPut("projeler/{projeId:int}/plan")]
        public async Task<ActionResult<AmbalajUretimPlanDto>> PlanKaydet(
            int projeId,
            [FromBody] AmbalajPlanKaydetRequest request,
            [FromQuery] int? kaynakProjeTipiId,
            CancellationToken cancellationToken)
        {
            var proje = await _context.Projeler
                .Include(p => p.ProjeTipiLookup)
                .Include(p => p.Sandiklar)
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(plan => plan.Kalemler)
                .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);

            if (proje == null)
                return NotFound(new { message = "Proje bulunamadı." });
            if (kaynakProjeTipiId.HasValue && proje.ProjeTipiId != kaynakProjeTipiId)
                return BadRequest(new { message = "Proje seçilen yönetim kaynağına ait değil." });

            if (request.Grup is not (1 or 2 or 3) || request.DurumId is not (1 or 2 or 3))
                return BadRequest(new { message = "Üretim grubu veya durumu geçersiz." });

            var planMevcut = proje.AmbalajUretimPlani != null;
            var mevcutKayitlar = proje.AmbalajUretimPlani?.Kalemler
                .Where(k => k.KaynakSandikId.HasValue)
                .ToDictionary(k => k.KaynakSandikId!.Value) ?? new Dictionary<int, AmbalajUretimKalemi>();
            var hedefTur = request.Grup == 3 ? 3 : request.Grup;
            var gecerliSandikIds = proje.Sandiklar
                .Where(s => KaynakTuru(s, proje.AmbalajUretimPlani, mevcutKayitlar.GetValueOrDefault(s.Id)) == hedefTur)
                .Select(s => s.Id)
                .ToHashSet();
            if (request.SeciliKaynakSandikIds.Any(id => !gecerliSandikIds.Contains(id)))
                return BadRequest(new { message = "Seçilen sandıklardan biri bu üretim grubuna ait değil." });

            var plan = proje.AmbalajUretimPlani ?? new AmbalajUretimPlani
            {
                ProjeId = projeId,
                CreatedBy = KullaniciMetni()
            };
            if (proje.AmbalajUretimPlani == null)
                _context.AmbalajUretimPlanlari.Add(plan);

            PlanGrupBilgileriniGuncelle(plan, request);
            plan.UpdatedDate = DateTime.Now;
            plan.UpdatedBy = KullaniciMetni();

            foreach (var sandik in proje.Sandiklar.Where(s => gecerliSandikIds.Contains(s.Id)))
            {
                var kalem = plan.Kalemler.FirstOrDefault(k => k.KaynakSandikId == sandik.Id);
                if (kalem == null)
                {
                    kalem = new AmbalajUretimKalemi
                    {
                        KaynakSandikId = sandik.Id,
                        Tur = hedefTur,
                        CreatedBy = KullaniciMetni()
                    };
                    plan.Kalemler.Add(kalem);
                }

                kalem.UretimeAlindi = request.SeciliKaynakSandikIds.Contains(sandik.Id);
                kalem.SandikNo = sandik.SandikNo;
                kalem.Ad = sandik.Ad;
                kalem.Adet = SandikAdediHesapla(sandik.SandikNo);
                kalem.Boy = sandik.Boy ?? 0;
                kalem.En = sandik.En ?? 0;
                kalem.Yukseklik = sandik.Yukseklik ?? 0;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(PlanDtoOlustur(proje, request.Grup));
        }

        [HttpPost("projeler/{projeId:int}/kalemler")]
        public async Task<ActionResult<AmbalajUretimKalemDto>> KalemEkle(
            int projeId,
            [FromBody] AmbalajKalemKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = KalemDogrula(request);
            if (validation != null)
                return BadRequest(new { message = validation });

            var proje = await _context.Projeler
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(plan => plan.Kalemler)
                .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);
            if (proje == null)
                return NotFound(new { message = "Proje bulunamadı." });
            if (request.Tur == 1 && proje.ProjeTipiId == 1)
                return BadRequest(new { message = "Normal projelere manuel sandık eklenemez." });

            var plan = proje.AmbalajUretimPlani ?? new AmbalajUretimPlani { ProjeId = projeId, CreatedBy = KullaniciMetni() };
            if (proje.AmbalajUretimPlani == null)
                _context.AmbalajUretimPlanlari.Add(plan);

            var kalem = new AmbalajUretimKalemi { CreatedBy = KullaniciMetni() };
            var sandikNo = string.IsNullOrWhiteSpace(request.SandikNo)
                ? SonrakiManuelSandikNo(plan, request.Tur)
                : request.SandikNo.Trim();
            KalemGuncelle(kalem, request, null, sandikNo);
            plan.Kalemler.Add(kalem);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(KalemDtoOlustur(kalem));
        }

        [HttpPut("kalemler/{kalemId:int}")]
        public async Task<ActionResult<AmbalajUretimKalemDto>> KalemGuncelle(
            int kalemId,
            [FromBody] AmbalajKalemKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = KalemDogrula(request);
            if (validation != null)
                return BadRequest(new { message = validation });

            var kalem = await _context.AmbalajUretimKalemleri
                .Include(k => k.AmbalajUretimPlani)
                .FirstOrDefaultAsync(k => k.Id == kalemId && !k.KaynakSandikId.HasValue, cancellationToken);
            if (kalem == null)
                return NotFound(new { message = "Ambalaj kalemi bulunamadı." });

            var sandikNo = string.IsNullOrWhiteSpace(request.SandikNo)
                ? kalem.SandikNo
                : request.SandikNo.Trim();
            KalemGuncelle(kalem, request, null, sandikNo);
            kalem.UpdatedDate = DateTime.Now;
            kalem.UpdatedBy = KullaniciMetni();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(KalemDtoOlustur(kalem));
        }

        [HttpDelete("kalemler/{kalemId:int}")]
        public async Task<IActionResult> KalemSil(int kalemId, CancellationToken cancellationToken)
        {
            var kalem = await _context.AmbalajUretimKalemleri
                .Include(k => k.IcSandiklar)
                .FirstOrDefaultAsync(k => k.Id == kalemId && !k.KaynakSandikId.HasValue, cancellationToken);
            if (kalem == null)
                return NotFound(new { message = "Ambalaj kalemi bulunamadı." });
            if (kalem.IcSandiklar.Count > 0)
                return BadRequest(new { message = "Bu sandığa bağlı iç sandıklar silinmeden ana sandık silinemez." });

            _context.AmbalajUretimKalemleri.Remove(kalem);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("ic-sandik-sablonlari")]
        public async Task<ActionResult<IReadOnlyList<AmbalajIcSandikSablonDto>>> GetIcSandikSablonlari(CancellationToken cancellationToken)
        {
            var sablonlar = await _context.AmbalajIcSandikSablonlari.AsNoTracking().OrderBy(s => s.Ad)
                .Select(s => new AmbalajIcSandikSablonDto(s.Id, s.Ad, s.SandikTipi, s.Boy, s.En, s.Yukseklik))
                .ToListAsync(cancellationToken);
            return Ok(sablonlar);
        }

        [HttpPost("ic-sandik-sablonlari")]
        public async Task<ActionResult<AmbalajIcSandikSablonDto>> IcSandikSablonuEkle(
            [FromBody] AmbalajIcSandikSablonKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = SablonDogrula(request);
            if (validation != null)
                return BadRequest(new { message = validation });
            if (await _context.AmbalajIcSandikSablonlari.AnyAsync(s => s.Ad.ToLower() == request.Ad.Trim().ToLower(), cancellationToken))
                return BadRequest(new { message = "Bu isimde bir iç sandık şablonu zaten var." });

            var sablon = new AmbalajIcSandikSablonu
            {
                Ad = request.Ad.Trim(),
                SandikTipi = request.SandikTipi.Trim(),
                Boy = request.Boy,
                En = request.En,
                Yukseklik = request.Yukseklik,
                CreatedBy = KullaniciMetni()
            };
            _context.AmbalajIcSandikSablonlari.Add(sablon);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new AmbalajIcSandikSablonDto(sablon.Id, sablon.Ad, sablon.SandikTipi, sablon.Boy, sablon.En, sablon.Yukseklik));
        }

        [HttpDelete("ic-sandik-sablonlari/{sablonId:int}")]
        public async Task<IActionResult> IcSandikSablonuSil(int sablonId, CancellationToken cancellationToken)
        {
            var sablon = await _context.AmbalajIcSandikSablonlari.FindAsync([sablonId], cancellationToken);
            if (sablon == null)
                return NotFound(new { message = "İç sandık şablonu bulunamadı." });
            _context.AmbalajIcSandikSablonlari.Remove(sablon);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("bagimsiz-sandiklar")]
        public async Task<ActionResult<IReadOnlyList<AmbalajBagimsizSandikDto>>> GetBagimsizSandiklar(
            [FromQuery] int tur,
            CancellationToken cancellationToken)
        {
            if (tur is not (2 or 3))
                return BadRequest(new { message = "Bağımsız sandık grubu geçersiz." });

            var sandiklar = await _context.AmbalajBagimsizSandiklar
                .AsNoTracking()
                .Where(s => s.Tur == tur)
                .OrderByDescending(s => s.Id)
                .ToListAsync(cancellationToken);
            return Ok(sandiklar.Select(BagimsizSandikDtoOlustur).ToList());
        }

        [HttpPost("bagimsiz-sandiklar")]
        public async Task<ActionResult<AmbalajBagimsizSandikDto>> BagimsizSandikEkle(
            [FromBody] AmbalajKalemKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = BagimsizSandikDogrula(request);
            if (validation != null)
                return BadRequest(new { message = validation });

            var sandik = new AmbalajBagimsizSandik
            {
                CreatedBy = KullaniciMetni(),
                SandikNo = string.IsNullOrWhiteSpace(request.SandikNo)
                    ? await SonrakiBagimsizSandikNo(request.Tur, cancellationToken)
                    : request.SandikNo.Trim()
            };
            BagimsizSandikGuncelle(sandik, request);
            _context.AmbalajBagimsizSandiklar.Add(sandik);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(BagimsizSandikDtoOlustur(sandik));
        }

        [HttpPut("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult<AmbalajBagimsizSandikDto>> BagimsizSandikGuncelle(
            int sandikId,
            [FromBody] AmbalajKalemKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = BagimsizSandikDogrula(request);
            if (validation != null)
                return BadRequest(new { message = validation });

            var sandik = await _context.AmbalajBagimsizSandiklar.FindAsync([sandikId], cancellationToken);
            if (sandik == null)
                return NotFound(new { message = "Sandık bulunamadı." });

            BagimsizSandikGuncelle(sandik, request);
            sandik.SandikNo = string.IsNullOrWhiteSpace(request.SandikNo) ? sandik.SandikNo : request.SandikNo.Trim();
            sandik.UpdatedBy = KullaniciMetni();
            sandik.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(BagimsizSandikDtoOlustur(sandik));
        }

        [HttpDelete("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<IActionResult> BagimsizSandikSil(int sandikId, CancellationToken cancellationToken)
        {
            var sandik = await _context.AmbalajBagimsizSandiklar.FindAsync([sandikId], cancellationToken);
            if (sandik == null)
                return NotFound(new { message = "Sandık bulunamadı." });
            _context.AmbalajBagimsizSandiklar.Remove(sandik);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private AmbalajUretimPlanDto PlanDtoOlustur(Proje proje, int? grup = null)
        {
            var plan = proje.AmbalajUretimPlani;
            var kayitMap = plan?.Kalemler.Where(k => k.KaynakSandikId.HasValue)
                .ToDictionary(k => k.KaynakSandikId!.Value) ?? new Dictionary<int, AmbalajUretimKalemi>();
            var kaynaklar = grup == 3
                ? new List<AmbalajUretimKalemDto>()
                : proje.Sandiklar
                    .OrderBy(s => s.SandikNo)
                    .Select(s =>
                    {
                        kayitMap.TryGetValue(s.Id, out var kayit);
                        var adet = SandikAdediHesapla(s.SandikNo);
                        var tur = KaynakTuru(s, plan, kayit);
                        var hacim = s.Boy > 0 && s.En > 0 && s.Yukseklik > 0
                            ? AmbalajHesaplayici.Hesapla(s.Boy.Value, s.En.Value, s.Yukseklik.Value).ToplamHacimM3 * adet
                            : 0;
                        return new AmbalajUretimKalemDto(kayit?.Id ?? 0, s.Id, null, tur, tur == 2 ? "İlave Sandık" : "Proje Sandığı", kayit?.UretimeAlindi ?? true,
                            s.SandikNo, s.Ad, kayit?.SandikTipi ?? "Ahşap Kapalı", adet, s.Boy ?? 0, s.En ?? 0, s.Yukseklik ?? 0, null, null, null, hacim);
                    }).ToList();
            var manueller = plan?.Kalemler.Where(k => !k.KaynakSandikId.HasValue).OrderBy(k => k.Tur).ThenBy(k => k.SandikNo)
                .Select(KalemDtoOlustur).ToList() ?? new List<AmbalajUretimKalemDto>();
            var tumKalemler = kaynaklar.Concat(manueller).ToList();

            return new AmbalajUretimPlanDto(proje.Id, proje.ProjeNo, proje.FBNo, proje.Musteri,
                proje.ProjeTipiId, proje.ProjeTipiLookup?.Deger ?? "-", plan?.FirinPartiNo,
                plan?.IlaveFirinPartiNo, plan?.IcSandikFirinPartiNo,
                plan?.ProjeSandiklariDurumId ?? 1, plan?.IlaveSandiklarDurumId ?? 1, plan?.IcSandiklarDurumId ?? 1,
                tumKalemler,
                tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.Adet),
                tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.HacimM3));
        }

        private static void PlanGrupBilgileriniGuncelle(AmbalajUretimPlani plan, AmbalajPlanKaydetRequest request)
        {
            if (request.Grup == 1)
            {
                plan.FirinPartiNo = Temizle(request.FirinPartiNo);
                plan.ProjeSandiklariDurumId = request.DurumId;
            }
            else if (request.Grup == 2)
            {
                plan.IlaveFirinPartiNo = Temizle(request.FirinPartiNo);
                plan.IlaveSandiklarDurumId = request.DurumId;
            }
            else
            {
                plan.IcSandikFirinPartiNo = Temizle(request.FirinPartiNo);
                plan.IcSandiklarDurumId = request.DurumId;
            }
        }

        private static AmbalajUretimKalemDto KalemDtoOlustur(AmbalajUretimKalemi k) =>
            new(k.Id, k.KaynakSandikId, k.UstKalemId, k.Tur, k.Tur == 1 ? "Manuel Proje Sandığı" : k.Tur == 2 ? "İlave Sandık" : "İç Sandık", k.UretimeAlindi,
                k.SandikNo, k.Ad, k.SandikTipi, k.Adet, k.Boy, k.En, k.Yukseklik, k.KullanimAmaci, k.TalimatVeren, k.Aciklama,
                Olculu(k) ? KalemHacmi(k) : 0);

        private static bool Olculu(AmbalajUretimKalemi k) => k.Boy > 0 && k.En > 0 && k.Yukseklik > 0;
        private static decimal KalemHacmi(AmbalajUretimKalemi k) =>
            AmbalajHesaplayici.Hesapla(k.Boy, k.En, k.Yukseklik).ToplamHacimM3 * k.Adet;
        private static decimal KaynakHacmi(IEnumerable<Sandik> sandiklar) => sandiklar
            .Where(s => s.Boy > 0 && s.En > 0 && s.Yukseklik > 0)
            .Sum(s => AmbalajHesaplayici.Hesapla(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value).ToplamHacimM3 * SandikAdediHesapla(s.SandikNo));
        private static int KaynakTuru(Sandik sandik, AmbalajUretimPlani? plan, AmbalajUretimKalemi? kayit)
        {
            if (plan == null)
                return 1;
            if (kayit?.Tur == 2 || sandik.CreatedDate > plan.CreatedDate)
                return 2;
            return kayit?.Tur ?? 1;
        }

        private static string? KalemDogrula(AmbalajKalemKaydetRequest request)
        {
            if (request.Tur is not (1 or 2 or 3)) return "Sandık grubu geçersizdir.";
            if (string.IsNullOrWhiteSpace(request.Ad)) return "Sandık adı zorunludur.";
            if (!GecerliSandikTipleri.Contains(request.SandikTipi)) return "Sandık tipi geçersizdir.";
            if (request.Adet <= 0) return "Adet sıfırdan büyük olmalıdır.";
            if (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0) return "Boy, en ve yükseklik zorunludur.";
            if (string.IsNullOrWhiteSpace(request.TalimatVeren)) return "Talimat veren kişi zorunludur.";
            return null;
        }

        private static string? SablonDogrula(AmbalajIcSandikSablonKaydetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Ad)) return "Şablon adı zorunludur.";
            if (!GecerliSandikTipleri.Contains(request.SandikTipi)) return "Sandık tipi geçersizdir.";
            if (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0) return "Boy, en ve yükseklik zorunludur.";
            return null;
        }

        private static string? BagimsizSandikDogrula(AmbalajKalemKaydetRequest request)
        {
            if (request.Tur is not (2 or 3)) return "Bağımsız sandık grubu geçersizdir.";
            return KalemDogrula(request);
        }

        private static void BagimsizSandikGuncelle(AmbalajBagimsizSandik sandik, AmbalajKalemKaydetRequest request)
        {
            sandik.Tur = request.Tur;
            sandik.UretimeAlindi = request.UretimeAlindi;
            sandik.Ad = request.Ad!.Trim();
            sandik.SandikTipi = request.SandikTipi;
            sandik.Adet = request.Adet;
            sandik.Boy = request.Boy;
            sandik.En = request.En;
            sandik.Yukseklik = request.Yukseklik;
            sandik.KullanimAmaci = Temizle(request.KullanimAmaci);
            sandik.TalimatVeren = Temizle(request.TalimatVeren);
            sandik.Aciklama = Temizle(request.Aciklama);
        }

        private static AmbalajBagimsizSandikDto BagimsizSandikDtoOlustur(AmbalajBagimsizSandik sandik) =>
            new(sandik.Id, sandik.Tur, sandik.UretimeAlindi, sandik.SandikNo, sandik.Ad, sandik.SandikTipi,
                sandik.Adet, sandik.Boy, sandik.En, sandik.Yukseklik, sandik.KullanimAmaci, sandik.TalimatVeren,
                sandik.Aciklama, AmbalajHesaplayici.Hesapla(sandik.Boy, sandik.En, sandik.Yukseklik).ToplamHacimM3 * sandik.Adet);

        private async Task<string> SonrakiBagimsizSandikNo(int tur, CancellationToken cancellationToken)
        {
            var onEk = tur == 2 ? "ILV-" : "IC-";
            var numaralar = await _context.AmbalajBagimsizSandiklar
                .AsNoTracking()
                .Where(s => s.Tur == tur && s.SandikNo.StartsWith(onEk))
                .Select(s => s.SandikNo)
                .ToListAsync(cancellationToken);
            var sonSira = numaralar.Select(no => int.TryParse(no[onEk.Length..], out var sira) ? sira : 0)
                .DefaultIfEmpty(0).Max();
            return $"{onEk}{sonSira + 1:000}";
        }

        private static void KalemGuncelle(AmbalajUretimKalemi kalem, AmbalajKalemKaydetRequest request, int? ustKalemId, string sandikNo)
        {
            kalem.Tur = request.Tur;
            kalem.UstKalemId = ustKalemId;
            kalem.UretimeAlindi = request.UretimeAlindi;
            kalem.SandikNo = sandikNo;
            kalem.Ad = request.Ad.Trim();
            kalem.SandikTipi = request.SandikTipi;
            kalem.Adet = request.Adet;
            kalem.Boy = request.Boy;
            kalem.En = request.En;
            kalem.Yukseklik = request.Yukseklik;
            kalem.KullanimAmaci = Temizle(request.KullanimAmaci);
            kalem.TalimatVeren = Temizle(request.TalimatVeren);
            kalem.Aciklama = Temizle(request.Aciklama);
        }

        private static string SonrakiManuelSandikNo(AmbalajUretimPlani plan, int tur)
        {
            var onEk = tur == 1 ? "MAN-" : tur == 2 ? "ILV-" : "IC-";
            var sonSira = plan.Kalemler.Where(k => !k.KaynakSandikId.HasValue && k.Tur == tur)
                .Select(k => k.SandikNo.StartsWith(onEk, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(k.SandikNo[onEk.Length..], out var sira) ? sira : 0)
                .DefaultIfEmpty(0).Max();
            return $"{onEk}{sonSira + 1:000}";
        }

        private static readonly HashSet<string> GecerliSandikTipleri = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ahşap Kapalı", "Kafes Sandık", "Kontrplak Sandık"
        };

        private string KullaniciMetni() => _currentUserService.UserId?.ToString() ?? "system";
        private static string? Temizle(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static int SandikAdediHesapla(string sandikNo)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sandikNo ?? string.Empty, @"^(\d+)\s*-\s*(\d+)$");
            if (!match.Success)
                return 1;

            var baslangic = int.Parse(match.Groups[1].Value);
            var bitis = int.Parse(match.Groups[2].Value);
            return bitis >= baslangic ? bitis - baslangic + 1 : 1;
        }
    }

    public sealed record AmbalajProjeOzetDto(
        int ProjeId,
        string ProjeNo,
        string? FbNo,
        string Musteri,
        int ProjeTipiId,
        string ProjeTipiMetni,
        int ToplamSandikAdedi,
        int OlculuSandikSayisi,
        int EksikOlculuSandikSayisi,
        IReadOnlyList<string> EksikOlculuSandiklar,
        decimal ToplamHacimM3,
        string? FirinPartiNo,
        int UretimeAlinanSandikAdedi,
        int IlaveSandikSayisi,
        int IcSandikSayisi,
        decimal UretimHacimM3,
        int ProjeSandiklariDurumId,
        int IlaveSandiklarDurumId,
        int IcSandiklarDurumId,
        string? IlaveFirinPartiNo,
        string? IcSandikFirinPartiNo,
        int ProjeSandikSayisi,
        decimal ProjeSandiklariHacimM3,
        decimal IlaveSandiklarHacimM3,
        decimal IcSandiklarHacimM3);

    public sealed record AmbalajUretimPlanDto(int ProjeId, string ProjeNo, string? FbNo, string Musteri,
        int ProjeTipiId, string ProjeTipiMetni, string? FirinPartiNo, string? IlaveFirinPartiNo, string? IcSandikFirinPartiNo,
        int ProjeSandiklariDurumId, int IlaveSandiklarDurumId, int IcSandiklarDurumId, IReadOnlyList<AmbalajUretimKalemDto> Kalemler,
        int SeciliSandikAdedi, decimal SeciliHacimM3);

    public sealed record AmbalajUretimKalemDto(int Id, int? KaynakSandikId, int? UstKalemId, int Tur, string TurMetni,
        bool UretimeAlindi, string SandikNo, string? Ad, string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik,
        string? KullanimAmaci, string? TalimatVeren, string? Aciklama, decimal HacimM3);

    public sealed record AmbalajPlanKaydetRequest(string? FirinPartiNo, IReadOnlyList<int> SeciliKaynakSandikIds,
        int Grup = 1, int DurumId = 1);

    public sealed record AmbalajKalemKaydetRequest(int Tur, int? UstKalemId, int? UstKaynakSandikId,
        bool UretimeAlindi, string SandikNo, string? Ad, string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik,
        string? KullanimAmaci, string? TalimatVeren, string? Aciklama);

    public sealed record AmbalajIcSandikSablonDto(int Id, string Ad, string SandikTipi, decimal Boy, decimal En, decimal Yukseklik);
    public sealed record AmbalajIcSandikSablonKaydetRequest(string Ad, string SandikTipi, decimal Boy, decimal En, decimal Yukseklik);
    public sealed record AmbalajBagimsizSandikDto(int Id, int Tur, bool UretimeAlindi, string SandikNo, string Ad,
        string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik, string? KullanimAmaci,
        string? TalimatVeren, string? Aciklama, decimal HacimM3);
}