using MediatR;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikSilCommandHandler : IRequestHandler<SandikSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikSilCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(SandikSilCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı.", 404);

            if (sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bu projeye ait değil.");

            // İçinde ürün var mı kontrol et
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await sandikIcerikRepo.FindAsync(x => x.SandikId == sandik.Id)).ToList();
            var manuelSatirlar = new List<CekiSatiri>();

            if (icerikler.Any())
            {
                var cekiSatiriIds = icerikler
                    .Where(x => x.CekiSatiriId.HasValue)
                    .Select(x => x.CekiSatiriId!.Value)
                    .Distinct()
                    .ToArray();

                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                var cekiSatirlari = (await cekiSatiriRepo.FindAsync(x => cekiSatiriIds.Contains(x.Id)))
                    .ToDictionary(x => x.Id);

                foreach (var icerik in icerikler)
                {
                    if (!icerik.CekiSatiriId.HasValue
                        || !cekiSatirlari.TryGetValue(icerik.CekiSatiriId.Value, out var satir)
                        || !satir.IsManuelEklenen)
                    {
                        return Result.Failure($"Bu sandıkta {icerikler.Count} ürün bulunuyor. Önce ürünleri silin veya taşıyın.");
                    }

                    manuelSatirlar.Add(satir);
                }

                var islemGormusSatir = manuelSatirlar.FirstOrDefault(ManuelSatirIslemGormus);
                if (islemGormusSatir != null)
                {
                    return Result.Failure(
                        $"Bu manuel sandıktaki {islemGormusSatir.BarkodNo} ürünü üzerinde 3K işlemi var. Silmeden önce işlemleri geri alın.");
                }
            }

            var sandikNo = sandik.SandikNo;
            var silinenManuelUrunSayisi = manuelSatirlar.Select(x => x.Id).Distinct().Count();

            if (icerikler.Any())
            {
                foreach (var icerik in icerikler)
                {
                    sandikIcerikRepo.Remove(icerik);
                }

                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                foreach (var satir in manuelSatirlar.GroupBy(x => x.Id).Select(x => x.First()))
                {
                    cekiSatiriRepo.Remove(satir);
                }
            }

            sandikRepo.Remove(sandik);
            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = request.SandikId.ToString(),
                Islem = "Sandık Silindi",
                IslemTipiId = (int)IslemTipi.SandikSilindi,
                Aciklama = silinenManuelUrunSayisi > 0
                    ? $"Manuel sandık {sandikNo} ve içindeki {silinenManuelUrunSayisi} manuel ürün silindi."
                    : $"Sandık {sandikNo} silindi."
            });

            return Result.Success();
        }

        private static bool ManuelSatirIslemGormus(CekiSatiri satir)
        {
            return satir.GelenMiktar > 0
                || satir.KarsilananMiktar > 0
                || satir.HataliMiktar > 0
                || satir.StokKarsilanan > 0
                || satir.ProjeKarsilanan > 0
                || satir.ProjeGonderilen > 0
                || satir.TedarikciKarsilanan > 0
                || satir.GeriGonderilenMiktar > 0;
        }
    }
}
