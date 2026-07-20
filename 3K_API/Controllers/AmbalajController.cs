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
        private readonly IFinansSenkronService _finansSenkronService;

        public AmbalajController(
            AppDbContext context,
            ICurrentUserService currentUserService,
            IFinansSenkronService finansSenkronService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _finansSenkronService = finansSenkronService;
        }

        [HttpGet("projeler")]
        public async Task<ActionResult<IReadOnlyList<AmbalajProjeOzetDto>>> GetProjeler(CancellationToken cancellationToken)
        {
            var projeler = await _context.Projeler
                .AsNoTracking()
                .Where(p => p.ProjeTipiId == 1)
                .Include(p => p.ProjeTipiLookup)
                .Include(p => p.Sandiklar)
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(plan => plan.Kalemler)
                .OrderByDescending(p => p.Id)
                .ToListAsync(cancellationToken);

            var sonuc = projeler.Select(proje =>
            {
                var plan = proje.AmbalajUretimPlani;
                var kaynakKayitlari = plan?.Kalemler
                    .Where(k => k.KaynakSandikId.HasValue)
                    .ToDictionary(k => k.KaynakSandikId!.Value) ?? new Dictionary<int, AmbalajUretimKalemi>();
                var ambalajKaynaklari = proje.Sandiklar.Where(s => s.AmbalajaDahilMi != false).ToList();
                var seciliKaynaklar = ambalajKaynaklari
                    .Where(s => !kaynakKayitlari.TryGetValue(s.Id, out var kayit) || kayit.UretimeAlindi)
                    .ToList();
                var olculuSandiklar = ambalajKaynaklari
                    .Where(s => s.Boy > 0 && s.En > 0 && s.Yukseklik > 0)
                    .ToList();
                var eksikSandiklar = seciliKaynaklar
                    .Where(s => !s.Boy.HasValue || s.Boy <= 0 || !s.En.HasValue || s.En <= 0 || !s.Yukseklik.HasValue || s.Yukseklik <= 0)
                    .Select(s => s.SandikNo)
                    .ToList();
                var toplamHacim = olculuSandiklar.Sum(s =>
                    KaynakSandikHacmi(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value) * SandikAdediHesapla(s.SandikNo));
                var seciliOlculuKaynaklar = seciliKaynaklar
                    .Where(s => s.Boy > 0 && s.En > 0 && s.Yukseklik > 0)
                    .ToList();
                var ilaveKaynaklar = ambalajKaynaklari
                    .Where(s => KaynakTuru(s, plan, kaynakKayitlari.GetValueOrDefault(s.Id)) == 2)
                    .ToList();
                var projeKaynaklar = ambalajKaynaklari.Except(ilaveKaynaklar).ToList();
                var seciliIlaveKaynaklar = seciliOlculuKaynaklar.Intersect(ilaveKaynaklar).ToList();
                var seciliProjeKaynaklar = seciliOlculuKaynaklar.Intersect(projeKaynaklar).ToList();
                var manuelKalemler = plan?.Kalemler.Where(k => !k.KaynakSandikId.HasValue && k.UretimeAlindi).ToList()
                    ?? new List<AmbalajUretimKalemi>();
                var uretimHacmi = seciliOlculuKaynaklar.Sum(s =>
                    KaynakSandikHacmi(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value) * SandikAdediHesapla(s.SandikNo))
                    + manuelKalemler.Where(Olculu).Sum(KalemHacmi);

                return new AmbalajProjeOzetDto(
                    proje.Id,
                    proje.ProjeNo,
                    proje.FBNo,
                    proje.Musteri,
                    proje.ProjeTipiId,
                    proje.ProjeTipiLookup?.Deger ?? "-",
                    ambalajKaynaklari.Sum(s => SandikAdediHesapla(s.SandikNo)),
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
                        KaynakHacmi(seciliProjeKaynaklar) + manuelKalemler.Where(k => k.Tur == 1 && Olculu(k)).Sum(KalemHacmi),
                        KaynakHacmi(seciliIlaveKaynaklar) + manuelKalemler.Where(k => k.Tur == 2 && Olculu(k)).Sum(KalemHacmi),
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
            if (proje.ProjeTipiId != 1)
                return BadRequest(new { message = "Ambalaj üretim planı yalnız normal projeler için kullanılabilir." });
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
            if (proje.ProjeTipiId != 1)
                return BadRequest(new { message = "Ambalaj üretim planı yalnız normal projeler için kullanılabilir." });
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
                .Where(s => s.AmbalajaDahilMi != false && KaynakTuru(s, proje.AmbalajUretimPlani, mevcutKayitlar.GetValueOrDefault(s.Id)) == hedefTur)
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
            await _finansSenkronService.ProjeyiSenkronizeEtAsync(projeId, cancellationToken);
            return Ok(PlanDtoOlustur(proje, request.Grup));
        }

        [HttpPut("sandiklar/{sandikId:int}/ambalaj-karari")]
        public async Task<ActionResult<AmbalajUretimPlanDto>> AmbalajKarariKaydet(
            int sandikId,
            [FromBody] AmbalajKarariKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var sandik = await _context.Sandiklar
                .Include(s => s.Proje)
                    .ThenInclude(p => p.ProjeTipiLookup)
                .Include(s => s.Proje)
                    .ThenInclude(p => p.Sandiklar)
                .Include(s => s.Proje)
                    .ThenInclude(p => p.AmbalajUretimPlani)!
                        .ThenInclude(plan => plan.Kalemler)
                .FirstOrDefaultAsync(s => s.Id == sandikId, cancellationToken);

            if (sandik == null)
                return NotFound(new { message = "Sandık bulunamadı." });

            sandik.AmbalajaDahilMi = request.AmbalajaDahilMi;
            if (!request.AmbalajaDahilMi)
            {
                var kalem = sandik.Proje.AmbalajUretimPlani?.Kalemler
                    .FirstOrDefault(k => k.KaynakSandikId == sandik.Id);
                if (kalem != null)
                    kalem.UretimeAlindi = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _finansSenkronService.ProjeyiSenkronizeEtAsync(sandik.ProjeId, cancellationToken);
            return Ok(PlanDtoOlustur(sandik.Proje, null));
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
            if (request.IcSandikSablonId.HasValue && !await _context.AmbalajIcSandikSablonlari.AsNoTracking()
                .AnyAsync(s => s.Id == request.IcSandikSablonId.Value, cancellationToken))
                return BadRequest(new { message = "Seçilen kayıtlı iç sandık tipi bulunamadı." });

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
            await _finansSenkronService.ProjeyiSenkronizeEtAsync(projeId, cancellationToken);
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
            if (request.IcSandikSablonId.HasValue && !await _context.AmbalajIcSandikSablonlari.AsNoTracking()
                .AnyAsync(s => s.Id == request.IcSandikSablonId.Value, cancellationToken))
                return BadRequest(new { message = "Seçilen kayıtlı iç sandık tipi bulunamadı." });

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
            await _finansSenkronService.ProjeyiSenkronizeEtAsync(kalem.AmbalajUretimPlani.ProjeId, cancellationToken);
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

            var projeId = await _context.AmbalajUretimPlanlari
                .Where(p => p.Id == kalem.AmbalajUretimPlaniId)
                .Select(p => p.ProjeId)
                .SingleAsync(cancellationToken);
            _context.AmbalajUretimKalemleri.Remove(kalem);
            await _context.SaveChangesAsync(cancellationToken);
            await _finansSenkronService.ProjeyiSenkronizeEtAsync(projeId, cancellationToken);
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

        [HttpGet("talep-edenler")]
        public async Task<ActionResult<IReadOnlyList<AmbalajTalepEdenDto>>> GetTalepEdenler(CancellationToken cancellationToken)
        {
            var kisiler = await _context.AmbalajTalepEdenler.AsNoTracking()
                .OrderBy(t => t.Ad)
                .Select(t => new AmbalajTalepEdenDto(t.Id, t.Ad))
                .ToListAsync(cancellationToken);
            return Ok(kisiler);
        }

        [HttpPost("talep-edenler")]
        public async Task<ActionResult<AmbalajTalepEdenDto>> TalepEdenEkle(
            [FromBody] AmbalajTalepEdenKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var ad = request.Ad?.Trim();
            if (string.IsNullOrWhiteSpace(ad))
                return BadRequest(new { message = "Talep eden adı zorunludur." });
            if (ad.Length > 150)
                return BadRequest(new { message = "Talep eden adı en fazla 150 karakter olabilir." });
            if (await _context.AmbalajTalepEdenler.AnyAsync(t => t.Ad.ToLower() == ad.ToLower(), cancellationToken))
                return BadRequest(new { message = "Bu talep eden zaten kayıtlıdır." });

            var kisi = new AmbalajTalepEden { Ad = ad, CreatedBy = KullaniciMetni() };
            _context.AmbalajTalepEdenler.Add(kisi);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new AmbalajTalepEdenDto(kisi.Id, kisi.Ad));
        }

        [HttpGet("bagimsiz-sandiklar")]
        public async Task<ActionResult<IReadOnlyList<AmbalajBagimsizSandikDto>>> GetBagimsizSandiklar(
            [FromQuery] int? tur,
            CancellationToken cancellationToken)
        {
            if (tur.HasValue && tur is not (2 or 3 or 4 or 5))
                return BadRequest(new { message = "Özel sandık türü geçersiz." });

            var sorgu = _context.AmbalajBagimsizSandiklar
                .AsNoTracking()
                .Include(s => s.Proje)
                .Include(s => s.KaynakSandik)
                .Include(s => s.UstKaynakSandik)
                .AsQueryable();
            if (tur.HasValue)
                sorgu = sorgu.Where(s => s.Tur == tur.Value);

            var sandiklar = await sorgu
                .OrderByDescending(s => s.Id)
                .ToListAsync(cancellationToken);
            return Ok(sandiklar.Select(BagimsizSandikDtoOlustur).ToList());
        }

        [HttpGet("projeler/{projeId:int}/ilave-sandik-adaylari")]
        public async Task<ActionResult<IReadOnlyList<AmbalajIlaveSandikAdayDto>>> GetIlaveSandikAdaylari(
            int projeId,
            [FromQuery] int? mevcutKayitId,
            CancellationToken cancellationToken)
        {
            var kullanilanKaynakIdleri = _context.AmbalajBagimsizSandiklar.AsNoTracking()
                .Where(s => s.Tur == 2 && s.KaynakSandikId.HasValue && s.Id != mevcutKayitId)
                .Select(s => s.KaynakSandikId!.Value);

            var adaylar = await _context.Sandiklar.AsNoTracking()
                .Where(s => s.ProjeId == projeId && s.AmbalajaDahilMi == false)
                .Where(s => !kullanilanKaynakIdleri.Contains(s.Id))
                .OrderBy(s => s.SandikNo)
                .Select(s => new AmbalajIlaveSandikAdayDto(s.Id, s.SandikNo, s.Ad, s.Boy, s.En, s.Yukseklik))
                .ToListAsync(cancellationToken);

            return Ok(adaylar);
        }

        [HttpPost("bagimsiz-sandiklar")]
        public async Task<ActionResult<AmbalajBagimsizSandikDto>> BagimsizSandikEkle(
            [FromBody] AmbalajOzelSandikKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await BagimsizSandikDogrula(request, null, cancellationToken);
            if (validation != null)
                return BadRequest(new { message = validation });

            var sandik = new AmbalajBagimsizSandik
            {
                CreatedBy = KullaniciMetni(),
                SandikNo = request.Tur == 3
                    ? await SonrakiIcSandikNo(request.UstKaynakSandikId!.Value, cancellationToken)
                    : string.IsNullOrWhiteSpace(request.SandikNo)
                    ? await SonrakiBagimsizSandikNo(request.Tur, cancellationToken)
                    : request.SandikNo.Trim()
            };
            BagimsizSandikGuncelle(sandik, request);
            _context.AmbalajBagimsizSandiklar.Add(sandik);
            await _context.SaveChangesAsync(cancellationToken);
            await _finansSenkronService.BagimsizSandigiSenkronizeEtAsync(sandik.Id, cancellationToken);
            return Ok(BagimsizSandikDtoOlustur(sandik));
        }

        [HttpPut("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult<AmbalajBagimsizSandikDto>> BagimsizSandikGuncelle(
            int sandikId,
            [FromBody] AmbalajOzelSandikKaydetRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await BagimsizSandikDogrula(request, sandikId, cancellationToken);
            if (validation != null)
                return BadRequest(new { message = validation });

            var sandik = await _context.AmbalajBagimsizSandiklar
                .Include(s => s.Proje)
                .Include(s => s.KaynakSandik)
                .Include(s => s.UstKaynakSandik)
                .FirstOrDefaultAsync(s => s.Id == sandikId, cancellationToken);
            if (sandik == null)
                return NotFound(new { message = "Sandık bulunamadı." });

            BagimsizSandikGuncelle(sandik, request);
            sandik.SandikNo = string.IsNullOrWhiteSpace(request.SandikNo) ? sandik.SandikNo : request.SandikNo.Trim();
            sandik.UpdatedBy = KullaniciMetni();
            sandik.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
            await _finansSenkronService.BagimsizSandigiSenkronizeEtAsync(sandik.Id, cancellationToken);
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
            await _finansSenkronService.BagimsizSandigiSenkronizeEtAsync(sandikId, cancellationToken);
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
                    .OrderBy(s => SandikSiraAnahtari(s.SandikNo))
                    .ThenBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase)
                    .Select(s =>
                    {
                        kayitMap.TryGetValue(s.Id, out var kayit);
                        var adet = SandikAdediHesapla(s.SandikNo);
                        var tur = KaynakTuru(s, plan, kayit);
                        var sandikTipi = kayit?.SandikTipi ?? "Ahşap Kapalı";
                        var hacim = sandikTipi != "Kontrplak Sandık" && s.Boy > 0 && s.En > 0 && s.Yukseklik > 0
                            ? KaynakSandikHacmi(s.Boy.Value, s.En.Value, s.Yukseklik.Value) * adet
                            : 0;
                        return new AmbalajUretimKalemDto(kayit?.Id ?? 0, s.Id, null, null, tur, tur == 2 ? "İlave Sandık" : "Proje Sandığı",
                            s.AmbalajaDahilMi != false && (kayit?.UretimeAlindi ?? true),
                            s.SandikNo, s.Ad, sandikTipi, adet, s.Boy ?? 0, s.En ?? 0, s.Yukseklik ?? 0, null, null, null, hacim,
                            s.AmbalajaDahilMi, AmbalajKarariOneriliyor(s));
                    }).ToList();
            var manueller = plan?.Kalemler.Where(k => !k.KaynakSandikId.HasValue)
                .OrderBy(k => k.Tur)
                .ThenBy(k => SandikSiraAnahtari(k.SandikNo))
                .ThenBy(k => k.SandikNo, StringComparer.OrdinalIgnoreCase)
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
            new(k.Id, k.KaynakSandikId, k.UstKalemId, k.IcSandikSablonId, k.Tur, k.Tur == 1 ? "Manuel Proje Sandığı" : k.Tur == 2 ? "İlave Sandık" : "İç Sandık", k.UretimeAlindi,
                k.SandikNo, k.Ad, k.SandikTipi, k.Adet, k.Boy, k.En, k.Yukseklik, k.KullanimAmaci, k.TalimatVeren, k.Aciklama,
                Olculu(k) ? KalemHacmi(k) : 0);

        private static bool Olculu(AmbalajUretimKalemi k) => k.Boy > 0 && k.En > 0 && k.Yukseklik > 0;
        private static decimal KalemHacmi(AmbalajUretimKalemi k) => k.SandikTipi == "Kontrplak Sandık"
            ? 0
            : AmbalajHesaplayici.Hesapla(k.Boy, k.En, k.Yukseklik).ToplamHacimM3 * k.Adet;
        private static decimal KaynakHacmi(IEnumerable<Sandik> sandiklar) => sandiklar
            .Where(s => s.Boy > 0 && s.En > 0 && s.Yukseklik > 0)
            .Sum(s => KaynakSandikHacmi(s.Boy!.Value, s.En!.Value, s.Yukseklik!.Value) * SandikAdediHesapla(s.SandikNo));
        private static decimal KaynakSandikHacmi(decimal disBoy, decimal disEn, decimal disYukseklik) =>
            disBoy > 92m && disEn > 92m && disYukseklik > 255m
                ? AmbalajHesaplayici.Hesapla(disBoy - 92m, disEn - 92m, disYukseklik - 255m).ToplamHacimM3
                : 0;
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
            if (request.Tur != 3 && request.IcSandikSablonId.HasValue) return "Kayıtlı iç sandık tipi yalnız İç Sandık grubunda kullanılabilir.";
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

        private async Task<string?> BagimsizSandikDogrula(
            AmbalajOzelSandikKaydetRequest request,
            int? mevcutKayitId,
            CancellationToken cancellationToken)
        {
            if (request.Tur is not (2 or 3 or 4 or 5)) return "Özel sandık türü geçersizdir.";
            if (request.Tur != 3 && request.IcSandikSablonId.HasValue) return "Kayıtlı iç sandık tipi yalnız İç Sandık türünde kullanılabilir.";
            if (request.ProjeId <= 0) return "Proje seçimi zorunludur.";
            var proje = await _context.Projeler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProjeId, cancellationToken);
            if (proje == null) return "Seçilen proje bulunamadı.";
            if (request.Tur == 2 && proje.ProjeTipiId != 1) return "İlave sandık yalnız normal projeye bağlanabilir.";
            if (request.Tur == 4 && proje.ProjeTipiId != 2) return "Saha sandığı yalnız saha projesine bağlanabilir.";
            if (request.Tur == 5 && proje.ProjeTipiId != 3) return "Yedek sandığı yalnız yedek projesine bağlanabilir.";
            if (request.Tur is 4 or 5 && request.KaynakSandikId.HasValue)
                return "Saha ve yedek sandıkları mevcut sandıklardan çekilemez; bilgileri manuel girilmelidir.";
            if (request.KaynakSandikId.HasValue)
            {
                if (request.Tur != 2) return "Kaynak sandık yalnız ilave sandık kaydında seçilebilir.";
                var kaynakUygun = await _context.Sandiklar.AsNoTracking().AnyAsync(s =>
                    s.Id == request.KaynakSandikId &&
                    s.ProjeId == request.ProjeId &&
                    (request.Tur != 2 || s.AmbalajaDahilMi == false), cancellationToken);
                if (!kaynakUygun) return "Seçilen kaynak sandık bu projeye ait değildir.";
                if (request.Tur == 2 && await _context.AmbalajBagimsizSandiklar.AsNoTracking().AnyAsync(s =>
                        s.Tur == 2 && s.KaynakSandikId == request.KaynakSandikId && s.Id != mevcutKayitId,
                        cancellationToken))
                    return "Seçilen sandık daha önce İlave sandık olarak kullanılmıştır.";
            }
            if (request.Tur == 3)
            {
                if (!request.UstKaynakSandikId.HasValue) return "İç sandığın gireceği dış koli seçilmelidir.";
                if (request.IcSandikSablonId.HasValue && !await _context.AmbalajIcSandikSablonlari.AsNoTracking()
                    .AnyAsync(s => s.Id == request.IcSandikSablonId.Value, cancellationToken))
                    return "Seçilen kayıtlı iç sandık tipi bulunamadı.";
                var ustSandikUygun = await _context.Sandiklar.AsNoTracking()
                    .AnyAsync(s => s.Id == request.UstKaynakSandikId && s.ProjeId == request.ProjeId, cancellationToken);
                if (!ustSandikUygun) return "Seçilen dış koli bu projeye ait değildir.";
            }

            if (string.IsNullOrWhiteSpace(request.Ad)) return "Sandık adı zorunludur.";
            if (!GecerliSandikTipleri.Contains(request.SandikTipi)) return "Sandık tipi geçersizdir.";
            if (request.Adet <= 0) return "Adet sıfırdan büyük olmalıdır.";
            if (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0) return "Boy, en ve yükseklik zorunludur.";
            if (string.IsNullOrWhiteSpace(request.TalimatVeren)) return "Talimat veren kişi zorunludur.";
            return null;
        }

        private static void BagimsizSandikGuncelle(AmbalajBagimsizSandik sandik, AmbalajOzelSandikKaydetRequest request)
        {
            sandik.ProjeId = request.ProjeId;
            sandik.KaynakSandikId = request.Tur == 2 ? request.KaynakSandikId : null;
            sandik.UstKaynakSandikId = request.Tur == 3 ? request.UstKaynakSandikId : null;
            sandik.IcSandikSablonId = request.Tur == 3 ? request.IcSandikSablonId : null;
            sandik.Tur = request.Tur;
            sandik.UretimeAlindi = true;
            sandik.Ad = request.Ad!.Trim();
            sandik.SandikTipi = request.SandikTipi;
            sandik.Adet = request.Adet;
            sandik.Boy = request.Boy;
            sandik.En = request.En;
            sandik.Yukseklik = request.Yukseklik;
            sandik.KullanimAmaci = null;
            sandik.TalimatVeren = Temizle(request.TalimatVeren);
            sandik.Aciklama = Temizle(request.Aciklama);
        }

        private static AmbalajBagimsizSandikDto BagimsizSandikDtoOlustur(AmbalajBagimsizSandik sandik) =>
            new(sandik.Id, sandik.Tur, OzelSandikTurMetni(sandik.Tur), sandik.ProjeId, sandik.Proje?.ProjeNo,
                sandik.Proje?.Musteri, sandik.KaynakSandikId, sandik.KaynakSandik?.SandikNo, sandik.KaynakSandik?.Ad,
                sandik.UstKaynakSandikId, sandik.IcSandikSablonId, sandik.UstKaynakSandik?.SandikNo, sandik.UstKaynakSandik?.Ad,
                true, sandik.SandikNo, sandik.Ad, sandik.SandikTipi,
                sandik.Adet, sandik.Boy, sandik.En, sandik.Yukseklik, sandik.KullanimAmaci, sandik.TalimatVeren,
                sandik.Aciklama, sandik.SandikTipi == "Kontrplak Sandık"
                    ? 0
                    : AmbalajHesaplayici.Hesapla(sandik.Boy, sandik.En, sandik.Yukseklik).ToplamHacimM3 * sandik.Adet);

        private async Task<string> SonrakiIcSandikNo(int ustKaynakSandikId, CancellationToken cancellationToken)
        {
            var ustSandikNo = await _context.Sandiklar.AsNoTracking()
                .Where(s => s.Id == ustKaynakSandikId)
                .Select(s => s.SandikNo)
                .SingleAsync(cancellationToken);
            var onEk = $"{ustSandikNo}.";
            var numaralar = await _context.AmbalajBagimsizSandiklar.AsNoTracking()
                .Where(s => s.Tur == 3 && s.UstKaynakSandikId == ustKaynakSandikId && s.SandikNo.StartsWith(onEk))
                .Select(s => s.SandikNo)
                .ToListAsync(cancellationToken);
            var sonSira = numaralar.Select(no => int.TryParse(no[onEk.Length..], out var sira) ? sira : 0)
                .DefaultIfEmpty(0).Max();
            return $"{onEk}{sonSira + 1}";
        }

        private async Task<string> SonrakiBagimsizSandikNo(int tur, CancellationToken cancellationToken)
        {
            var onEk = tur == 2 ? "ILV-" : tur == 3 ? "IC-" : tur == 4 ? "SAH-" : "YDK-";
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
            kalem.IcSandikSablonId = request.Tur == 3 ? request.IcSandikSablonId : null;
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
            "Ahşap Kapalı", "Kafes Sandık", "Kontrplak Sandık", "Katlanır Sandık"
        };

        private static string OzelSandikTurMetni(int tur) => tur switch
        {
            2 => "İlave",
            3 => "İç Sandık",
            4 => "Saha",
            5 => "Yedek",
            _ => "Bilinmiyor"
        };

        private string KullaniciMetni() => _currentUserService.UserId?.ToString() ?? "system";
        private static string? Temizle(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool AmbalajKarariOneriliyor(Sandik sandik)
        {
            if (int.TryParse(sandik.SandikNo.Trim(), out var sandikNo) && sandikNo == 1)
                return true;

            var ad = $"{sandik.Ad} {sandik.AdIngilizce}".ToUpperInvariant()
                .Replace('İ', 'I')
                .Replace('Ş', 'S');
            return ad.Contains("BUSHING", StringComparison.Ordinal)
                || ad.Contains("BUSING", StringComparison.Ordinal)
                || ad.Contains("PARAFUD", StringComparison.Ordinal)
                || ad.Contains("SURGE ARRESTER", StringComparison.Ordinal);
        }

        private static int SandikAdediHesapla(string sandikNo)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sandikNo ?? string.Empty, @"^(\d+)\s*-\s*(\d+)$");
            if (!match.Success)
                return 1;

            var baslangic = int.Parse(match.Groups[1].Value);
            var bitis = int.Parse(match.Groups[2].Value);
            return bitis >= baslangic ? bitis - baslangic + 1 : 1;
        }

        private static int SandikSiraAnahtari(string? sandikNo)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sandikNo ?? string.Empty, @"\d+");
            return match.Success && int.TryParse(match.Value, out var sayi) ? sayi : int.MaxValue;
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

    public sealed record AmbalajUretimKalemDto(int Id, int? KaynakSandikId, int? UstKalemId, int? IcSandikSablonId, int Tur, string TurMetni,
        bool UretimeAlindi, string SandikNo, string? Ad, string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik,
        string? KullanimAmaci, string? TalimatVeren, string? Aciklama, decimal HacimM3,
        bool? AmbalajaDahilMi = true, bool AmbalajKarariOneriliyor = false);

    public sealed record AmbalajPlanKaydetRequest(string? FirinPartiNo, IReadOnlyList<int> SeciliKaynakSandikIds,
        int Grup = 1, int DurumId = 1);

    public sealed record AmbalajKarariKaydetRequest(bool AmbalajaDahilMi);

    public sealed record AmbalajKalemKaydetRequest(int Tur, int? UstKalemId, int? UstKaynakSandikId, int? IcSandikSablonId,
        bool UretimeAlindi, string SandikNo, string? Ad, string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik,
        string? KullanimAmaci, string? TalimatVeren, string? Aciklama);

    public sealed record AmbalajIcSandikSablonDto(int Id, string Ad, string SandikTipi, decimal Boy, decimal En, decimal Yukseklik);
    public sealed record AmbalajIcSandikSablonKaydetRequest(string Ad, string SandikTipi, decimal Boy, decimal En, decimal Yukseklik);
    public sealed record AmbalajTalepEdenDto(int Id, string Ad);
    public sealed record AmbalajTalepEdenKaydetRequest(string? Ad);
    public sealed record AmbalajIlaveSandikAdayDto(int Id, string SandikNo, string? Ad,
        decimal? Boy, decimal? En, decimal? Yukseklik);
    public sealed record AmbalajOzelSandikKaydetRequest(int Tur, int ProjeId, int? KaynakSandikId, int? UstKaynakSandikId, int? IcSandikSablonId,
        bool UretimeAlindi, string SandikNo, string? Ad, string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik,
        string? TalimatVeren, string? Aciklama);
    public sealed record AmbalajBagimsizSandikDto(int Id, int Tur, string TurMetni, int? ProjeId, string? ProjeNo,
        string? Musteri, int? KaynakSandikId, string? KaynakSandikNo, string? KaynakSandikAdi,
        int? UstKaynakSandikId, int? IcSandikSablonId, string? UstSandikNo, string? UstSandikAdi, bool UretimeAlindi, string SandikNo, string Ad,
        string SandikTipi, int Adet, decimal Boy, decimal En, decimal Yukseklik, string? KullanimAmaci,
        string? TalimatVeren, string? Aciklama, decimal HacimM3);
}