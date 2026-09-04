using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimKaydiIptalEtCommandHandler
        : IRequestHandler<AmbalajUretimKaydiIptalEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimKaydiIptalEtCommandHandler(
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

        public async Task<Result> Handle(AmbalajUretimKaydiIptalEtCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result.Failure("Kayıt zaten iptal edilmiş.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            var aktifIcSandikVar = repo.Queryable().Any(k => k.UstKayitId == kayit.Id && !k.IptalMi);
            if (aktifIcSandikVar)
                return Result.Failure("Bu kayda bağlı aktif iç sandıklar varken iptal işlemi yapılamaz.", 409);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.IptalOncesiUretimDurumu = kayit.UretimDurumu;
            kayit.IptalMi = true;
            kayit.IptalTarihi = TurkeyTime.Now;
            kayit.IptalEdenKullaniciId = _currentUserService.UserId;
            kayit.IptalNedeni = request.Neden.Trim();
            // İptal, kullanıcının üretime alma seçimini silmemelidir. Kaydın aktifliği
            // IptalMi üzerinden değerlendirilir; yeniden aktifleştirildiğinde önceki seçim
            // ve buna bağlı finans senkronizasyonu tutarlı biçimde geri gelir.
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Ambalaj üretim kaydı iptal edildi",
                _currentUserService.UserId ?? 0,
                request.Neden);
            var proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, proje, cancellationToken);
            return Result.Success();
        }
    }
}
