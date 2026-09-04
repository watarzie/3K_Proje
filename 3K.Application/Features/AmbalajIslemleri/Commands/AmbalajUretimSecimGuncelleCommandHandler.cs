using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimSecimGuncelleCommandHandler
        : IRequestHandler<AmbalajUretimSecimGuncelleCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimSecimGuncelleCommandHandler(
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
            AmbalajUretimSecimGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("İptal edilmiş kaydın seçimi değiştirilemez.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            var eksikYetki = await AmbalajYetkilendirmeYardimcisi.EksikYetkiKodunuGetirAsync(
                AmbalajYetkilendirmeYardimcisi.SecimGecisYetkiKodlariniBelirle(
                    kayit, request.AmbalajaDahil, request.UretimeAlindi),
                _rolService,
                _currentUserService,
                cancellationToken);
            if (eksikYetki != null)
                return Result<AmbalajUretimKaydiDto>.Failure(
                    $"Bu seçim geçişi için gerekli yetkiniz bulunmuyor ({eksikYetki}).", 403);
            if (request.UretimeAlindi && !AmbalajUretimYardimcilari.UretimMiktariGecerli(kayit))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Ölçüleri ve manuel m³ değeri olmayan sandık üretime alınamaz.", 409);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.AmbalajaDahil = request.AmbalajaDahil;
            kayit.UretimeAlindi = request.UretimeAlindi;
            if (request.UretimeAlindi)
                kayit.UretimTarihi ??= TurkeyTime.Now;
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Ambalaj ve üretim seçimi güncellendi",
                _currentUserService.UserId ?? 0,
                request.Aciklama);
            var proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, proje, cancellationToken);
            var ustKayit = kayit.UstKayitId.HasValue
                ? await repo.GetByIdAsync(kayit.UstKayitId.Value)
                : null;
            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, proje, ustKayit);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto);
        }
    }
}
