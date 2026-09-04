using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimKaydiAktiflestirCommandHandler
        : IRequestHandler<AmbalajUretimKaydiAktiflestirCommand, Result<AmbalajUretimKaydiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajUretimKaydiAktiflestirCommandHandler(
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
            AmbalajUretimKaydiAktiflestirCommand request,
            CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            if (!kayit.IptalMi)
                return Result<AmbalajUretimKaydiDto>.Failure("Kayıt zaten aktiftir.", 409);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(
                    kayit, _rolService, _currentUserService, cancellationToken))
                return Result<AmbalajUretimKaydiDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            AmbalajUretimKaydi? ust = null;
            if (kayit.UstKayitId.HasValue)
            {
                ust = await repo.GetByIdAsync(kayit.UstKayitId.Value);
                if (ust == null || ust.IptalMi)
                    return Result<AmbalajUretimKaydiDto>.Failure("Üst sandık aktif olmadan iç sandık aktifleştirilemez.", 409);
            }

            var ayniSandikVar = repo.Queryable().Any(k =>
                k.Id != kayit.Id && !k.IptalMi &&
                k.SandikNo.ToUpper() == kayit.SandikNo.ToUpper() &&
                (kayit.ProjeId.HasValue
                    ? k.ProjeId == kayit.ProjeId
                    : k.ProjeId == null && k.ManuelProjeNo != null && kayit.ManuelProjeNo != null &&
                      k.ManuelProjeNo.ToUpper() == kayit.ManuelProjeNo.ToUpper()));
            if (ayniSandikVar)
                return Result<AmbalajUretimKaydiDto>.Failure("Aynı proje ve sandık numarasıyla başka bir aktif kayıt var.", 409);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            kayit.IptalMi = false;
            kayit.IptalTarihi = null;
            kayit.IptalEdenKullaniciId = null;
            kayit.IptalNedeni = null;
            kayit.UretimDurumu = kayit.IptalOncesiUretimDurumu ?? AmbalajUretimDurumu.Planlandi;
            kayit.IptalOncesiUretimDurumu = null;
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                _unitOfWork,
                kayit,
                eski,
                "Ambalaj üretim kaydı yeniden aktifleştirildi",
                _currentUserService.UserId ?? 0,
                request.Aciklama);
            var proje = kayit.ProjeId.HasValue
                ? await _unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value)
                : null;
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(
                _unitOfWork, _finansService, kayit, proje, cancellationToken);
            var dto = AmbalajKomutYardimcisi.DtoOlustur(kayit, proje, ust);
            await AmbalajYetkilendirmeYardimcisi.DtoyuYetkiyeGoreMaskeleAsync(
                dto, _rolService, _currentUserService, cancellationToken);
            return Result<AmbalajUretimKaydiDto>.Success(dto);
        }
    }
}
