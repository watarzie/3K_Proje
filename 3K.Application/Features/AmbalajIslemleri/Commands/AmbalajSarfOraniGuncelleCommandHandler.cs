using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajSarfOraniGuncelleCommandHandler
        : IRequestHandler<AmbalajSarfOraniGuncelleCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRolService _rolService;

        public AmbalajSarfOraniGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _rolService = rolService;
        }

        public async Task<Result<AmbalajUretimKaydiDto>> Handle(
            AmbalajSarfOraniGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("İptal edilmiş kaydın sarf oranı değiştirilemez.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.SarfOrani = request.SarfOrani;
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Sarf oranı değiştirildi",
                _currentUserService.UserId ?? 0,
                request.Neden);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            var ust = kayit.UstKayitId.HasValue ? await repo.GetByIdAsync(kayit.UstKayitId.Value) : null;
            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, proje, ust);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto);
        }
    }
}
