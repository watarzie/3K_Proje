using MediatR;
using _3K.Core.Enums;
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

            if (request.GridDurumuId.HasValue) urun.GridDurumuId = request.GridDurumuId.Value;
            if (request.UcKDurumuId.HasValue) urun.UcKDurumuId = request.UcKDurumuId.Value;

            if (!string.IsNullOrEmpty(request.Aciklama)) urun.Remarks = request.Aciklama;

            // Durum hesaplama (State Diagram) — int karşılaştırma
            if (urun.UcKDurumuId == (int)UcKDurum.TamGeldi || icerik.KonulanAdet >= urun.IstenenAdet)
                urun.DurumId = (int)UrunDurum.Tamamlandi;
            else if (urun.UcKDurumuId == (int)UcKDurum.EksikGeldi || icerik.EksikAdet > 0)
                urun.DurumId = (int)UrunDurum.Eksik;
            else if (urun.GridDurumuId == (int)GridDurum.TamGeldi || (icerik.KonulanAdet > 0 && icerik.KonulanAdet < urun.IstenenAdet))
                urun.DurumId = (int)UrunDurum.KismiGeldi;

            cekiSatiriRepo.Update(urun);

            // Sandık durumu ve lokasyon otomasyonu
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik != null)
            {
                if (request.UcKDurumuId.HasValue && request.UcKDurumuId.Value != (int)UcKDurum.Bekliyor)
                    sandik.DepoLokasyonId = (int)DepoLokasyon.UcK;
                else if (request.GridDurumuId.HasValue && request.GridDurumuId.Value != (int)GridDurum.Bekliyor)
                    sandik.DepoLokasyonId = (int)DepoLokasyon.Grid;

                var tumIcerikler = await sandikIcerikRepo.FindAsync(si => si.SandikId == request.SandikId);
                var tumUrunIds = tumIcerikler.Where(si => si.CekiSatiriId.HasValue).Select(si => si.CekiSatiriId!.Value).ToList();

                bool hepsiTamamlandi = true;
                bool enAzBiriKonuldu = false;
                foreach (var urunId in tumUrunIds)
                {
                    var u = await cekiSatiriRepo.GetByIdAsync(urunId);
                    if (u != null)
                    {
                        if (u.DurumId != (int)UrunDurum.Tamamlandi) hepsiTamamlandi = false;
                        var uIcerik = tumIcerikler.FirstOrDefault(si => si.CekiSatiriId == urunId);
                        if (uIcerik != null && uIcerik.KonulanAdet > 0) enAzBiriKonuldu = true;
                    }
                }

                var eskiDurumId = sandik.DurumId;

                if (hepsiTamamlandi && tumUrunIds.Count > 0) sandik.DurumId = (int)SandikDurum.Hazir;
                else if (enAzBiriKonuldu) sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                else sandik.DurumId = (int)SandikDurum.Bos;

                sandikRepo.Update(sandik);

                // Eğer sandık "Hazır" iken geri açıldıysa, hareket kaydı oluştur.
                if (eskiDurumId == (int)SandikDurum.Hazir && sandik.DurumId != (int)SandikDurum.Hazir)
                {
                    var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? "Hazir";
                    var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), sandik.DurumId) ?? "Hazirlaniyor";

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = request.ProjeId,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik.Id.ToString(),
                        Islem = "Sandık Geri Açıldı",
                        IslemTipiId = null,
                        EskiDeger = eskiDurumMetni,
                        YeniDeger = yeniDurumMetni,
                        Aciklama = $"Ürün durumundaki değişiklik nedeniyle sandık tekrar '{yeniDurumMetni}' konumuna getirildi.",
                        KullaniciId = request.KullaniciId
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var durumMetni = Enum.GetName(typeof(UrunDurum), urun.DurumId) ?? urun.DurumId.ToString();
            var gridMetni = Enum.GetName(typeof(GridDurum), urun.GridDurumuId) ?? urun.GridDurumuId.ToString();
            var uckMetni = Enum.GetName(typeof(UcKDurum), urun.UcKDurumuId) ?? urun.UcKDurumuId.ToString();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Ürün Güncellendi",
                IslemTipiId = (int)IslemTipi.UrunGuncellendi,
                KullaniciId = request.KullaniciId,
                Aciklama = $"Durum: {durumMetni}, Grid: {gridMetni}, 3K: {uckMetni}, Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}"
            });

            return Result.Success();
        }
    }
}
