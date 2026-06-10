using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikKapatCommandHandler : IRequestHandler<SandikKapatCommand, SandikKapatResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;

        public SandikKapatCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
        }

        public async Task<SandikKapatResult> Handle(SandikKapatCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return new SandikKapatResult { IsSuccess = false, Message = "Oturum açmanız gerekiyor." };
            }

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null)
            {
                return new SandikKapatResult { IsSuccess = false, Message = "Sandık bulunamadı." };
            }

            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
            {
                return new SandikKapatResult { IsSuccess = false, Message = SandikSevkKilidiHelper.SandikKilitliMesaji };
            }

            if (sandik.DurumId == (int)SandikDurum.Kapandi)
            {
                return new SandikKapatResult { IsSuccess = false, Message = "Sandık zaten kapalı (Hazır) durumdadır." };
            }

            // Sandığın içindeki ürünleri bul
            // SandikIcerik tablosundan bulabiliriz veya CekiSatiri'ndeki FiiliSandikNo'dan
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await icerikRepo.FindAsync(si => si.SandikId == request.SandikId);
            
            var hataliVeyaEksikUrunler = new List<object>();

            foreach (var icerik in icerikler)
            {
                if (!icerik.CekiSatiriId.HasValue) continue;
                var urun = await cekiSatiriRepo.GetByIdAsync(icerik.CekiSatiriId.Value);
                if (urun == null) continue;

                // Ürün hala eksikse veya hatalı geldiyse
                if (urun.KalanMiktar > 0)
                {
                    hataliVeyaEksikUrunler.Add(new
                    {
                        SiraNo = urun.SiraNo,
                        Barkod = urun.BarkodNo,
                        Aciklama = urun.Aciklama,
                        Kalan = urun.KalanMiktar,
                        Durum = urun.UcKDurumuId,
                        GridDurum = urun.GridDurumuId
                    });
                }
            }

            // Uyarı durumu
            if (hataliVeyaEksikUrunler.Any() && !request.ForceClose)
            {
                return new SandikKapatResult
                {
                    IsSuccess = false,
                    HasMissingOrDefectiveItems = true,
                    Message = "Sandık içinde eksik veya hatalı ürünler var. Yine de kapatmak istiyor musunuz?",
                    MissingItemDetails = hataliVeyaEksikUrunler
                };
            }

            var eskiDurumId = sandik.DurumId;
            var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? eskiDurumId.ToString();

            // Kapat (Eksik yok, ya da ForceClose = true)
            sandik.DurumId = (int)SandikDurum.Kapandi;
            sandikRepo.Update(sandik);
            await _unitOfWork.SaveChangesAsync();

            var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), SandikDurum.Kapandi) ?? "Hazir";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = sandik.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = sandik.Id.ToString(),
                Islem = "Sandık Manuel Kapatma",
                IslemTipiId = (int)IslemTipi.SandikKapatildi,
                EskiDeger = eskiDurumMetni,
                YeniDeger = yeniDurumMetni,
                Aciklama = request.ForceClose 
                    ? $"Sandık {sandik.SandikNo} (eksik ürün loguna rağmen zorunlu onayla) manuel olarak kapatıldı." 
                    : $"Sandık {sandik.SandikNo} başarıyla kapatıldı."
            });

            return new SandikKapatResult { IsSuccess = true };
        }
    }
}
