using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using _3K.Application.Features.UcKIslemleri.Commands;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UcKTopluTeslimAlCommandHandler : IRequestHandler<UcKTopluTeslimAlCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ILookupCacheService _lookupCache;

        public UcKTopluTeslimAlCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(UcKTopluTeslimAlCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            UcKTopluTeslimAlCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Urunler == null || request.Urunler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var idler = request.Urunler.Select(u => u.CekiSatiriId).ToList();
            var satirlar = (await repo.FindAsync(cs => idler.Contains(cs.Id))).ToDictionary(s => s.Id);

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var now = TurkeyTime.Now;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int teslimAlinan = 0;
            var isKuraliBlokajlari = new List<string>();
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                idler);
            var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                _sahaTamamlamaService,
                satirlar.Values,
                cancellationToken);
            var kaynakSatirIds = new HashSet<int>();

            foreach (var item in request.Urunler)
            {
                if (!satirlar.TryGetValue(item.CekiSatiriId, out var satir))
                    continue;

                if (kilitliSatirIdleri.Contains(item.CekiSatiriId))
                    continue;

                if (sahayaAktarilanSatirIdleri.Contains(item.CekiSatiriId))
                    continue;

                if (satir.GridDurumuId is (int)GridDurum.Iptal or (int)GridDurum.GridKapandi)
                {
                    isKuraliBlokajlari.Add($"#{satir.SiraNo}: Grid tarafından iptal edilmiş veya kapatılmış.");
                    continue;
                }

                if (satir.KaliteDurumId.HasValue &&
                    _lookupCache.GetDeger<LookupKaliteDurum>(satir.KaliteDurumId.Value) == "Tadilatta")
                {
                    isKuraliBlokajlari.Add($"#{satir.SiraNo}: Kalite Tadilatta.");
                    continue;
                }

                if (item.GelenMiktar <= 0)
                    continue;

                var seciliIcerikResult = await UcKSandikIcerikSenkronizasyonHelper.GetSeciliIcerikAsync(
                    _unitOfWork,
                    satir.Id,
                    item.SandikIcerikId);
                if (!seciliIcerikResult.IsSuccess)
                    continue;

                var seciliIcerik = seciliIcerikResult.Value;
                var sandikKalan = seciliIcerik == null
                    ? satir.KalanMiktar
                    : Math.Max((seciliIcerik.TahsisMiktari > 0 ? seciliIcerik.TahsisMiktari : satir.IstenenAdet) - seciliIcerik.KonulanAdet, 0);
                var teslimUstSiniriResult = GridUcKSevkPartisiKurali.TamKarsilamaMiktariniHesapla(
                    _unitOfWork,
                    satir,
                    seciliIcerik,
                    sandikKalan);
                if (!teslimUstSiniriResult.IsSuccess)
                    continue;

                var gelenMiktar = Math.Min(item.GelenMiktar, teslimUstSiniriResult.Value);
                if (gelenMiktar <= 0)
                    continue;

                var aktifPartiKayitResult = GridUcKSevkPartisiKurali.AktifPartiKarsilamasiniKaydet(
                    _unitOfWork,
                    satir,
                    seciliIcerik,
                    gelenMiktar);
                if (!aktifPartiKayitResult.IsSuccess)
                    continue;

                satir.GelenMiktar += gelenMiktar;
                satir.TeslimTarihi = now;
                satir.UcKAciklama = request.Aciklama;

                // 3K durumunu otomatik belirle
                if (satir.GelenMiktar >= satir.IstenenAdet)
                    satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                else
                    satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
                satir.UcKKarsilamaTipiId = satir.UcKDurumuId;

                // Genel durumu otomatik hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                // KURAL 2: Merkezi kalan hesaplaması ve durum override
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                var senkronizasyonResult = await UcKSandikIcerikSenkronizasyonHelper.SenkronizeAsync(
                    _unitOfWork,
                    satir,
                    item.SandikIcerikId);
                if (!senkronizasyonResult.IsSuccess)
                    return Result.Failure(senkronizasyonResult.Error!.Message, senkronizasyonResult.StatusCode);

                await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, satir.Id);

                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

                teslimAlinan++;
            }

            if (teslimAlinan == 0 && isKuraliBlokajlari.Count > 0)
            {
                return Result.Failure(
                    $"Hiçbir ürün teslim alınamadı: {string.Join("; ", isKuraliBlokajlari.Take(3))}",
                    409);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            // Toplu hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = kullaniciId,
                ReferansTipi = "TopluTeslim",
                ReferansId = string.Join(",", idler),
                Islem = "3K Toplu Teslim Alma",
                IslemTipiId = (int)IslemTipi.UcKTopluTeslimAlindi,
                YeniDeger = $"{teslimAlinan} ürün teslim alındı",
                Aciklama = request.Aciklama
            });

            return Result.Success();
        }
    }
}
