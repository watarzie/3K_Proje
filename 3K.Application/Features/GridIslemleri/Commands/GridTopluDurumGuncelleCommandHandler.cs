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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public GridTopluDurumGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        private static readonly HashSet<int> IzinliDurumlar = new()
        {
            (int)GridDurum.TamGeldi,
            (int)GridDurum.GridKapandi,
            (int)GridDurum.Iptal,
        };

        public async Task<Result> Handle(GridTopluDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            GridTopluDurumGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            if (!IzinliDurumlar.Contains(request.HedefDurumId))
                return Result.Failure("Toplu güncelleme yalnızca Tam Geldi, Grid Kapandı veya İptal durumları için yapılabilir.");

            var durumAdi = ((GridDurum)request.HedefDurumId).ToString();
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = (await repo.FindAsync(cs => request.CekiSatiriIdler.Contains(cs.Id))).ToList();

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var iceriklerBySatirId = new Dictionary<int, IReadOnlyCollection<SandikIcerik>>();
            if (request.HedefDurumId == (int)GridDurum.Iptal)
            {
                var satirIdler = satirlar.Select(s => s.Id).ToList();
                iceriklerBySatirId = (await _unitOfWork.GetRepository<SandikIcerik>()
                        .FindAsync(i => i.CekiSatiriId.HasValue && satirIdler.Contains(i.CekiSatiriId.Value)))
                    .GroupBy(i => i.CekiSatiriId!.Value)
                    .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());
            }

            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Select(s => s.Id));

            if (kilitliSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {kilitliSatirIdleri.Count} tanesi sevk edilmiş sandıkta olduğu için Grid işlemi yapılamaz.");

            var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                _sahaTamamlamaService,
                satirlar,
                cancellationToken);

            if (sahayaAktarilanSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {sahayaAktarilanSatirIdleri.Count} tanesi sahaya aktarıldığı için normal proje üzerinden Grid işlemi yapılamaz.");

            var kullaniciId = _currentUserService.UserId ?? 0;
            int basarili = 0;
            var hatalar = new List<string>();
            var gridKapandiSandikNolari = new HashSet<string>();
            var kaynakSatirIds = new HashSet<int>();

            foreach (var satir in satirlar)
            {
                // Mevcut toplu iş kuralı: İptal/Grid Kapandı, 3K karşılamasından
                // bağımsızdır. Tam Geldi ise gerçekleşmiş 3K bilgisini değiştiremez.
                if (request.HedefDurumId == (int)GridDurum.TamGeldi &&
                    GridUcKSevkPartisiKurali.UcKTarafindaIslemVar(satir))
                {
                    hatalar.Add($"#{satir.SiraNo}: 3K işlem yapılmış.");
                    continue;
                }

                var eskiDurum = satir.GridDurumuId;

                // Durum güncelle
                satir.GridDurumuId = request.HedefDurumId;

                if (request.HedefDurumId == (int)GridDurum.TamGeldi)
                {
                    satir.GridGelenAdet = satir.IstenenAdet;
                }
                else if (request.HedefDurumId == (int)GridDurum.Iptal)
                {
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
                    satir.GridSevkMiktari = null;
                    await GridUcKSevkPartisiKurali.AktifPartiTakibiniTemizleAsync(
                        _unitOfWork,
                        satir,
                        iceriklerBySatirId.GetValueOrDefault(satir.Id) ?? Array.Empty<SandikIcerik>());
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
                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

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

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

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
