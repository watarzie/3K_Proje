using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

// Planlama komutları tek transaction içinde çalışır: önce değişiklik öncesi kayıtlar üzerinden
// kritik işlem kapıları denetlenir, ardından değişiklik uygulanır ve kritik işlem geçmişe yazılır.
// Herhangi bir adım başarısız olursa senkronizasyon dahil bütün değişiklikler geri alınır.

public sealed class AmbalajPlanKaydetCommandHandler
    : IRequestHandler<AmbalajPlanKaydetCommand, Result<AmbalajPlanlamaPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRolService _roller;
    private readonly IAmbalajKaynakSenkronizasyonService _senkronizasyon;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajPlanKaydetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRolService roller,
        IAmbalajKaynakSenkronizasyonService senkronizasyon,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _roller = roller;
        _senkronizasyon = senkronizasyon;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result<AmbalajPlanlamaPlanDto>> Handle(
        AmbalajPlanKaydetCommand request,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var hedefTur = AmbalajPlanlamaYardimcisi.GrupTuru(request.Grup);
            var firinPartiNo = AmbalajUretimYardimcilari.Temizle(request.FirinPartiNo);
            var hedefler = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.ProjeId == request.ProjeId && !k.IptalMi && !k.BagimsizKayitMi && k.Tur == hedefTur)
                .ToList();
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler, degisiklik => degisiklik with
            {
                HedefUretimSecimi = degisiklik.Kayit.KaynakKayitId is { } kaynakId
                    ? request.SeciliKaynakSandikIds.Contains(kaynakId)
                    : null,
                PartiDegisiyor = (request.Grup == 3
                        ? !degisiklik.Kayit.KaynakKayitId.HasValue
                        : degisiklik.Kayit.KaynakKayitId.HasValue && degisiklik.Kayit.AmbalajaDahil) &&
                    !string.Equals(AmbalajUretimYardimcilari.Temizle(degisiklik.Kayit.FirinPartiNo), firinPartiNo, StringComparison.Ordinal)
            }));
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, request.Gerekce, token, gerekceZorunlu: false);
            if (!kapi.IsSuccess)
                return Result<AmbalajPlanlamaPlanDto>.Failure(kapi.Error!.Message, kapi.StatusCode);

            var sonuc = await KaydetAsync(request, token);
            if (sonuc.IsSuccess)
                await _denetim.KritikIslemiKaydetAsync(gereksinim, hedefler, nameof(AmbalajPlanKaydetCommand), request.Gerekce, token);
            return sonuc;
        }, cancellationToken);

    private async Task<Result<AmbalajPlanlamaPlanDto>> KaydetAsync(
        AmbalajPlanKaydetCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Grup is not (1 or 2 or 3))
            return Result<AmbalajPlanlamaPlanDto>.Failure("Üretim grubu geçersiz.");
        if (!_currentUser.IslemKullaniciId.HasValue)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

        var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
        if (proje == null) return Result<AmbalajPlanlamaPlanDto>.Failure("Proje bulunamadı.", 404);
        if (proje.ProjeTipiId != (int)ProjeTipi.Normal)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Ambalaj üretim planı yalnız normal projeler için kullanılabilir.");
        if (request.KaynakProjeTipiId.HasValue && request.KaynakProjeTipiId.Value != proje.ProjeTipiId)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Proje seçilen yönetim kaynağına ait değil.");

        var senkron = await _senkronizasyon.SenkronizeEtAsync(proje.Id, _currentUser, cancellationToken);
        if (!senkron.IsSuccess)
            return Result<AmbalajPlanlamaPlanDto>.Failure(senkron.Error!.Message, senkron.Error.Code);

        var sandiklar = _unitOfWork.GetRepository<Sandik>().Queryable()
            .Where(s => s.ProjeId == proje.Id).ToList();
        var kayitRepo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        var tumKayitlar = kayitRepo.Queryable()
            .Where(k => k.ProjeId == proje.Id).ToList();
        var kayitlar = tumKayitlar.Where(k => !k.IptalMi).ToList();
        var planKayitlari = kayitlar.Where(k => !k.BagimsizKayitMi).ToList();
        var kaynakMap = planKayitlari.Where(k => k.KaynakKayitId.HasValue)
            .ToDictionary(k => k.KaynakKayitId!.Value);
        var hedefTur = AmbalajPlanlamaYardimcisi.GrupTuru(request.Grup);

        // RR Q2: senkron sonrası her kaynak sandığın kaydı vardır; grup yalnız kaydın Tur alanından okunur.
        var gecerliKayitlar = request.Grup == 3
            ? planKayitlari.Where(k => !k.KaynakKayitId.HasValue && k.Tur == AmbalajSandikTuru.Ic).ToList()
            : sandiklar
                .Where(s => kaynakMap.TryGetValue(s.Id, out var k) && k.AmbalajaDahil)
                .Where(s =>
                {
                    var ilave = kaynakMap[s.Id].Tur == AmbalajSandikTuru.Ilave;
                    return request.Grup == 2 ? ilave : !ilave;
                })
                .Select(s => kaynakMap[s.Id])
                .ToList();
        var gecerliKaynakIds = gecerliKayitlar.Where(k => k.KaynakKayitId.HasValue)
            .Select(k => k.KaynakKayitId!.Value).ToHashSet();
        if (request.SeciliKaynakSandikIds.Any(id => !gecerliKaynakIds.Contains(id)))
            return Result<AmbalajPlanlamaPlanDto>.Failure("Seçilen sandıklardan biri bu üretim grubuna ait değil.");

        var gecersizSecimler = gecerliKayitlar
            .Where(k => k.KaynakKayitId.HasValue
                ? request.SeciliKaynakSandikIds.Contains(k.KaynakKayitId.Value)
                : k.UretimeAlindi)
            .Where(k => !AmbalajUretimYardimcilari.UretimMiktariGecerli(k))
            .Select(k => k.SandikNo)
            .ToList();
        if (gecersizSecimler.Count > 0)
            return Result<AmbalajPlanlamaPlanDto>.Failure(
                $"Ölçüleri ve manuel m³ değeri olmayan sandıklar üretime alınamaz: {string.Join(", ", gecersizSecimler)}",
                409);

        var aktarilacaklar = new List<AmbalajUretimKaydi>();
        var firinPartiNo = AmbalajUretimYardimcilari.Temizle(request.FirinPartiNo);
        foreach (var kayit in gecerliKayitlar)
        {
            var secili = kayit.KaynakKayitId.HasValue
                ? request.SeciliKaynakSandikIds.Contains(kayit.KaynakKayitId.Value)
                : kayit.UretimeAlindi;
            if (kayit.Tur == hedefTur && kayit.UretimeAlindi == secili &&
                string.Equals(kayit.FirinPartiNo, firinPartiNo, StringComparison.Ordinal))
                continue;
            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.Tur = hedefTur;
            kayit.UretimeAlindi = secili;
            kayit.FirinPartiNo = firinPartiNo;
            // Plan seçimi fiziksel başlangıç/tamamlama değildir. İlk form ve açık
            // durum komutları tarihleri ve gerçekleşmeyi kendi lifecycle kapısında yönetir.
            kayit.UpdatedDate = TurkeyTime.Now;
            kayit.UpdatedBy = _currentUser.IslemKullaniciId.Value.ToString();
            kayitRepo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork, kayit, eski, "Üretim planı güncellendi", _currentUser.IslemKullaniciId.Value,
                $"Grup: {request.Grup}");
            aktarilacaklar.Add(kayit);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (aktarilacaklar.Count > 0)
            await _finans.UretimKayitlariniAktarAsync(
                aktarilacaklar.Select(k => AmbalajFinansSenkronizasyonu.ModelOlustur(k, proje)).ToList(),
                cancellationToken);

        var tipMetni = _unitOfWork.GetRepository<LookupProjeTipi>().Queryable()
            .Where(x => x.Id == proje.ProjeTipiId).Select(x => x.Deger).FirstOrDefault() ?? "-";
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _currentUser, cancellationToken);
        return Result<AmbalajPlanlamaPlanDto>.Success(AmbalajAlanMaskesi.Maskele(
            AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, tipMetni, sandiklar, tumKayitlar, request.Grup), izin));
    }
}

public sealed class AmbalajKarariKaydetCommandHandler
    : IRequestHandler<AmbalajKarariKaydetCommand, Result<AmbalajPlanlamaPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRolService _roller;
    private readonly IAmbalajKaynakSenkronizasyonService _senkronizasyon;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajKarariKaydetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRolService roller,
        IAmbalajKaynakSenkronizasyonService senkronizasyon,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _roller = roller;
        _senkronizasyon = senkronizasyon;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result<AmbalajPlanlamaPlanDto>> Handle(
        AmbalajKarariKaydetCommand request,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var kayitlar = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable();
            var hedefler = kayitlar.Where(k => k.KaynakKayitId == request.SandikId).ToList();
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler, degisiklik => degisiklik with
            {
                HedefAmbalajaDahil = request.AmbalajaDahilMi,
                ProjeTamamlandi = request.AmbalajaDahilMi && !degisiklik.Kayit.AmbalajaDahil &&
                    degisiklik.Kayit.ProjeId is { } projeId && _denetim.ProjeTamamlandiMi(projeId)
            }));
            // Henüz senkronize edilmemiş kaynak tamamlanmış projeye dahil ediliyorsa yeni kayıt eklenmiş olur.
            var yeniKayitProjesi = request.AmbalajaDahilMi && hedefler.Count == 0
                ? _unitOfWork.GetRepository<Sandik>().Queryable()
                    .Where(s => s.Id == request.SandikId).Select(s => (int?)s.ProjeId).FirstOrDefault()
                : null;
            if (yeniKayitProjesi is { } yeniProjeId && _denetim.ProjeTamamlandiMi(yeniProjeId))
                AmbalajIslemKurallari.TamamlanmisProjeyeEkleme(gereksinim);
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, request.Gerekce, token, gerekceZorunlu: false);
            if (!kapi.IsSuccess)
                return Result<AmbalajPlanlamaPlanDto>.Failure(kapi.Error!.Message, kapi.StatusCode);

            var sonuc = await KaydetAsync(request, token);
            if (sonuc.IsSuccess)
            {
                var etkilenenler = hedefler.Count == 0 && yeniKayitProjesi.HasValue
                    ? kayitlar.Where(k => k.KaynakKayitId == request.SandikId && !k.IptalMi).Take(1).ToList()
                    : hedefler;
                await _denetim.KritikIslemiKaydetAsync(gereksinim, etkilenenler, nameof(AmbalajKarariKaydetCommand), request.Gerekce, token);
            }
            return sonuc;
        }, cancellationToken);

    private async Task<Result<AmbalajPlanlamaPlanDto>> KaydetAsync(
        AmbalajKarariKaydetCommand request,
        CancellationToken cancellationToken)
    {
        var sandik = await _unitOfWork.GetRepository<Sandik>().GetByIdAsync(request.SandikId);
        if (sandik == null) return Result<AmbalajPlanlamaPlanDto>.Failure("Sandık bulunamadı.", 404);
        if (!_currentUser.IslemKullaniciId.HasValue)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);
        var senkron = await _senkronizasyon.SenkronizeEtAsync(sandik.ProjeId, _currentUser, cancellationToken);
        if (!senkron.IsSuccess)
            return Result<AmbalajPlanlamaPlanDto>.Failure(senkron.Error!.Message, senkron.Error.Code);

        var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(sandik.ProjeId);
        var kayitRepo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        var kayit = kayitRepo.Queryable().FirstOrDefault(k => k.KaynakKayitId == sandik.Id && !k.IptalMi);
        if (proje == null || kayit == null)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Ambalaj kaynak kaydı bulunamadı.", 404);

        var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
        kayit.AmbalajKarariniUygula(request.AmbalajaDahilMi);
        kayit.UpdatedDate = TurkeyTime.Now;
        kayit.UpdatedBy = _currentUser.IslemKullaniciId.Value.ToString();
        kayitRepo.Update(kayit);
        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
            _unitOfWork, kayit, eski, "Ambalaj kararı güncellendi", _currentUser.IslemKullaniciId.Value);
        await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
            _unitOfWork, _finans, kayit, proje, cancellationToken);

        var sandiklar = _unitOfWork.GetRepository<Sandik>().Queryable().Where(s => s.ProjeId == proje.Id).ToList();
        var kayitlar = kayitRepo.Queryable().Where(k => k.ProjeId == proje.Id).ToList();
        var tipMetni = _unitOfWork.GetRepository<LookupProjeTipi>().Queryable()
            .Where(x => x.Id == proje.ProjeTipiId).Select(x => x.Deger).FirstOrDefault() ?? "-";
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _currentUser, cancellationToken);
        return Result<AmbalajPlanlamaPlanDto>.Success(AmbalajAlanMaskesi.Maskele(
            AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, tipMetni, sandiklar, kayitlar), izin));
    }
}

public sealed class AmbalajPlanKalemKaydetCommandHandler
    : IRequestHandler<AmbalajPlanKalemKaydetCommand, Result<AmbalajPlanlamaKalemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRolService _roller;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajPlanKalemKaydetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRolService roller,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _roller = roller;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result<AmbalajPlanlamaKalemDto>> Handle(
        AmbalajPlanKalemKaydetCommand request,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var kayitlar = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable();
            var hedefler = request.KalemId is { } kalemId ? kayitlar.Where(k => k.Id == kalemId).ToList() : [];
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler,
                degisiklik => degisiklik with { VeriDegisiyor = true }));
            if (!request.KalemId.HasValue && request.ProjeId is { } projeId && _denetim.ProjeTamamlandiMi(projeId))
                AmbalajIslemKurallari.TamamlanmisProjeyeEkleme(gereksinim);
            var gerekce = AmbalajKritikIslemDenetimi.GerekceSec(request.Gerekce, request.Aciklama);
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, gerekce, token, gerekceZorunlu: false);
            if (!kapi.IsSuccess)
                return Result<AmbalajPlanlamaKalemDto>.Failure(kapi.Error!.Message, kapi.StatusCode);

            var sonuc = await KaydetAsync(request, token);
            if (sonuc.IsSuccess)
            {
                var etkilenenler = hedefler.Count > 0 ? hedefler : kayitlar.Where(k => k.Id == sonuc.Value!.Id).ToList();
                await _denetim.KritikIslemiKaydetAsync(gereksinim, etkilenenler, nameof(AmbalajPlanKalemKaydetCommand), gerekce, token);
            }
            return sonuc;
        }, cancellationToken);

    private async Task<Result<AmbalajPlanlamaKalemDto>> KaydetAsync(
        AmbalajPlanKalemKaydetCommand request,
        CancellationToken cancellationToken)
    {
        // RR Q1 (A): ölçü görme izni olmayan kullanıcıya ölçüler maskeli gelir; mevcut kaydı düzenlerken
        // kayıtlı ölçüler korunur, istekteki ölçü alanları yok sayılır.
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _currentUser, cancellationToken);
        var olcuKorunur = request.KalemId.HasValue && !izin.OlcuGorunur;
        var hata = Dogrula(request, olcuKorunur);
        if (hata != null) return Result<AmbalajPlanlamaKalemDto>.Failure(hata);
        if (!_currentUser.IslemKullaniciId.HasValue)
            return Result<AmbalajPlanlamaKalemDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);
        if (request.IcSandikSablonId.HasValue && !_unitOfWork.GetRepository<AmbalajIcSandikSablonu>()
                .Queryable().Any(x => x.Id == request.IcSandikSablonId.Value))
            return Result<AmbalajPlanlamaKalemDto>.Failure("Seçilen kayıtlı iç sandık tipi bulunamadı.");

        var kayitRepo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        AmbalajUretimKaydi kayit;
        Proje? proje;
        IReadOnlyDictionary<string, string?>? eski = null;
        if (request.KalemId.HasValue)
        {
            kayit = await kayitRepo.GetByIdAsync(request.KalemId.Value) ?? null!;
            if (kayit == null || kayit.IptalMi || kayit.BagimsizKayitMi || kayit.KaynakKayitId.HasValue)
                return Result<AmbalajPlanlamaKalemDto>.Failure("Ambalaj kalemi bulunamadı.", 404);
            proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            eski = AmbalajUretimYardimcilari.Snapshot(kayit);
        }
        else
        {
            if (!request.ProjeId.HasValue)
                return Result<AmbalajPlanlamaKalemDto>.Failure("Proje bulunamadı.", 404);
            proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId.Value);
            if (proje == null) return Result<AmbalajPlanlamaKalemDto>.Failure("Proje bulunamadı.", 404);
            if (request.Tur == 1 && proje.ProjeTipiId == (int)ProjeTipi.Normal)
                return Result<AmbalajPlanlamaKalemDto>.Failure("Normal projelere manuel sandık eklenemez.");
            kayit = new AmbalajUretimKaydi
            {
                IsAkisKimligi = Guid.NewGuid(),
                ProjeId = proje.Id,
                BagimsizKayitMi = false,
                KaynakModul = AmbalajKaynakModulu.Manuel,
                AmbalajaDahil = true,
                SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
                CreatedBy = _currentUser.IslemKullaniciId.Value.ToString()
            };
            await kayitRepo.AddAsync(kayit);
        }

        var sandikNo = string.IsNullOrWhiteSpace(request.SandikNo)
            ? SonrakiSandikNo(kayitRepo, proje!.Id, request.Tur)
            : request.SandikNo.Trim();
        if (kayitRepo.Queryable().Any(k => k.Id != kayit.Id && k.ProjeId == proje!.Id &&
                                           !k.IptalMi && k.SandikNo.ToUpper() == sandikNo.ToUpper()))
            return Result<AmbalajPlanlamaKalemDto>.Failure("Bu projede aynı sandık numarasıyla aktif bir üretim kaydı zaten var.", 409);

        AlanlariUygula(kayit, request, sandikNo, olcuKorunur);
        kayit.UpdatedDate = request.KalemId.HasValue ? TurkeyTime.Now : null;
        kayit.UpdatedBy = request.KalemId.HasValue ? _currentUser.IslemKullaniciId.Value.ToString() : null;
        kayitRepo.Update(kayit);
        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
            _unitOfWork, kayit, eski,
            request.KalemId.HasValue ? "Ambalaj kalemi güncellendi" : "Ambalaj kalemi eklendi",
            _currentUser.IslemKullaniciId.Value, request.Aciklama);
        await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
            _unitOfWork, _finans, kayit, proje, cancellationToken);
        return Result<AmbalajPlanlamaKalemDto>.Success(
            AmbalajAlanMaskesi.Maskele(AmbalajPlanlamaYardimcisi.KalemDtoOlustur(kayit), izin));
    }

    private static string? Dogrula(AmbalajPlanKalemKaydetCommand request, bool olcuKorunur)
    {
        if (request.Tur is not (1 or 2 or 3)) return "Sandık grubu geçersizdir.";
        if (request.Tur != 3 && request.IcSandikSablonId.HasValue)
            return "Kayıtlı iç sandık tipi yalnız İç Sandık grubunda kullanılabilir.";
        if (string.IsNullOrWhiteSpace(request.Ad)) return "Sandık adı zorunludur.";
        if (!AmbalajPlanlamaYardimcisi.GecerliSandikTipi(request.SandikTipi)) return "Sandık tipi geçersizdir.";
        if (request.Adet <= 0) return "Adet sıfırdan büyük olmalıdır.";
        if (!olcuKorunur && (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0))
            return "Boy, en ve yükseklik zorunludur.";
        if (string.IsNullOrWhiteSpace(request.TalimatVeren)) return "Talimat veren kişi zorunludur.";
        if (request.TalimatVeren.Trim().Length > 200)
            return "Talimat veren kişi en fazla 200 karakter olabilir.";
        return null;
    }

    private static void AlanlariUygula(
        AmbalajUretimKaydi kayit,
        AmbalajPlanKalemKaydetCommand request,
        string sandikNo,
        bool olcuKorunur)
    {
        kayit.Tur = AmbalajPlanlamaYardimcisi.GrupTuru(request.Tur);
        kayit.UstKayitId = request.UstKalemId;
        kayit.IcSandikSablonId = request.Tur == 3 ? request.IcSandikSablonId : null;
        kayit.UretimeAlindi = request.UretimeAlindi;
        kayit.SandikNo = sandikNo;
        kayit.Ad = request.Ad!.Trim();
        kayit.SandikCinsi = AmbalajPlanlamaYardimcisi.SandikCinsiCoz(request.SandikTipi);
        kayit.DigerSandikCinsi = null;
        kayit.Adet = request.Adet;
        if (!olcuKorunur)
        {
            kayit.Boy = request.Boy;
            kayit.En = request.En;
            kayit.Yukseklik = request.Yukseklik;
        }
        kayit.KullanimAmaci = AmbalajUretimYardimcilari.Temizle(request.KullanimAmaci);
        kayit.TalimatVeren = AmbalajUretimYardimcilari.Temizle(request.TalimatVeren);
        kayit.Aciklama = AmbalajUretimYardimcilari.Temizle(request.Aciklama);
        kayit.UretimTarihi = request.UretimeAlindi ? kayit.UretimTarihi ?? TurkeyTime.Now : null;
        AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
    }

    private static string SonrakiSandikNo(IGenericRepository<AmbalajUretimKaydi> repo, int projeId, int tur)
    {
        var onEk = tur == 1 ? "MAN-" : tur == 2 ? "ILV-" : "IC-";
        var sonSira = repo.Queryable().Where(k => k.ProjeId == projeId && !k.IptalMi &&
                                                 !k.KaynakKayitId.HasValue && (int)k.Tur == tur)
            .Select(k => k.SandikNo).ToList()
            .Select(no => no.StartsWith(onEk, StringComparison.OrdinalIgnoreCase) &&
                          int.TryParse(no[onEk.Length..], out var sira) ? sira : 0)
            .DefaultIfEmpty(0).Max();
        return $"{onEk}{sonSira + 1:000}";
    }
}

public sealed class AmbalajPlanKalemSilCommandHandler
    : IRequestHandler<AmbalajPlanKalemSilCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajPlanKalemSilCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result> Handle(AmbalajPlanKalemSilCommand request, CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var hedefler = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.Id == request.KalemId).ToList();
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler,
                degisiklik => degisiklik with { Siliniyor = true }));
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, request.Gerekce, token);
            if (!kapi.IsSuccess)
                return kapi;

            var sonuc = await SilAsync(request, token);
            if (sonuc.IsSuccess)
                await _denetim.KritikIslemiKaydetAsync(gereksinim, hedefler, nameof(AmbalajPlanKalemSilCommand), request.Gerekce, token);
            return sonuc;
        }, cancellationToken);

    private async Task<Result> SilAsync(AmbalajPlanKalemSilCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        var kayit = await repo.GetByIdAsync(request.KalemId);
        if (kayit == null || kayit.IptalMi || kayit.BagimsizKayitMi || kayit.KaynakKayitId.HasValue)
            return Result.Failure("Ambalaj kalemi bulunamadı.", 404);
        if (repo.Queryable().Any(k => k.UstKayitId == kayit.Id && !k.IptalMi))
            return Result.Failure("Bu sandığa bağlı iç sandıklar silinmeden ana sandık silinemez.");
        if (!_currentUser.IslemKullaniciId.HasValue) return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);

        var proje = kayit.ProjeId.HasValue
            ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
            : null;
        var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
        kayit.KullaniciAdinaIptalEt(
            _currentUser.IslemKullaniciId.Value, AmbalajUretimYardimcilari.Temizle(request.Gerekce));
        repo.Update(kayit);
        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
            _unitOfWork, kayit, eski, "Ambalaj kalemi silindi", _currentUser.IslemKullaniciId.Value);
        await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
            _unitOfWork, _finans, kayit, proje, cancellationToken);
        return Result.Success();
    }
}

public sealed class AmbalajIcSandikSablonuEkleCommandHandler
    : IRequestHandler<AmbalajIcSandikSablonuEkleCommand, Result<AmbalajIcSandikSablonDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRolService _roller;

    public AmbalajIcSandikSablonuEkleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IRolService roller)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _roller = roller;
    }

    public async Task<Result<AmbalajIcSandikSablonDto>> Handle(
        AmbalajIcSandikSablonuEkleCommand request,
        CancellationToken cancellationToken)
    {
        var ad = request.Ad.Trim();
        if (string.IsNullOrWhiteSpace(ad))
            return Result<AmbalajIcSandikSablonDto>.Failure("Şablon adı zorunludur.");
        if (!AmbalajPlanlamaYardimcisi.GecerliSandikTipi(request.SandikTipi))
            return Result<AmbalajIcSandikSablonDto>.Failure("Sandık tipi geçersizdir.");
        if (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0)
            return Result<AmbalajIcSandikSablonDto>.Failure("Boy, en ve yükseklik zorunludur.");
        var repo = _unitOfWork.GetRepository<AmbalajIcSandikSablonu>();
        if (repo.Queryable().Any(x => x.Ad.ToLower() == ad.ToLower()))
            return Result<AmbalajIcSandikSablonDto>.Failure("Bu isimde bir iç sandık şablonu zaten var.", 409);

        var entity = new AmbalajIcSandikSablonu
        {
            Ad = ad,
            SandikCinsi = AmbalajPlanlamaYardimcisi.SandikCinsiCoz(request.SandikTipi),
            Boy = request.Boy,
            En = request.En,
            Yukseklik = request.Yukseklik,
            CreatedBy = _currentUser.IslemKullaniciId?.ToString()
        };
        await repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _currentUser, cancellationToken);
        return Result<AmbalajIcSandikSablonDto>.Success(AmbalajAlanMaskesi.Maskele(new AmbalajIcSandikSablonDto(
            entity.Id, entity.Ad, request.SandikTipi, entity.Boy, entity.En, entity.Yukseklik), izin));
    }
}

public sealed class AmbalajIcSandikSablonuSilCommandHandler
    : IRequestHandler<AmbalajIcSandikSablonuSilCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    public AmbalajIcSandikSablonuSilCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> Handle(AmbalajIcSandikSablonuSilCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.GetRepository<AmbalajIcSandikSablonu>();
        var entity = await repo.GetByIdAsync(request.SablonId);
        if (entity == null) return Result.Failure("İç sandık şablonu bulunamadı.", 404);
        if (_unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Any(k => k.IcSandikSablonId == entity.Id && !k.IptalMi))
            return Result.Failure("Kullanımda olan iç sandık şablonu silinemez.", 409);
        repo.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AmbalajBagimsizSandikKaydetCommandHandler
    : IRequestHandler<AmbalajBagimsizSandikKaydetCommand, Result<AmbalajBagimsizSandikDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRolService _roller;
    private readonly IAmbalajKaynakSenkronizasyonService _senkronizasyon;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajBagimsizSandikKaydetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRolService roller,
        IAmbalajKaynakSenkronizasyonService senkronizasyon,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _roller = roller;
        _senkronizasyon = senkronizasyon;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result<AmbalajBagimsizSandikDto>> Handle(
        AmbalajBagimsizSandikKaydetCommand request,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var kayitlar = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable();
            var hedefler = request.SandikId is { } sandikId ? kayitlar.Where(k => k.Id == sandikId).ToList() : [];
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler,
                degisiklik => degisiklik with { VeriDegisiyor = true }));
            if (!request.SandikId.HasValue && _denetim.ProjeTamamlandiMi(request.ProjeId))
                AmbalajIslemKurallari.TamamlanmisProjeyeEkleme(gereksinim);
            var gerekce = AmbalajKritikIslemDenetimi.GerekceSec(request.Gerekce, request.Aciklama);
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, gerekce, token);
            if (!kapi.IsSuccess)
                return Result<AmbalajBagimsizSandikDto>.Failure(kapi.Error!.Message, kapi.StatusCode);

            var sonuc = await KaydetAsync(request, token);
            if (sonuc.IsSuccess)
            {
                var etkilenenler = hedefler.Count > 0 ? hedefler : kayitlar.Where(k => k.Id == sonuc.Value!.Id).ToList();
                await _denetim.KritikIslemiKaydetAsync(gereksinim, etkilenenler, nameof(AmbalajBagimsizSandikKaydetCommand), gerekce, token);
            }
            return sonuc;
        }, cancellationToken);

    private async Task<Result<AmbalajBagimsizSandikDto>> KaydetAsync(
        AmbalajBagimsizSandikKaydetCommand request,
        CancellationToken cancellationToken)
    {
        // RR Q1 (A): ölçü görme izni yoksa mevcut kaydın (veya dönüştürülen kaynak kaydın) ölçüleri korunur.
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _currentUser, cancellationToken);
        var olcuKorunur = !izin.OlcuGorunur && (request.SandikId.HasValue || request.KaynakSandikId.HasValue);
        var hata = await DogrulaAsync(request, olcuKorunur);
        if (hata != null) return Result<AmbalajBagimsizSandikDto>.Failure(hata);
        if (!_currentUser.IslemKullaniciId.HasValue)
            return Result<AmbalajBagimsizSandikDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);
        var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
        if (proje == null) return Result<AmbalajBagimsizSandikDto>.Failure("Seçilen proje bulunamadı.", 404);

        if (request.KaynakSandikId.HasValue || request.UstKaynakSandikId.HasValue)
        {
            var senkron = await _senkronizasyon.SenkronizeEtAsync(proje.Id, _currentUser, cancellationToken);
            if (!senkron.IsSuccess)
                return Result<AmbalajBagimsizSandikDto>.Failure(senkron.Error!.Message, senkron.Error.Code);
        }

        var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        AmbalajUretimKaydi? kayit = request.SandikId.HasValue
            ? await repo.GetByIdAsync(request.SandikId.Value)
            : null;
        IReadOnlyDictionary<string, string?>? eski = null;
        if (request.SandikId.HasValue && (kayit == null || !kayit.BagimsizKayitMi || kayit.IptalMi))
            return Result<AmbalajBagimsizSandikDto>.Failure("Sandık bulunamadı.", 404);

        Sandik? kaynakSandik = null;
        if (request.KaynakSandikId.HasValue)
        {
            kaynakSandik = await _unitOfWork.GetRepository<Sandik>().GetByIdAsync(request.KaynakSandikId.Value);
            var kaynakKaydi = repo.Queryable()
                .FirstOrDefault(k => k.KaynakKayitId == request.KaynakSandikId.Value && !k.IptalMi);
            if (kayit == null) kayit = kaynakKaydi;
            else if (kayit.KaynakKayitId != request.KaynakSandikId.Value)
                return Result<AmbalajBagimsizSandikDto>.Failure("Kayıtlı ilave sandığın kaynak sandığı değiştirilemez.", 409);
            if (kayit == null)
                return Result<AmbalajBagimsizSandikDto>.Failure("Seçilen kaynak sandık senkronize edilemedi.", 409);
        }

        AmbalajUretimKaydi? ustKayit = null;
        if (request.UstKaynakSandikId.HasValue)
            ustKayit = repo.Queryable().FirstOrDefault(k =>
                k.KaynakKayitId == request.UstKaynakSandikId.Value && !k.IptalMi);

        var tur = AmbalajPlanlamaYardimcisi.OzelTurCoz(request.Tur);
        var sandikNo = SandikNoBelirle(kayit ?? new AmbalajUretimKaydi(), request, ustKayit, repo);
        // A1: aynı proje + grup + sandık no, iptal edilmemiş özel sandıklarda tekrar eklenemez.
        if (AmbalajPlanlamaYardimcisi.OzelSandikTekrariVarMi(repo.Queryable(), kayit?.Id ?? 0, proje.Id, tur, sandikNo))
            return Result<AmbalajBagimsizSandikDto>.Failure(
                $"{proje.ProjeNo} projesinde {sandikNo} numaralı sandık bu grupta zaten özel sandık olarak kayıtlı.", 409);

        var yeniKayitMi = kayit == null;
        if (kayit == null)
        {
            kayit = new AmbalajUretimKaydi
            {
                IsAkisKimligi = Guid.NewGuid(),
                ProjeId = proje.Id,
                KaynakModul = request.Tur switch
                {
                    4 => AmbalajKaynakModulu.Saha,
                    5 => AmbalajKaynakModulu.Yedek,
                    _ => AmbalajKaynakModulu.Manuel
                },
                AmbalajaDahil = true,
                BagimsizKayitMi = true,
                SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
                CreatedBy = _currentUser.IslemKullaniciId.Value.ToString()
            };
            await repo.AddAsync(kayit);
        }
        else
        {
            eski = AmbalajUretimYardimcilari.Snapshot(kayit);
        }

        kayit.BagimsizKayitMi = true;
        kayit.ProjeId = proje.Id;
        kayit.Tur = tur;
        kayit.UstKayitId = request.Tur == 3 ? ustKayit?.Id : null;
        kayit.IcSandikSablonId = request.Tur == 3 ? request.IcSandikSablonId : null;
        // Özel sandık normal plan sandıkları gibi üretim formu oluşturulunca üretime alınır ve finansa o zaman
        // aktarılır; kaydetmek tek başına iş kaydı açmaz. Mevcut kaydın üretime alınma durumu korunur.
        if (yeniKayitMi) kayit.UretimeAlindi = false;
        kayit.AmbalajaDahil = true;
        kayit.SandikNo = sandikNo;
        kayit.Ad = request.Ad!.Trim();
        // A7: projedeki sandıktan dönüştürülen kaydın cinsi kaynak sandığın tipidir; iki enum arasında
        // sayısal dönüşüm yapılmaz, formdan gelen metin kaynak tipi ezemez.
        kayit.SandikCinsi = kaynakSandik is not null
            ? AmbalajUretimPolitikasi.KaynakCinsi(kaynakSandik.TipId)
            : AmbalajPlanlamaYardimcisi.SandikCinsiCoz(request.SandikTipi);
        kayit.DigerSandikCinsi = null;
        kayit.Adet = request.Adet;
        if (!olcuKorunur)
        {
            kayit.Boy = request.Boy;
            kayit.En = request.En;
            kayit.Yukseklik = request.Yukseklik;
        }
        kayit.TalimatVeren = AmbalajUretimYardimcilari.Temizle(request.TalimatVeren);
        kayit.Aciklama = AmbalajUretimYardimcilari.Temizle(request.Aciklama);
        if (kayit.UretimeAlindi) kayit.UretimTarihi ??= TurkeyTime.Now;
        kayit.UpdatedDate = eski == null ? null : TurkeyTime.Now;
        kayit.UpdatedBy = eski == null ? null : _currentUser.IslemKullaniciId.Value.ToString();
        AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        if (!yeniKayitMi)
            repo.Update(kayit);
        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
            _unitOfWork, kayit, eski,
            eski == null ? "Bağımsız ambalaj sandığı eklendi" : "Bağımsız ambalaj sandığı güncellendi",
            _currentUser.IslemKullaniciId.Value, request.Aciklama);
        await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
            _unitOfWork, _finans, kayit, proje, cancellationToken);

        return Result<AmbalajBagimsizSandikDto>.Success(AmbalajAlanMaskesi.Maskele(
            AmbalajPlanlamaYardimcisi.BagimsizDtoOlustur(kayit, proje, ustKayit, kaynakSandik), izin));
    }

    private async Task<string?> DogrulaAsync(AmbalajBagimsizSandikKaydetCommand request, bool olcuKorunur)
    {
        if (request.Tur is not (2 or 3 or 4 or 5)) return "Özel sandık türü geçersizdir.";
        if (request.Tur != 3 && request.IcSandikSablonId.HasValue)
            return "Kayıtlı iç sandık tipi yalnız İç Sandık türünde kullanılabilir.";
        var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
        if (proje == null) return "Seçilen proje bulunamadı.";
        if (request.Tur == 2 && proje.ProjeTipiId != (int)ProjeTipi.Normal)
            return "İlave sandık yalnız normal projeye bağlanabilir.";
        if (request.Tur == 4 && proje.ProjeTipiId != (int)ProjeTipi.Saha)
            return "Saha sandığı yalnız saha projesine bağlanabilir.";
        if (request.Tur == 5 && proje.ProjeTipiId != (int)ProjeTipi.Yedek)
            return "Yedek sandığı yalnız yedek projesine bağlanabilir.";
        if (request.Tur is 4 or 5 && request.KaynakSandikId.HasValue)
            return "Saha ve yedek sandıkları mevcut sandıklardan çekilemez; bilgileri manuel girilmelidir.";
        if (request.KaynakSandikId.HasValue)
        {
            if (request.Tur != 2) return "Kaynak sandık yalnız ilave sandık kaydında seçilebilir.";
            var kaynakUygun = _unitOfWork.GetRepository<Sandik>().Queryable()
                .Any(s => s.Id == request.KaynakSandikId.Value && s.ProjeId == request.ProjeId);
            if (!kaynakUygun) return "Seçilen kaynak sandık bu projeye ait değildir.";
            var kaynakKaydi = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .FirstOrDefault(k => k.KaynakKayitId == request.KaynakSandikId.Value && !k.IptalMi);
            if (kaynakKaydi != null && kaynakKaydi.AmbalajaDahil && !kaynakKaydi.BagimsizKayitMi)
                return "İlave sandığa dönüştürülecek kaynak önce ambalaj kapsamı dışında bırakılmalıdır.";
            if (kaynakKaydi != null && kaynakKaydi.BagimsizKayitMi && kaynakKaydi.Id != request.SandikId)
                return "Seçilen sandık daha önce İlave sandık olarak kullanılmıştır.";
        }
        if (request.Tur == 3)
        {
            if (!request.UstKaynakSandikId.HasValue) return "İç sandığın gireceği dış koli seçilmelidir.";
            if (!_unitOfWork.GetRepository<Sandik>().Queryable()
                .Any(s => s.Id == request.UstKaynakSandikId.Value && s.ProjeId == request.ProjeId))
                return "Seçilen dış koli bu projeye ait değildir.";
            if (request.IcSandikSablonId.HasValue && !_unitOfWork.GetRepository<AmbalajIcSandikSablonu>()
                .Queryable().Any(s => s.Id == request.IcSandikSablonId.Value))
                return "Seçilen kayıtlı iç sandık tipi bulunamadı.";
        }
        if (string.IsNullOrWhiteSpace(request.Ad)) return "Sandık adı zorunludur.";
        if (!AmbalajPlanlamaYardimcisi.GecerliSandikTipi(request.SandikTipi)) return "Sandık tipi geçersizdir.";
        if (request.Adet <= 0) return "Adet sıfırdan büyük olmalıdır.";
        if (!olcuKorunur && (request.Boy <= 0 || request.En <= 0 || request.Yukseklik <= 0))
            return "Boy, en ve yükseklik zorunludur.";
        if (string.IsNullOrWhiteSpace(request.TalimatVeren)) return "Talimat veren kişi zorunludur.";
        if (request.TalimatVeren.Trim().Length > 200)
            return "Talimat veren kişi en fazla 200 karakter olabilir.";
        return null;
    }

    private static string SandikNoBelirle(
        AmbalajUretimKaydi kayit,
        AmbalajBagimsizSandikKaydetCommand request,
        AmbalajUretimKaydi? ustKayit,
        IGenericRepository<AmbalajUretimKaydi> repo)
    {
        if (request.Tur == 3 && ustKayit != null && (kayit.Id == 0 || string.IsNullOrWhiteSpace(kayit.SandikNo)))
        {
            var onEk = $"{ustKayit.SandikNo}.";
            var sira = repo.Queryable().Where(k => k.BagimsizKayitMi && k.Tur == AmbalajSandikTuru.Ic &&
                                                   k.UstKayitId == ustKayit.Id && !k.IptalMi)
                .Select(k => k.SandikNo).ToList()
                .Select(no => no.StartsWith(onEk) && int.TryParse(no[onEk.Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0).Max() + 1;
            return $"{onEk}{sira}";
        }
        if (!string.IsNullOrWhiteSpace(request.SandikNo)) return request.SandikNo.Trim();
        if (!string.IsNullOrWhiteSpace(kayit.SandikNo)) return kayit.SandikNo;
        var onEk2 = request.Tur == 2 ? "ILV-" : request.Tur == 3 ? "IC-" : request.Tur == 4 ? "SAH-" : "YDK-";
        var son = repo.Queryable().Where(k => k.BagimsizKayitMi && !k.IptalMi && k.Tur == AmbalajPlanlamaYardimcisi.OzelTurCoz(request.Tur))
            .Select(k => k.SandikNo).ToList()
            .Select(no => no.StartsWith(onEk2) && int.TryParse(no[onEk2.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return $"{onEk2}{son + 1:000}";
    }
}

public sealed class AmbalajBagimsizSandikSilCommandHandler
    : IRequestHandler<AmbalajBagimsizSandikSilCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinansUretimAktarimService _finans;
    private readonly AmbalajKritikIslemDenetimi _denetim;

    public AmbalajBagimsizSandikSilCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IFinansUretimAktarimService finans,
        AmbalajKritikIslemDenetimi denetim)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _finans = finans;
        _denetim = denetim;
    }

    public Task<Result> Handle(AmbalajBagimsizSandikSilCommand request, CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var hedefler = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.Id == request.SandikId).ToList();
            var gereksinim = _denetim.Degerlendir(_denetim.DegisiklikleriHazirla(hedefler,
                degisiklik => degisiklik with { Siliniyor = true }));
            var kapi = await _denetim.KapilariDenetleAsync(gereksinim, request.Gerekce, token);
            if (!kapi.IsSuccess)
                return kapi;

            var sonuc = await SilAsync(request, token);
            if (sonuc.IsSuccess)
                await _denetim.KritikIslemiKaydetAsync(gereksinim, hedefler, nameof(AmbalajBagimsizSandikSilCommand), request.Gerekce, token);
            return sonuc;
        }, cancellationToken);

    private async Task<Result> SilAsync(AmbalajBagimsizSandikSilCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
        var kayit = await repo.GetByIdAsync(request.SandikId);
        if (kayit == null || !kayit.BagimsizKayitMi || kayit.IptalMi)
            return Result.Failure("Sandık bulunamadı.", 404);
        if (!_currentUser.IslemKullaniciId.HasValue) return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);
        var proje = kayit.ProjeId.HasValue
            ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
            : null;
        var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
        kayit.KullaniciAdinaIptalEt(
            _currentUser.IslemKullaniciId.Value, AmbalajUretimYardimcilari.Temizle(request.Gerekce));
        repo.Update(kayit);
        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
            _unitOfWork, kayit, eski, "Bağımsız ambalaj sandığı silindi", _currentUser.IslemKullaniciId.Value);
        await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
            _unitOfWork, _finans, kayit, proje, cancellationToken);
        return Result.Success();
    }
}
