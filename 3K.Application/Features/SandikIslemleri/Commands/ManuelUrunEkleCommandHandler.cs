using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class ManuelUrunEkleCommandHandler : IRequestHandler<ManuelUrunEkleCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public ManuelUrunEkleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(ManuelUrunEkleCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId) return false;

            // En güncel Ceki'yi bul
            var sonCeki = (await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId))
                          .OrderByDescending(c => c.YuklemeTarihi)
                          .FirstOrDefault();

            if (sonCeki == null) return false;

            // CekiSatiri oluştur
            var yeniUrun = new CekiSatiri
            {
                CekiId = sonCeki.Id,
                SiraNo = 9999, // Manuel eklenenler için ayırt edici yüksek bir sıra numarası kullanılabilir
                BarkodNo = request.BarkodNo,
                Aciklama = request.Aciklama,
                IstenenAdet = request.IstenenAdet,
                Birim = request.Birim,
                CekideGecenSandikNo = sandik.SandikNo,
                FiiliSandikNo = sandik.SandikNo,
                Durum = UrunDurum.Bekliyor,
                IsManuelEklenen = true,
                EklemeNedeni = request.EklemeNedeni,
                GridDurumu = GridDurum.Bekliyor,
                UcKDurumu = UcKDurum.Bekliyor
            };

            await cekiSatiriRepo.AddAsync(yeniUrun);
            await _unitOfWork.SaveChangesAsync(); // Id almak için kaydediyoruz

            // Sandık İçerik kaydı
            var icerik = new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = yeniUrun.Id,
                KonulanAdet = request.IstenenAdet, // Manuel eklendiğine göre doğrudan fiziksel geldi diye farz edilebilir
                EksikAdet = 0
            };
            await sandikIcerikRepo.AddAsync(icerik);
            await _unitOfWork.SaveChangesAsync();

            // Hareket log
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = yeniUrun.Id.ToString(),
                Islem = "Manuel Ürün Eklendi",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Neden: {request.EklemeNedeni}, Miktar: {request.IstenenAdet} {request.Birim}"
            });

            return true;
        }
    }
}
