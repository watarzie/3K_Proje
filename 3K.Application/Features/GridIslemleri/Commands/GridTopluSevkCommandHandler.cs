using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridTopluSevkCommandHandler : IRequestHandler<GridTopluSevkCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public GridTopluSevkCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(GridTopluSevkCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await repo.FindAsync(cs =>
                request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var now = DateTime.UtcNow;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int guncellenen = 0;

            foreach (var satir in satirlar)
            {
                satir.GridDurumuId = (int)GridDurum.TamGeldi;
                satir.GridGelenAdet = satir.IstenenAdet;
                satir.TrafoSevkAdet = 0;

                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
                satir.GridSevkMiktari = satir.IstenenAdet;
                satir.GridSevkTarihi = now;
                satir.GridPersonelId = kullaniciId;
                satir.GridAciklama = request.Aciklama;

                // Genel durumu otomatik hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);

                repo.Update(satir);
                guncellenen++;
            }

            await _unitOfWork.SaveChangesAsync();

            var sandikGruplari = satirlar.GroupBy(s => s.FiiliSandikNo ?? s.CekideGecenSandikNo ?? "Belirsiz");
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Sevk Durumu: Sevk Edildi");
            sb.AppendLine($"{guncellenen} adet ürün toplu sevk edildi.\n");
            
            foreach (var grup in sandikGruplari)
            {
                sb.AppendLine($"📦 Sandık: {grup.Key}");
                foreach (var s in grup)
                {
                    sb.AppendLine($"  • {s.OlcuResmiPozNo ?? s.SiraNo.ToString()} - {s.Aciklama}");
                }
                sb.AppendLine(); // Boşluk
            }

            if (!string.IsNullOrWhiteSpace(request.Aciklama))
            {
                sb.AppendLine($"Not: {request.Aciklama}");
            }

            // Toplu hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = kullaniciId,
                ReferansTipi = "TopluSevk",
                ReferansId = string.Join(",", request.CekiSatiriIdler),
                Islem = "Grid Toplu Sevk",
                IslemTipiId = (int)IslemTipi.GridTopluSevkEdildi,
                EskiDeger = ((int)GridDurum.Bekliyor).ToString(),
                YeniDeger = ((int)GridDurum.TamGeldi).ToString(),
                Aciklama = sb.ToString().TrimEnd()
            });

            return Result.Success();
        }
    }
}
