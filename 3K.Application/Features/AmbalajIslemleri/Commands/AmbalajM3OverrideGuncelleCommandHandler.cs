using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajM3OverrideGuncelleCommandHandler
        : IRequestHandler<AmbalajM3OverrideGuncelleCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajM3OverrideGuncelleCommandHandler(
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
            AmbalajM3OverrideGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("İptal edilmiş kaydın m³ değeri değiştirilemez.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);
            if (!request.M3Override.HasValue &&
                kayit.UretimeAlindi &&
                !AmbalajUretimYardimcilari.OlculerGecerli(kayit))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Ölçüleri eksik ve üretime alınmış kayıttan manuel m³ değeri kaldırılamaz.", 409);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.M3Override = request.M3Override;
            kayit.M3OverrideNedeni = request.M3Override.HasValue ? request.Neden.Trim() : null;
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                request.M3Override.HasValue ? "M³ değeri manuel değiştirildi" : "M³ manuel değişikliği kaldırıldı",
                _currentUserService.UserId ?? 0,
                request.Neden);
            var proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, proje, cancellationToken);
            var ust = kayit.UstKayitId.HasValue ? await repo.GetByIdAsync(kayit.UstKayitId.Value) : null;
            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, proje, ust);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto);
        }
    }
}
