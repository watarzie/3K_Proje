using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class StoktanKarsilaCommandHandler : IRequestHandler<StoktanKarsilaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public StoktanKarsilaCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(StoktanKarsilaCommand request, CancellationToken cancellationToken)
        {
            var urunRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var stokRepo = _unitOfWork.GetRepository<StokKaydi>();
            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await urunRepo.GetByIdAsync(request.CekiSatiriId);
            var stok = await stokRepo.GetByIdAsync(request.StokKaydiId);

            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);
            if (stok == null) return Result.Failure("Stok kaydı bulunamadı.", 404);
            if (stok.Miktar < request.KarsilananAdet)
                return Result.Failure("Yeterli stok miktarı bulunmuyor!", 400);

            stok.Miktar -= request.KarsilananAdet;
            if (stok.Miktar == 0) stok.DurumId = (int)StokDurum.Tukendi;
            stokRepo.Update(stok);

            urun.DurumId = (int)UrunDurum.StoktanKarsilandi;
            urun.Remarks = $"Stoktan karşılandı ({request.KarsilananAdet} {urun.Birim})";
            urunRepo.Update(urun);

            var icerik = (await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == urun.Id)).FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.KarsilananAdet);
                icerik.KonulanAdet += request.KarsilananAdet;
                sandikIcerikRepo.Update(icerik);
            }

            await stokHareketRepo.AddAsync(new StokHareketi
            {
                StokKaydiId = stok.Id,
                KullaniciId = request.KullaniciId,
                Miktar = request.KarsilananAdet * -1,
                IslemTipiId = (int)IslemTipi.StoktanKarsilandi,
                CekiSatiriId = urun.Id,
                ProjeId = request.ProjeId,
                Tarih = DateTime.UtcNow,
                Aciklama = $"Projede ({request.ProjeId}) sipariş kapatıldı."
            });

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Stoktan Karşılandı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Zimmetlenen stok id: {stok.Id}, Miktar: {request.KarsilananAdet}"
            });

            return Result.Success();
        }
    }
}
