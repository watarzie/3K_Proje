using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class FBDenKarsilaCommandHandler : IRequestHandler<FBDenKarsilaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;

        public FBDenKarsilaCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
        }

        public async Task<Result> Handle(FBDenKarsilaCommand request, CancellationToken cancellationToken)
        {
            var urunRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var fbTransferRepo = _unitOfWork.GetRepository<FBTransfer>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            // ===== 1. Hedef ürünü bul =====
            var urun = await urunRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            // ===== 2. Kaynak projeyi bul ve stoğunu düş =====
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();

            var kaynakProjeler = await projeRepo.FindAsync(p => p.ProjeNo == request.AlinanFB);
            var kaynakProje = kaynakProjeler.FirstOrDefault();

            if (kaynakProje != null)
            {
                // Kaynak projede aynı barkodlu ürünü bul
                var kaynakCekiler = await cekiRepo.FindAsync(c => c.ProjeId == kaynakProje.Id);
                var kaynakCekiIdler = kaynakCekiler.Select(c => c.Id).ToList();

                var kaynakSatirlar = await urunRepo.FindAsync(cs =>
                    kaynakCekiIdler.Contains(cs.CekiId) && cs.BarkodNo == urun.BarkodNo);
                var kaynakSatir = kaynakSatirlar.FirstOrDefault();

                if (kaynakSatir != null)
                {
                    // ===== İŞ KURALI 6: Kaynak projenin stoğunu düş =====
                    kaynakSatir.GelenMiktar = Math.Max(0, kaynakSatir.GelenMiktar - request.KarsilananAdet);
                    kaynakSatir.UcKDurumu = StatusConstants.UcKDurum.EksikGeldi;
                    kaynakSatir.UcKKarsilamaTipi = StatusConstants.UcKDurum.BaskaProyeVerildi;
                    kaynakSatir.KaynakHedefProjeNo = request.AsilFB;
                    kaynakSatir.Durum = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumu, kaynakSatir.UcKDurumu);
                    urunRepo.Update(kaynakSatir);
                }

                // Hedef ürüne KaynakProjeId kaydet (Grid görsün diye)
                urun.KaynakProjeId = kaynakProje.Id;

                // Transfer kaydı
                var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
                await transferRepo.AddAsync(new ProjeTransfer
                {
                    KaynakProjeId = kaynakProje.Id,
                    HedefProjeId = request.ProjeId,
                    KaynakCekiSatiriId = kaynakSatir?.Id ?? 0,
                    HedefCekiSatiriId = urun.Id,
                    BarkodNo = urun.BarkodNo,
                    Miktar = request.KarsilananAdet,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    Aciklama = request.Aciklama,
                    Tarih = DateTime.UtcNow
                });
            }

            // ===== 3. FB Transfer kaydı =====
            await fbTransferRepo.AddAsync(new FBTransfer
            {
                CekiSatiriId = urun.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                AsilFB = request.AsilFB,
                AlinanFB = request.AlinanFB,
                Miktar = request.KarsilananAdet,
                Aciklama = request.Aciklama,
                IadeDurumu = request.IadeDurumu,
                Tarih = DateTime.UtcNow
            });

            // ===== 4. Hedef ürün kümülatif güncelle =====
            urun.KarsilananMiktar += request.KarsilananAdet;
            urun.Durum = StatusConstants.UrunDurum.FBdenKarsilandi;
            urun.Remarks = $"FB Transfer ({request.AlinanFB}) - {request.KarsilananAdet} adet";
            urunRepo.Update(urun);

            // ===== 5. Hedef sandık içerik güncelle =====
            var icerik = (await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == urun.Id)).FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.KarsilananAdet);
                icerik.KonulanAdet += request.KarsilananAdet;
                sandikIcerikRepo.Update(icerik);
            }

            await _unitOfWork.SaveChangesAsync();

            // ===== 6. Hareket kaydı =====
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "FB'den Karşılandı",
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = $"Asıl: {request.AsilFB}, Alınan: {request.AlinanFB}, Adet: {request.KarsilananAdet}"
            });

            return Result.Success();
        }
    }
}
