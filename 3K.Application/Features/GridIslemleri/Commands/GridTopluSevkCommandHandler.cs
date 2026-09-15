using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridTopluSevkCommandHandler : IRequestHandler<GridTopluSevkCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public GridTopluSevkCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(GridTopluSevkCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            GridTopluSevkCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = (await repo.FindAsync(cs =>
                request.CekiSatiriIdler.Contains(cs.Id))).ToList();

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var satirIdler = satirlar.Select(s => s.Id).ToList();
            var iceriklerBySatirId = (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && satirIdler.Contains(i.CekiSatiriId.Value)))
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());

            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Select(s => s.Id));

            if (kilitliSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {kilitliSatirIdleri.Count} tanesi sevk edilmiş sandıkta olduğu için Grid sevk işlemi yapılamaz.", 400);

            var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                _sahaTamamlamaService,
                satirlar,
                cancellationToken);

            if (sahayaAktarilanSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {sahayaAktarilanSatirIdleri.Count} tanesi sahaya aktarıldığı için normal proje üzerinden Grid sevk işlemi yapılamaz.", 400);

            var tadilattakiSatirlar = satirlar
                .Where(s => s.KaliteDurumId.HasValue
                    && _lookupCache.GetDeger<LookupKaliteDurum>(s.KaliteDurumId.Value) == "Tadilatta")
                .ToList();

            if (tadilattakiSatirlar.Any())
            {
                var detay = string.Join(", ", tadilattakiSatirlar
                    .Take(5)
                    .Select(s => $"{(string.IsNullOrWhiteSpace(s.BarkodNo) ? s.SiraNo.ToString() : s.BarkodNo)} - {s.Aciklama}"));
                var kalan = tadilattakiSatirlar.Count > 5 ? $" (+{tadilattakiSatirlar.Count - 5})" : string.Empty;

                return Result.Failure(
                    $"Kalite durumu 'Tadilatta' olan ürünler toplu sevk edilemez: {detay}{kalan}",
                    400);
            }

            var now = TurkeyTime.Now;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int guncellenen = 0;
            var atlananlar = new List<string>();
            var tahsisKapasitesiEngelleri = new List<string>();
            var sevkEdilenSatirlar = new List<CekiSatiri>();

            foreach (var satir in satirlar)
            {
                var devamSevkKarari = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);

                if (!devamSevkKarari.YeniPartiMi && GridUcKSevkPartisiKurali.UcKTarafindaIslemVar(satir))
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - 3K tarafında işlem yapılmış");
                    continue;
                }

                var sevkMiktari = satir.IstenenAdet;
                var gridTamGeldiYapilacak = !devamSevkKarari.YeniPartiMi &&
                    satir.GridDurumuId != (int)GridDurum.TrafoSevk;

                if (devamSevkKarari.YeniPartiMi)
                {
                    sevkMiktari = devamSevkKarari.UstSinir;
                }
                else if (satir.GridDurumuId == (int)GridDurum.TrafoSevk)
                {
                    if (satir.GridGelenAdet <= 0)
                    {
                        atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Trafo sevk, Grid'e gelen miktar yok");
                        continue;
                    }

                    sevkMiktari = satir.GridGelenAdet;
                }
                var satirIcerikleri = iceriklerBySatirId.GetValueOrDefault(satir.Id)
                    ?? Array.Empty<SandikIcerik>();
                var tahsisKapasitesiResult = GridUcKSevkPartisiKurali.YeniSevkTahsisKapasitesiniDogrula(
                    satir,
                    satirIcerikleri,
                    sevkMiktari,
                    trafoSevkAdedi: gridTamGeldiYapilacak ? 0 : satir.TrafoSevkAdet);
                if (!tahsisKapasitesiResult.IsSuccess)
                {
                    var mesaj = $"#{satir.SiraNo} ({satir.Aciklama}) - {tahsisKapasitesiResult.Error!.Message}";
                    atlananlar.Add(mesaj);
                    tahsisKapasitesiEngelleri.Add(mesaj);
                    continue;
                }

                // Atlanan satırın bellekteki hali de korunur. İzlenen entity kullanan
                // akışlarda başarısız satırın diğerleriyle kaydedilmesi de önlenir.
                if (gridTamGeldiYapilacak)
                {
                    satir.GridDurumuId = (int)GridDurum.TamGeldi;
                    satir.GridGelenAdet = satir.IstenenAdet;
                    satir.TrafoSevkAdet = 0;
                }

                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
                await GridUcKSevkPartisiKurali.YeniPartiBaslatAsync(
                    _unitOfWork,
                    satir,
                    sevkMiktari,
                    devamSevkKarari,
                    satirIcerikleri);
                satir.GridSevkTarihi = now;
                satir.GridPersonelId = kullaniciId;
                satir.GridAciklama = request.Aciklama;

                // Genel durumu otomatik hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                GridSurecDurumHelper.SyncSurecTamamlandi(satir);

                repo.Update(satir);
                guncellenen++;
                sevkEdilenSatirlar.Add(satir);
            }

            if (guncellenen == 0)
            {
                if (tahsisKapasitesiEngelleri.Any())
                {
                    return Result.Failure(
                        $"Seçili ürünlerin sandık tahsis kapasitesi Grid sevkini karşılamıyor: {string.Join("; ", tahsisKapasitesiEngelleri.Take(3))}",
                        409);
                }

                return Result.Failure("Sevk edilebilecek urun bulunamadi. Trafo sevk satirlari icin Grid gelen adet 0 olamaz.", 400);
            }

            await _unitOfWork.SaveChangesAsync();

            var sandikGruplari = sevkEdilenSatirlar.GroupBy(s => s.FiiliSandikNo ?? s.CekideGecenSandikNo ?? "Belirsiz");
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Sevk Durumu: Sevk Edildi");
            sb.AppendLine($"{guncellenen} adet ürün toplu sevk edildi.\n");
            if (atlananlar.Any())
            {
                sb.AppendLine($"Atlanan ({atlananlar.Count}):");
                foreach (var atlanan in atlananlar.Take(10))
                    sb.AppendLine($"  - {atlanan}");
                sb.AppendLine();
            }
            
            foreach (var grup in sandikGruplari)
            {
                sb.AppendLine($"Sandık: {grup.Key}");
                foreach (var s in grup)
                {
                    sb.AppendLine($"  - {s.OlcuResmiPozNo ?? s.SiraNo.ToString()} - {s.Aciklama}");
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
