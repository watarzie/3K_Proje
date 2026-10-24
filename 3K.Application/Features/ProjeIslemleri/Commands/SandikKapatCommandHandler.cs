using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class SandikKapatCommandHandler : IRequestHandler<SandikKapatCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikKapatCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(SandikKapatCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result<bool>.Failure("Sandık bulunamadı.", 404);

            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return Result<bool>.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);
            
            var eskiDurumId = sandik.DurumId;

            int yeniDurumId = request.Kapali ? (int)SandikDurum.Kapandi : (int)SandikDurum.Hazirlaniyor;
            
            if (sandik.DurumId != yeniDurumId)
            {
                // Kapalı sandığı geri almak sebep ister; reddedilen istekte sandık değişmez.
                LookupSandikAcilmaSebebi? sebep = null;
                if (SandikDurumOlayKurali.OlayTuru(eskiDurumId, yeniDurumId) == SandikDurumOlayTuru.Acildi)
                {
                    var hata = SandikDurumOlayKurali.GeriAcmaHatasi(request.AcilmaSebebiId, request.Not);
                    if (hata != null)
                        return Result<bool>.Failure(hata);
                    sebep = await _unitOfWork.GetRepository<LookupSandikAcilmaSebebi>().GetByIdAsync(request.AcilmaSebebiId!.Value);
                    if (sebep == null)
                        return Result<bool>.Failure("Seçilen geri açma sebebi bulunamadı.");
                }

                sandik.DurumId = yeniDurumId;
                repo.Update(sandik);
                await SandikDurumOlayYazici.EkleAsync(_unitOfWork, sandik, eskiDurumId, SandikDurumOlayKaynagi.Elle,
                    _currentUserService.UserId, sebep?.Id, request.Not);
                await _unitOfWork.SaveChangesAsync();

                var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? eskiDurumId.ToString();
                var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), yeniDurumId) ?? yeniDurumId.ToString();
                var islemMetni = request.Kapali ? "Sandık Manuel Kapatma" : "Sandık Geri Açıldı";

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = sandik.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = islemMetni,
                    IslemTipiId = request.Kapali ? (int)IslemTipi.SandikKapatildi : null,
                    EskiDeger = eskiDurumMetni,
                    YeniDeger = yeniDurumMetni,
                    Aciklama = request.Kapali
                        ? $"Sandık yetkili tarafından manuel olarak kapatıldı."
                        : GeriAcmaAciklamasi(sebep, request.Not)
                });
            }

            return Result<bool>.Success(true);
        }

        private static string GeriAcmaAciklamasi(LookupSandikAcilmaSebebi? sebep, string? not)
        {
            var metin = "Sandık yetkili tarafından tekrar hazırlanıyor konumuna getirildi.";
            if (sebep != null)
                metin += $" Sebep: {sebep.Deger}.";
            var temizNot = SandikDurumOlayKurali.NotNormalize(not);
            return temizNot == null ? metin : $"{metin} Not: {temizNot}";
        }
    }
}
