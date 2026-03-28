using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// İş akışı 6: Stoktan eksik karşılama handler
    /// Sequence: StokYeterliMi → StokDüş → EksikMiktarAzalt → HareketKaydet
    /// </summary>
    public class StokKarsilaCommandHandler : IRequestHandler<StokKarsilaCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStokService _stokService;
        private readonly IHareketService _hareketService;

        public StokKarsilaCommandHandler(IUnitOfWork unitOfWork, IStokService stokService, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _stokService = stokService;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(StokKarsilaCommand request, CancellationToken cancellationToken)
        {
            // 1. Stok yeterli mi kontrol et
            var yeterli = await _stokService.StokYeterliMi(request.StokKaydiId, request.Miktar);
            if (!yeterli)
                throw new InvalidOperationException("Stok miktarı yetersiz.");

            // 2. Stok düş
            await _stokService.StokDusAsync(request.StokKaydiId, request.Miktar);

            // 3. Ürünün eksik miktarını azalt
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return false;

            // SandikIcerik'teki eksik adetini azalt
            var icerikler = await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == request.CekiSatiriId);
            var icerik = icerikler.FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.Miktar);
                icerik.KonulanAdet += request.Miktar;
                sandikIcerikRepo.Update(icerik);
            }

            // Durum güncelle
            urun.Durum = UrunDurum.StoktanKarsilandi;
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks)
                ? "Kalan stoktan karşılandı"
                : $"{urun.Remarks}; Kalan stoktan karşılandı";
            cekiSatiriRepo.Update(urun);

            // 4. Stok kullanım hareketi oluştur
            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var stokHareketi = new StokHareketi
            {
                StokKaydiId = request.StokKaydiId,
                CekiSatiriId = request.CekiSatiriId,
                ProjeId = request.ProjeId,
                KullaniciId = request.KullaniciId,
                Miktar = request.Miktar,
                IslemTipi = "StokKullanimi",
                Aciklama = $"Proje {request.ProjeId} için stoktan {request.Miktar} adet kullanıldı"
            };
            await stokHareketRepo.AddAsync(stokHareketi);

            await _unitOfWork.SaveChangesAsync();

            // 5. Hareket geçmişi kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "StokHareketi",
                ReferansId = stokHareketi.Id.ToString(),
                Islem = "Stoktan Karşılandı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"StokKaydı: {request.StokKaydiId}, Miktar: {request.Miktar}"
            });

            return true;
        }
    }
}
