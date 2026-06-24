using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class EksiklerdenSahaProjesiOlusturCommandHandler : IRequestHandler<EksiklerdenSahaProjesiOlusturCommand, Result<ProjeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ICurrentUserService _currentUserService;

        public EksiklerdenSahaProjesiOlusturCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ILookupCacheService lookupCache,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
            _currentUserService = currentUserService;
        }

        public async Task<Result<ProjeDto>> Handle(EksiklerdenSahaProjesiOlusturCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sandikTaslaklari = request.Sandiklar
                .Where(s => s.Urunler.Any(u => u.CekiSatiriId > 0 && u.Miktar > 0))
                .Select(s => new EksikSahaSandikDto
                {
                    HedefSandikId = s.HedefSandikId,
                    SandikNo = NormalizeSandikNo(s.SandikNo),
                    SandikIsmi = string.IsNullOrWhiteSpace(s.SandikIsmi) ? null : s.SandikIsmi.Trim(),
                    En = s.En,
                    Boy = s.Boy,
                    Yukseklik = s.Yukseklik,
                    NetKg = s.NetKg,
                    GrossKg = s.GrossKg,
                    Urunler = s.Urunler
                        .Where(u => u.CekiSatiriId > 0 && u.Miktar > 0)
                        .Select(u => new EksikSahaUrunDto
                        {
                            CekiSatiriId = u.CekiSatiriId,
                            KaynakProjeId = u.KaynakProjeId,
                            Miktar = u.Miktar,
                            Aciklama = string.IsNullOrWhiteSpace(u.Aciklama) ? null : u.Aciklama.Trim()
                        })
                        .ToList()
                })
                .ToList();

            if (sandikTaslaklari.Count == 0)
                return Result<ProjeDto>.Failure("Saha projesi oluşturmak için en az bir sandığa ürün eklenmelidir.");

            var bosSandikNoVar = sandikTaslaklari.Any(s => string.IsNullOrWhiteSpace(s.SandikNo));
            if (bosSandikNoVar)
                return Result<ProjeDto>.Failure("Tüm sandıkların sandık numarası olmalıdır.");

            var hedefSandikIdleri = sandikTaslaklari
                .Where(s => s.HedefSandikId.HasValue)
                .Select(s => s.HedefSandikId!.Value)
                .Distinct()
                .ToList();

            if (hedefSandikIdleri.Count > 0 && !request.HedefSahaProjeId.HasValue)
                return Result<ProjeDto>.Failure("Mevcut sandığa ekleme yalnızca mevcut saha projesi seçildiğinde yapılabilir.");

            var tekrarEdenSandikNo = sandikTaslaklari
                .GroupBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1)?.Key;

            if (!string.IsNullOrWhiteSpace(tekrarEdenSandikNo))
                return Result<ProjeDto>.Failure($"'{tekrarEdenSandikNo}' numaralı sandık birden fazla kez kullanılamaz.");

            var gecersizOlcu = sandikTaslaklari.Any(s =>
                IsNegative(s.En) ||
                IsNegative(s.Boy) ||
                IsNegative(s.Yukseklik) ||
                IsNegative(s.NetKg) ||
                IsNegative(s.GrossKg));

            if (gecersizOlcu)
                return Result<ProjeDto>.Failure("Sandık ebat ve ağırlık değerleri negatif olamaz.");

            var talepGruplari = sandikTaslaklari
                .SelectMany(s => s.Urunler)
                .GroupBy(u => u.CekiSatiriId)
                .ToDictionary(g => g.Key, g => g.Sum(u => u.Miktar));
            var talepCekiSatiriIds = talepGruplari.Keys.ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var kaynakSatirlar = (await cekiSatiriRepo.FindAsync(cs =>
                    talepCekiSatiriIds.Contains(cs.Id) &&
                    !cs.KaynakCekiSatiriId.HasValue))
                .ToDictionary(cs => cs.Id);

            if (kaynakSatirlar.Count != talepGruplari.Count)
                return Result<ProjeDto>.Failure("Seçilen ürünlerden bazıları bulunamadı veya eksik tamamlama satırı.");

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var kaynakCekiIdler = kaynakSatirlar.Values.Select(s => s.CekiId).Distinct().ToList();
            var kaynakCekiler = (await cekiRepo.FindAsync(c => kaynakCekiIdler.Contains(c.Id)))
                .ToDictionary(c => c.Id);

            var kaynakProjeIdler = kaynakCekiler.Values.Select(c => c.ProjeId).Distinct().ToList();
            var kaynakProjeler = (await projeRepo.FindAsync(p => kaynakProjeIdler.Contains(p.Id)))
                .ToDictionary(p => p.Id);

            if (kaynakCekiler.Count != kaynakCekiIdler.Count || kaynakProjeler.Count != kaynakProjeIdler.Count)
                return Result<ProjeDto>.Failure("Seçilen ürünlerin kaynak proje bilgisi bulunamadı.");

            if (kaynakProjeler.Values.Any(p => p.ProjeTipiId != (int)ProjeTipi.Normal))
                return Result<ProjeDto>.Failure("Eksiklerden saha projesi sadece normal projelerden oluşturulabilir.");

            var varsayilanKaynakProje = request.KaynakProjeId.HasValue && kaynakProjeler.TryGetValue(request.KaynakProjeId.Value, out var seciliKaynakProje)
                ? seciliKaynakProje
                : kaynakProjeler.Values.OrderBy(p => p.ProjeNo).FirstOrDefault();

            if (varsayilanKaynakProje == null)
                return Result<ProjeDto>.Failure("Kaynak proje bulunamadı.", 404);

            var kaynakProjeNolari = kaynakProjeler.Values
                .OrderBy(p => p.ProjeNo)
                .Select(p => p.ProjeNo)
                .ToList();

            var mevcutTamamlamaSatirlari = (await cekiSatiriRepo.FindAsync(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    talepCekiSatiriIds.Contains(cs.KaynakCekiSatiriId.Value)))
                .ToList();

            var mevcutPlanMap = mevcutTamamlamaSatirlari
                .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(cs => cs.IstenenAdet));

            foreach (var (cekiSatiriId, talepMiktari) in talepGruplari)
            {
                var kaynakSatir = kaynakSatirlar[cekiSatiriId];
                var planlanmis = mevcutPlanMap.GetValueOrDefault(cekiSatiriId);
                var kalanPlanlanabilir = Math.Max(kaynakSatir.KalanMiktar - planlanmis, 0);

                if (talepMiktari > kalanPlanlanabilir)
                    return Result<ProjeDto>.Failure($"'{kaynakSatir.Aciklama}' için istenen miktar kalan eksik adetten büyük. Kalan: {FormatAdet(kalanPlanlanabilir)}");
            }

            Proje? hedefSahaProje = null;
            if (request.HedefSahaProjeId.HasValue)
            {
                hedefSahaProje = await projeRepo.GetByIdAsync(request.HedefSahaProjeId.Value);
                if (hedefSahaProje == null)
                    return Result<ProjeDto>.Failure("Hedef saha projesi bulunamadı.", 404);

                if (hedefSahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
                    return Result<ProjeDto>.Failure("Aktarım sadece saha projelerine eklenebilir.");

                if (hedefSahaProje.DurumId == (int)ProjeDurum.SevkEdildi ||
                    hedefSahaProje.DurumId == (int)ProjeDurum.EksikSevkEdildi)
                    return Result<ProjeDto>.Failure("Sevk edilmiş veya kısmi sevk edilmiş saha projesine yeni aktarım eklenemez.");
            }

            var projeNoTalebi = string.IsNullOrWhiteSpace(request.ProjeNo) ? null : request.ProjeNo.Trim();
            if (hedefSahaProje == null && !string.IsNullOrWhiteSpace(projeNoTalebi))
            {
                var projeNoKullanimda = (await projeRepo.FindAsync(p => p.ProjeNo == projeNoTalebi)).Any();
                if (projeNoKullanimda)
                    return Result<ProjeDto>.Failure($"'{projeNoTalebi}' proje numarası zaten kullanılıyor.");
            }

            var tekKaynakMi = kaynakProjeler.Count == 1;
            var yeniProjeOlusturuldu = hedefSahaProje == null;
            var yeniProje = hedefSahaProje;
            if (yeniProje == null)
            {
                var baseSahaProjeNo = tekKaynakMi
                    ? $"{varsayilanKaynakProje.ProjeNo}-SAHA"
                    : $"SAHA-{TurkeyTime.Now:yyyyMMdd}";
                var sahaProjeNo = projeNoTalebi ?? await GenerateSahaProjeNoAsync(baseSahaProjeNo);
                yeniProje = new Proje
                {
                    ProjeNo = sahaProjeNo,
                    Musteri = string.IsNullOrWhiteSpace(request.Musteri)
                        ? (tekKaynakMi ? varsayilanKaynakProje.Musteri : "Çoklu Proje")
                        : request.Musteri.Trim(),
                    DurumId = (int)ProjeDurum.Hazirlaniyor,
                    ProjeTipiId = (int)ProjeTipi.Saha,
                    PlanlananSevkTarihi = tekKaynakMi ? varsayilanKaynakProje.PlanlananSevkTarihi : null,
                    SorumluKisi = tekKaynakMi ? varsayilanKaynakProje.SorumluKisi : string.Empty,
                    Lokasyon = string.IsNullOrWhiteSpace(request.Lokasyon)
                        ? (tekKaynakMi ? varsayilanKaynakProje.Lokasyon : null)
                        : request.Lokasyon.Trim(),
                    FBNo = tekKaynakMi ? varsayilanKaynakProje.FBNo : null,
                    Guc = tekKaynakMi ? varsayilanKaynakProje.Guc : null,
                    Gerilim = tekKaynakMi ? varsayilanKaynakProje.Gerilim : null,
                    OlcuResmiNo = tekKaynakMi ? varsayilanKaynakProje.OlcuResmiNo : null,
                    NakilOlcuResmiNo = tekKaynakMi ? varsayilanKaynakProje.NakilOlcuResmiNo : null,
                    SonMontajResmiNo = tekKaynakMi ? varsayilanKaynakProje.SonMontajResmiNo : null,
                    ProjeMuduru = tekKaynakMi ? varsayilanKaynakProje.ProjeMuduru : null
                };

                await projeRepo.AddAsync(yeniProje);
                await _unitOfWork.SaveChangesAsync();
            }

            var kaynakCekiId = kaynakCekiIdler.Count == 1 ? kaynakCekiIdler[0] : (int?)null;
            Ceki? sahaCeki = null;
            if (!yeniProjeOlusturuldu)
            {
                sahaCeki = (await cekiRepo.FindAsync(c =>
                        c.ProjeId == yeniProje.Id &&
                        c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
                    .OrderBy(c => c.Id)
                    .FirstOrDefault();
            }

            if (sahaCeki == null)
            {
                var mevcutTamamlamaNo = (await cekiRepo.FindAsync(c =>
                        c.ProjeId == yeniProje.Id &&
                        c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
                    .Select(c => c.TamamlamaNo ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                sahaCeki = new Ceki
                {
                    ProjeId = yeniProje.Id,
                    OrijinalDosyaYolu = string.Empty,
                    YuklemeTarihi = TurkeyTime.Now,
                    CekiTipiId = (int)CekiTipi.EksikTamamlama,
                    KaynakCekiId = kaynakCekiId,
                    TamamlamaNo = mevcutTamamlamaNo + 1,
                    Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                        ? $"Kaynak projeler: {string.Join(", ", kaynakProjeNolari)}"
                        : request.Aciklama.Trim()
                };

                await cekiRepo.AddAsync(sahaCeki);
                await _unitOfWork.SaveChangesAsync();
            }

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var mevcutSahaSatirlari = (await cekiSatiriRepo.FindAsync(cs => cs.CekiId == sahaCeki.Id)).ToList();
            var siraNo = mevcutSahaSatirlari.Any() ? mevcutSahaSatirlari.Max(cs => cs.SiraNo) + 1 : 1;
            var mevcutSahaSandiklari = (await sandikRepo.FindAsync(s => s.ProjeId == yeniProje.Id)).ToList();
            var mevcutSandikNolari = mevcutSahaSandiklari
                .Select(s => s.SandikNo)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hedefSandikMap = mevcutSahaSandiklari
                .Where(s => hedefSandikIdleri.Contains(s.Id))
                .ToDictionary(s => s.Id);

            if (hedefSandikIdleri.Count != hedefSandikMap.Count)
                return Result<ProjeDto>.Failure("Seçilen hedef sandıklardan bazıları saha projesi altında bulunamadı.");

            if (hedefSandikMap.Values.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi))
                return Result<ProjeDto>.Failure("Sevk edilmiş saha sandığına yeni ürün eklenemez.");

            var toplamSatir = 0;
            var toplamAdet = 0m;

            foreach (var sandikTaslak in sandikTaslaklari)
            {
                Sandik sandik;
                if (sandikTaslak.HedefSandikId.HasValue)
                {
                    sandik = hedefSandikMap[sandikTaslak.HedefSandikId.Value];
                }
                else
                {
                    var sandikNo = GenerateUniqueSandikNo(sandikTaslak.SandikNo, mevcutSandikNolari);
                    sandik = new Sandik
                    {
                        ProjeId = yeniProje.Id,
                        SandikNo = sandikNo,
                        Ad = sandikTaslak.SandikIsmi,
                        En = sandikTaslak.En,
                        Boy = sandikTaslak.Boy,
                        Yukseklik = sandikTaslak.Yukseklik,
                        NetKg = sandikTaslak.NetKg,
                        GrossKg = sandikTaslak.GrossKg,
                        TipId = (int)SandikTipi.AhsapKapali,
                        DurumId = (int)SandikDurum.Hazirlaniyor,
                        DepoLokasyonId = (int)DepoLokasyon.Belirsiz
                    };

                    await sandikRepo.AddAsync(sandik);
                    await _unitOfWork.SaveChangesAsync();
                }

                foreach (var urunTaslak in sandikTaslak.Urunler)
                {
                    var kaynakSatir = kaynakSatirlar[urunTaslak.CekiSatiriId];
                    var satirKaynakCeki = kaynakCekiler[kaynakSatir.CekiId];
                    var satirKaynakProje = kaynakProjeler[satirKaynakCeki.ProjeId];
                    var yeniSatir = new CekiSatiri
                    {
                        CekiId = sahaCeki.Id,
                        KaynakCekiSatiriId = kaynakSatir.Id,
                        SiraNo = siraNo++,
                        OlcuResmiPozNo = kaynakSatir.OlcuResmiPozNo,
                        BarkodNo = kaynakSatir.BarkodNo,
                        Aciklama = kaynakSatir.Aciklama,
                        IstenenAdet = urunTaslak.Miktar,
                        BirimId = kaynakSatir.BirimId,
                        CekideGecenSandikNo = sandik.SandikNo,
                        FiiliSandikNo = sandik.SandikNo,
                        Remarks = kaynakSatir.Remarks,
                        DurumId = (int)UrunDurum.Bekliyor,
                        GridDurumuId = (int)GridDurum.Gelmedi,
                        GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                        UcKDurumuId = (int)UcKDurum.Bekliyor,
                        UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                        KaynakHedefProjeNo = satirKaynakProje.ProjeNo,
                        UcKAciklama = urunTaslak.Aciklama,
                        IsManuelEklenen = true,
                        EklemeNedeni = "Eksik saha tamamlama"
                    };

                    await cekiSatiriRepo.AddAsync(yeniSatir);
                    await _unitOfWork.SaveChangesAsync();

                    await icerikRepo.AddAsync(new SandikIcerik
                    {
                        SandikId = sandik.Id,
                        CekiSatiriId = yeniSatir.Id,
                        KonulanAdet = 0,
                        EksikAdet = 0,
                        KaynakProjeNo = satirKaynakProje.ProjeNo,
                        Aciklama = urunTaslak.Aciklama
                    });

                    toplamSatir++;
                    toplamAdet += urunTaslak.Miktar;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = yeniProje.Id,
                ReferansTipi = "Proje",
                ReferansId = yeniProje.Id.ToString(),
                Islem = yeniProjeOlusturuldu ? "Eksiklerden Saha Projesi Oluşturuldu" : "Saha Projesine Eksik Aktarımı Eklendi",
                IslemTipiId = (int)IslemTipi.ProjeOlusturuldu,
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = yeniProjeOlusturuldu
                    ? $"{string.Join(", ", kaynakProjeNolari)} kaynak projesinden {sandikTaslaklari.Count} sandık, {toplamSatir} satır, {FormatAdet(toplamAdet)} adet için saha projesi oluşturuldu."
                    : $"{string.Join(", ", kaynakProjeNolari)} kaynak projesinden {sandikTaslaklari.Count} sandık, {toplamSatir} satır, {FormatAdet(toplamAdet)} adet mevcut saha projesine eklendi."
            });

            return Result<ProjeDto>.Success(new ProjeDto
            {
                Id = yeniProje.Id,
                ProjeNo = yeniProje.ProjeNo,
                Musteri = yeniProje.Musteri,
                DurumId = yeniProje.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupProjeDurum>(yeniProje.DurumId),
                ProjeTipiId = yeniProje.ProjeTipiId,
                ProjeTipiMetni = _lookupCache.GetDeger<LookupProjeTipi>(yeniProje.ProjeTipiId),
                BaslamaTarihi = yeniProje.CreatedDate,
                CalismaGunSayisi = Math.Max(0, (TurkeyTime.Now.Date - yeniProje.CreatedDate.Date).Days),
                PlanlananSevkTarihi = yeniProje.PlanlananSevkTarihi,
                SorumluKisi = yeniProje.SorumluKisi,
                SandikSayisi = sandikTaslaklari.Count,
                ToplamUrunSayisi = toplamSatir,
                Lokasyon = yeniProje.Lokasyon
            });
        }

        private async Task<string> GenerateSahaProjeNoAsync(string baseNo)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var candidate = baseNo;
            var index = 2;

            while ((await projeRepo.FindAsync(p => p.ProjeNo == candidate)).Any())
            {
                candidate = $"{baseNo}-{index}";
                index++;
            }

            return candidate;
        }

        private static string NormalizeSandikNo(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static string GenerateUniqueSandikNo(string baseNo, HashSet<string> usedSandikNos)
        {
            var normalized = NormalizeSandikNo(baseNo);
            if (usedSandikNos.Add(normalized))
                return normalized;

            var index = 2;
            string candidate;
            do
            {
                candidate = $"{normalized}-{index}";
                index++;
            }
            while (!usedSandikNos.Add(candidate));

            return candidate;
        }

        private static bool IsNegative(decimal? value)
        {
            return value.HasValue && value.Value < 0;
        }

        private static string FormatAdet(decimal value)
        {
            return decimal.Truncate(value) == value
                ? decimal.Truncate(value).ToString("0")
                : value.ToString("0.####");
        }
    }
}
