using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimKaydiGuncelleCommandHandler
        : IRequestHandler<AmbalajUretimKaydiGuncelleCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimKaydiGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFinansUretimAktarimService finansService,
            IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _finansService = finansService;
            _rolService = rolService;
        }

        public async Task<Result<AmbalajUretimKaydiDto>> Handle(
            AmbalajUretimKaydiGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("İptal edilmiş kayıt düzenlenemez. Önce kaydı aktifleştirin.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            var eksikYetki = await AmbalajYetkilendirmeYardimcisi.EksikYetkiKodunuGetirAsync(
                AmbalajYetkilendirmeYardimcisi.GuncellemeEkYetkiKodlariniBelirle(kayit, request),
                _rolService,
                _currentUserService,
                cancellationToken);
            if (eksikYetki != null)
                return Result<AmbalajUretimKaydiDto>.Failure(
                    $"Bu alan değişikliği için gerekli yetkiniz bulunmuyor ({eksikYetki}).", 403);

            var aktifIcSandikVar = repo.Queryable().Any(k => k.UstKayitId == kayit.Id && !k.IptalMi);
            var projeBaglantisiDegisiyor = kayit.ProjeId != request.ProjeId ||
                                          !string.Equals(kayit.ManuelProjeNo, request.ManuelProjeNo?.Trim(), StringComparison.OrdinalIgnoreCase);
            if (aktifIcSandikVar && (request.Tur == AmbalajSandikTuru.Ic || projeBaglantisiDegisiyor))
            {
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Aktif iç sandıkları bulunan kaydın türü iç sandık yapılamaz veya proje bağlantısı değiştirilemez.",
                    409);
            }

            var baglanti = await AmbalajKomutYardimcisi.BaglantilariDogrulaAsync(_unitOfWork, request, kayit.Id);
            if (baglanti.Hata != null)
                return Result<AmbalajUretimKaydiDto>.Failure(baglanti.Hata, baglanti.HataKodu);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            AmbalajKomutYardimcisi.OrtakAlanlariUygula(kayit, request);
            kayit.UretimTarihi = request.UretimTarihi ??
                                  (kayit.UretimeAlindi ? kayit.UretimTarihi ?? TurkeyTime.Now : null);
            if (kayit.KaynakModul is not (AmbalajKaynakModulu.Manuel or AmbalajKaynakModulu.Diger))
                kayit.KaynakSenkronizasyonuKilitliMi = true;

            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Ambalaj üretim kaydı güncellendi",
                _currentUserService.UserId ?? 0,
                request.Aciklama);
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, baglanti.Proje, cancellationToken);

            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, baglanti.Proje, baglanti.UstKayit);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto);
        }
    }
}
