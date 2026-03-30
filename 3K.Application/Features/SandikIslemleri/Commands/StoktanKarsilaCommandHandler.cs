using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class StoktanKarsilaCommandHandler : IRequestHandler<StoktanKarsilaCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public StoktanKarsilaCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(StoktanKarsilaCommand request, CancellationToken cancellationToken)
        {
            var urunRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var stokRepo = _unitOfWork.GetRepository<StokKaydi>();
            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await urunRepo.GetByIdAsync(request.CekiSatiriId);
            var stok = await stokRepo.GetByIdAsync(request.StokKaydiId);

            if (urun == null || stok == null) return false;

            if (stok.Miktar < request.KarsilananAdet)
                throw new Exception("Yeterli stok miktarı bulunmuyor!");

            // Stok düşümü yap
            stok.Miktar -= request.KarsilananAdet;
            if (stok.Miktar == 0) stok.Durum = StokDurum.Tukendi;
            stokRepo.Update(stok);

            // Ürün durumunu güncelle
            urun.Durum = UrunDurum.StoktanKarsilandi;
            urun.Remarks = $"Stoktan karşılandı ({request.KarsilananAdet} {urun.Birim})";
            urunRepo.Update(urun);

            // Sandık İçerik kaydındaki eksik adeti kapat/güncelle
            var icerik = (await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == urun.Id)).FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.KarsilananAdet);
                icerik.KonulanAdet += request.KarsilananAdet;
                sandikIcerikRepo.Update(icerik);
            }

            // Stok hareketi kaydet (StokModülü kullanımı)
            await stokHareketRepo.AddAsync(new StokHareketi
            {
                StokKaydiId = stok.Id,
                KullaniciId = request.KullaniciId,
                Miktar = request.KarsilananAdet * -1,
                IslemTipi = "KULLANIM",
                CekiSatiriId = urun.Id,
                ProjeId = request.ProjeId,
                Tarih = DateTime.UtcNow,
                Aciklama = $"Projede ({request.ProjeId}) sipariş kapatıldı."
            });

            await _unitOfWork.SaveChangesAsync();

            // Genel sistem tarihçesine (Log) kaydet
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Stoktan Karşılandı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Zimmetlenen stok id: {stok.Id}, Miktar: {request.KarsilananAdet}"
            });

            return true;
        }
    }
}
