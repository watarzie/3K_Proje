using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UrunGuncelleCommandHandler : IRequestHandler<UrunGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public UrunGuncelleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(UrunGuncelleCommand request, CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

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

            if (request.KonulanAdet.HasValue) icerik.KonulanAdet = request.KonulanAdet.Value;
            if (request.EksikAdet.HasValue) icerik.EksikAdet = request.EksikAdet.Value;
            sandikIcerikRepo.Update(icerik);

            if (request.PaketleyenId.HasValue) urun.PaketleyenId = request.PaketleyenId.Value;
            if (request.KontrolEdenId.HasValue) urun.KontrolEdenId = request.KontrolEdenId.Value;

            if (!string.IsNullOrEmpty(request.GridDurumu)) urun.GridDurumu = request.GridDurumu;
            if (!string.IsNullOrEmpty(request.UcKDurumu)) urun.UcKDurumu = request.UcKDurumu;

            if (!string.IsNullOrEmpty(request.Aciklama)) urun.Remarks = request.Aciklama;

            // Durum hesaplama (State Diagram) — string karşılaştırma
            if (urun.UcKDurumu == "TamGeldi" || icerik.KonulanAdet >= urun.IstenenAdet)
                urun.Durum = "Tamamlandi";
            else if (urun.UcKDurumu == "EksikGeldi" || icerik.EksikAdet > 0)
                urun.Durum = "Eksik";
            else if (urun.GridDurumu == "Geldi" || (icerik.KonulanAdet > 0 && icerik.KonulanAdet < urun.IstenenAdet))
                urun.Durum = "KismiGeldi";

            cekiSatiriRepo.Update(urun);

            // Sandık durumu ve lokasyon otomasyonu
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik != null)
            {
                if (!string.IsNullOrEmpty(request.UcKDurumu) && request.UcKDurumu != "Bekliyor")
                    sandik.DepoLokasyonu = "UcK";
                else if (!string.IsNullOrEmpty(request.GridDurumu) && request.GridDurumu != "Bekliyor")
                    sandik.DepoLokasyonu = "Grid";

                var tumIcerikler = await sandikIcerikRepo.FindAsync(si => si.SandikId == request.SandikId);
                var tumUrunIds = tumIcerikler.Select(si => si.CekiSatiriId).ToList();

                bool hepsiTamamlandi = true;
                bool enAzBiriKonuldu = false;
                foreach (var urunId in tumUrunIds)
                {
                    var u = await cekiSatiriRepo.GetByIdAsync(urunId);
                    if (u != null)
                    {
                        if (u.Durum != "Tamamlandi") hepsiTamamlandi = false;
                        var uIcerik = tumIcerikler.FirstOrDefault(si => si.CekiSatiriId == urunId);
                        if (uIcerik != null && uIcerik.KonulanAdet > 0) enAzBiriKonuldu = true;
                    }
                }

                if (hepsiTamamlandi && tumUrunIds.Count > 0) sandik.Durum = "Hazir";
                else if (enAzBiriKonuldu) sandik.Durum = "Hazirlaniyor";
                else sandik.Durum = "Bos";

                sandikRepo.Update(sandik);
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Ürün Güncellendi",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Durum: {urun.Durum}, Grid: {urun.GridDurumu}, 3K: {urun.UcKDurumu}, Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}"
            });

            return Result.Success();
        }
    }
}
