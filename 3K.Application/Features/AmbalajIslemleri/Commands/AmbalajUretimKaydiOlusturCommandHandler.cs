using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Helpers;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimKaydiOlusturCommandHandler
        : IRequestHandler<AmbalajUretimKaydiOlusturCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimKaydiOlusturCommandHandler(
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
            AmbalajUretimKaydiOlusturCommand request,
            CancellationToken cancellationToken)
        {
            var baglanti = await AmbalajKomutYardimcisi.BaglantilariDogrulaAsync(_unitOfWork, request);
            if (baglanti.Hata != null)
                return Result<AmbalajUretimKaydiDto>.Failure(baglanti.Hata, baglanti.HataKodu);

            var kayit = new AmbalajUretimKaydi
            {
                CreatedBy = _currentUserService.UserId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                KaynakModul = request.KaynakModul,
                KaynakKayitId = null,
                AmbalajaDahil = request.AmbalajaDahil,
                UretimeAlindi = request.UretimeAlindi,
                SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
                UretimDurumu = AmbalajUretimDurumu.Planlandi,
                UretimTarihi = request.UretimTarihi ?? (request.UretimeAlindi ? TurkeyTime.Now : null)
            };
            AmbalajKomutYardimcisi.OrtakAlanlariUygula(kayit, request);

            await _unitOfWork.GetRepository<AmbalajUretimKaydi>().AddAsync(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                null,
                "Ambalaj üretim kaydı oluşturuldu",
                _currentUserService.UserId ?? 0,
                request.Aciklama);
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, baglanti.Proje, cancellationToken);

            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, baglanti.Proje, baglanti.UstKayit);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto, 201);
        }
    }
}
