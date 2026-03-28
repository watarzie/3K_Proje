using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 3, 7, 8: Ürün güncelleme handler
    /// Konulan adet, eksik adet, paketleyen, kontrol eden, açıklama güncellenir.
    /// Durum otomatik hesaplanır. Hareket kaydı oluşturulur.
    /// </summary>
    public class UrunGuncelleCommandHandler : IRequestHandler<UrunGuncelleCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public UrunGuncelleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(UrunGuncelleCommand request, CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return false;

            // SandikIcerik kaydını bul veya oluştur
            var icerikler = await sandikIcerikRepo.FindAsync(si =>
                si.CekiSatiriId == request.CekiSatiriId && si.SandikId == request.SandikId);
            var icerik = icerikler.FirstOrDefault();

            if (icerik == null)
            {
                icerik = new SandikIcerik
                {
                    SandikId = request.SandikId,
                    CekiSatiriId = request.CekiSatiriId,
                    KonulanAdet = 0,
                    EksikAdet = 0
                };
                await sandikIcerikRepo.AddAsync(icerik);
            }

            // Konulan adet güncelle
            if (request.KonulanAdet.HasValue)
            {
                icerik.KonulanAdet = request.KonulanAdet.Value;
            }

            // Eksik adet güncelle
            if (request.EksikAdet.HasValue)
            {
                icerik.EksikAdet = request.EksikAdet.Value;
            }

            sandikIcerikRepo.Update(icerik);

            // Paketleyen güncelle (İş akışı 7)
            if (request.PaketleyenId.HasValue)
            {
                urun.PaketleyenId = request.PaketleyenId.Value;
            }

            // Kontrol eden güncelle (İş akışı 7)
            if (request.KontrolEdenId.HasValue)
            {
                urun.KontrolEdenId = request.KontrolEdenId.Value;
            }

            // Açıklama güncelle (İş akışı 8)
            if (!string.IsNullOrEmpty(request.Aciklama))
            {
                urun.Remarks = request.Aciklama;
            }

            // Durum otomatik hesaplama (State Diagram)
            if (icerik.KonulanAdet >= urun.IstenenAdet)
            {
                urun.Durum = UrunDurum.Tamamlandi;
            }
            else if (icerik.KonulanAdet > 0 && icerik.KonulanAdet < urun.IstenenAdet)
            {
                urun.Durum = UrunDurum.KismiGeldi;
            }
            else if (icerik.EksikAdet > 0)
            {
                urun.Durum = UrunDurum.Eksik;
            }

            cekiSatiriRepo.Update(urun);

            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Ürün Güncellendi",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}, Durum: {urun.Durum}"
            });

            return true;
        }
    }
}
