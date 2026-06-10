using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid toplu durum güncelleme handler: Tam Geldi, Grid Kapandı veya İptal.
    /// </summary>
    public class GridTopluDurumGuncelleCommandHandler : IRequestHandler<GridTopluDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public GridTopluDurumGuncelleCommandHandler(
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

        private static readonly HashSet<int> IzinliDurumlar = new()
        {
            (int)GridDurum.TamGeldi,
            (int)GridDurum.GridKapandi,
            (int)GridDurum.Iptal,
        };

        public async Task<Result> Handle(GridTopluDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            if (!IzinliDurumlar.Contains(request.HedefDurumId))
                return Result.Failure("Toplu güncelleme yalnızca Tam Geldi, Grid Kapandı veya İptal durumları için yapılabilir.");

            var durumAdi = ((GridDurum)request.HedefDurumId).ToString();
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await repo.FindAsync(cs => request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Select(s => s.Id));

            if (kilitliSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {kilitliSatirIdleri.Count} tanesi sevk edilmiş sandıkta olduğu için Grid işlemi yapılamaz.");

            var kullaniciId = _currentUserService.UserId ?? 0;
            int basarili = 0;
            var hatalar = new List<string>();
            var gridKapandiSandikNolari = new HashSet<string>();

            foreach (var satir in satirlar)
            {
                // 3K işlem blokajı (İptal ve GridKapandı hariç — onlar 3K'dan bağımsız)
                if (request.HedefDurumId == (int)GridDurum.TamGeldi)
                {
                    if (satir.UcKDurumuId != (int)UcKDurum.Bekliyor || satir.GelenMiktar > 0 || satir.KarsilananMiktar > 0)
                    {
                        hatalar.Add($"#{satir.SiraNo}: 3K işlem yapılmış.");
                        continue;
                    }
                }

                var eskiDurum = satir.GridDurumuId;

                // Durum güncelle
                satir.GridDurumuId = request.HedefDurumId;

                if (request.HedefDurumId == (int)GridDurum.TamGeldi)
                {
                    satir.GridGelenAdet = satir.IstenenAdet;
                }
                else if (request.HedefDurumId == (int)GridDurum.Iptal || request.HedefDurumId == (int)GridDurum.GridKapandi)
                {
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
                    satir.GridSevkMiktari = null;
                }

                if (request.HedefDurumId == (int)GridDurum.GridKapandi)
                {
                    var sandikNo = satir.FiiliSandikNo ?? satir.CekideGecenSandikNo;
                    if (!string.IsNullOrWhiteSpace(sandikNo))
                        gridKapandiSandikNolari.Add(sandikNo);
                }

                satir.GridAciklama = request.Aciklama;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);
                GridSurecDurumHelper.SyncSurecTamamlandi(satir);

                repo.Update(satir);
                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = $"Grid Toplu {durumAdi}",
                    IslemTipiId = (int)IslemTipi.GridDurumGuncellendi,
                    EskiDeger = eskiDurum.ToString(),
                    YeniDeger = request.HedefDurumId.ToString(),
                    Aciklama = $"Toplu {durumAdi} — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            if (basarili == 0)
                return Result.Failure("Hiçbir ürün güncellenemedi.");

            if (gridKapandiSandikNolari.Count > 0)
            {
                await SandiklariGridLokasyonunaAlAsync(request.ProjeId, gridKapandiSandikNolari);
            }

            await _unitOfWork.SaveChangesAsync();

            if (hatalar.Any())
                return Result.Success();

            return Result.Success();
        }

        private async Task SandiklariGridLokasyonunaAlAsync(int projeId, IReadOnlyCollection<string> sandikNolari)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikNoListesi = sandikNolari.ToList();
            var sandiklar = await sandikRepo.FindAsync(s => s.ProjeId == projeId && sandikNoListesi.Contains(s.SandikNo));

            foreach (var sandik in sandiklar.Where(s => s.DepoLokasyonId != (int)DepoLokasyon.Grid))
            {
                sandik.DepoLokasyonId = (int)DepoLokasyon.Grid;
                sandikRepo.Update(sandik);
            }
        }
    }
}
