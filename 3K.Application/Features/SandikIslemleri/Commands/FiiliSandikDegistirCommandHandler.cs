using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 4: Sandık değişikliği handler
    /// UML Sequence Diagram: SandikService → UrunService → RevizyonService
    /// </summary>
    public class FiiliSandikDegistirCommandHandler : IRequestHandler<FiiliSandikDegistirCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public FiiliSandikDegistirCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(FiiliSandikDegistirCommand request, CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var revizyonRepo = _unitOfWork.GetRepository<Revizyon>();

            // 1. Ürünü getir
            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return false;

            string eskiSandikNo = urun.FiiliSandikNo ?? urun.CekideGecenSandikNo;

            // 2. Hedef sandığı bul veya oluştur
            var hedefSandiklar = await sandikRepo.FindAsync(s =>
                s.ProjeId == request.ProjeId && s.SandikNo == request.YeniFiiliSandikNo);
            var hedefSandik = hedefSandiklar.FirstOrDefault();

            if (hedefSandik == null)
            {
                hedefSandik = new Sandik
                {
                    ProjeId = request.ProjeId,
                    SandikNo = request.YeniFiiliSandikNo,
                    Durum = SandikDurum.Hazirlaniyor
                };
                await sandikRepo.AddAsync(hedefSandik);
                await _unitOfWork.SaveChangesAsync();
            }

            // 3. Eski sandıktan SandikIcerik kaydını kaldır
            var eskiIcerikler = await sandikIcerikRepo.FindAsync(si =>
                si.CekiSatiriId == request.CekiSatiriId);
            var eskiIcerik = eskiIcerikler.FirstOrDefault();

            int konulanAdet = eskiIcerik?.KonulanAdet ?? urun.IstenenAdet;
            int eksikAdet = eskiIcerik?.EksikAdet ?? 0;

            if (eskiIcerik != null)
            {
                sandikIcerikRepo.Remove(eskiIcerik);
            }

            // 4. Yeni sandığa SandikIcerik ekle
            var yeniIcerik = new SandikIcerik
            {
                SandikId = hedefSandik.Id,
                CekiSatiriId = request.CekiSatiriId,
                KonulanAdet = konulanAdet,
                EksikAdet = eksikAdet
            };
            await sandikIcerikRepo.AddAsync(yeniIcerik);

            // 5. Çekide geçen sandık DEĞİŞMEZ; fiili sandık güncellenir
            urun.FiiliSandikNo = request.YeniFiiliSandikNo;
            cekiSatiriRepo.Update(urun);

            // 6. Revizyon kaydı oluştur
            var revizyon = new Revizyon
            {
                ProjeId = request.ProjeId,
                KullaniciId = request.KullaniciId,
                Tip = "Sandık Değişikliği",
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = $"{eskiSandikNo} no'lu sandıktan {request.YeniFiiliSandikNo} no'lu sandığa alındı"
            };
            await revizyonRepo.AddAsync(revizyon);

            // 7. Otomatik açıklama notu
            var otomatikNot = $"{eskiSandikNo} no'lu sandıktan {request.YeniFiiliSandikNo} no'lu sandığa alındı";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks) ? otomatikNot : $"{urun.Remarks}; {otomatikNot}";
            cekiSatiriRepo.Update(urun);

            await _unitOfWork.SaveChangesAsync();

            // 8. Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Fiili Sandık Değiştirildi",
                KullaniciId = request.KullaniciId,
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = otomatikNot
            });

            return true;
        }
    }
}