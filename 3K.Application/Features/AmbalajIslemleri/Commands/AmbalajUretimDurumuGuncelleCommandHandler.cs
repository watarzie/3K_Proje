using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimDurumuGuncelleCommandHandler
        : IRequestHandler<AmbalajUretimDurumuGuncelleCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimDurumuGuncelleCommandHandler(
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
            AmbalajUretimDurumuGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("İptal edilmiş kaydın üretim durumu değiştirilemez.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            if (request.Durum != AmbalajUretimDurumu.Planlandi)
            {
                if (!kayit.AmbalajaDahil || !kayit.UretimeAlindi)
                    return Result<AmbalajUretimKaydiDto>.Failure("Yalnız ambalaja dahil edilip üretime alınan kayıt ilerletilebilir.", 409);
                if (!AmbalajUretimYardimcilari.UretimMiktariGecerli(kayit))
                    return Result<AmbalajUretimKaydiDto>.Failure("Ölçüleri ve manuel m³ değeri olmayan kayıt üretime alınamaz.", 409);
            }

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.UretimDurumu = request.Durum;
            kayit.FirinPartiNo = AmbalajUretimYardimcilari.Temizle(request.FirinPartiNo) ?? kayit.FirinPartiNo;
            if (request.UretimTarihi.HasValue)
                kayit.UretimTarihi = request.UretimTarihi;

            if (request.Durum == AmbalajUretimDurumu.Tamamlandi)
            {
                kayit.UretimTarihi ??= TurkeyTime.Now;
                kayit.TamamlanmaTarihi ??= TurkeyTime.Now;
            }
            else
            {
                kayit.TamamlanmaTarihi = null;
            }

            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Ambalaj üretim durumu güncellendi",
                _currentUserService.UserId ?? 0,
                request.Aciklama);
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
