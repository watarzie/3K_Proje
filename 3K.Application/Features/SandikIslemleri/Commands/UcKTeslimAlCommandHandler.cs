using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using System.Globalization;
using _3K.Core.Helpers;
using _3K.Application.Features.UcKIslemleri.Commands;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UcKTeslimAlCommandHandler : IRequestHandler<UcKTeslimAlCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ILookupCacheService _lookupCache;

        public UcKTeslimAlCommandHandler(
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

        public async Task<Result> Handle(UcKTeslimAlCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            UcKTeslimAlCommand request,
            CancellationToken cancellationToken)
        {
            if (request.GelenMiktar <= 0)
                return Result.Failure("Gelen miktar 0'dan büyük olmalıdır.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, satir, cancellationToken))
                return Result.Failure(SahaAktarimBlokajHelper.UcKMesaji);

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

            if (satir.GridDurumuId is (int)GridDurum.Iptal or (int)GridDurum.GridKapandi)
                return Result.Failure("Grid tarafından iptal edilen veya kapatılan ürün için 3K teslim işlemi yapılamaz.", 409);

            if (satir.KaliteDurumId.HasValue &&
                _lookupCache.GetDeger<LookupKaliteDurum>(satir.KaliteDurumId.Value) == "Tadilatta")
            {
                return Result.Failure("Bu ürün Kalite tarafından 'Tadilatta' olarak işaretlenmiş. 3K işlemi yapılamaz.", 409);
            }

            var eskiGelenMiktar = satir.GelenMiktar;
            var eskiUcKDurum = satir.UcKDurumuId;

            var teslimUstSiniriResult = GridUcKSevkPartisiKurali.TamKarsilamaMiktariniHesapla(
                _unitOfWork,
                satir,
                null,
                Math.Max(satir.KalanMiktar, 0));
            if (!teslimUstSiniriResult.IsSuccess)
                return Result.Failure(teslimUstSiniriResult.Error!.Message, teslimUstSiniriResult.StatusCode);
            if (request.GelenMiktar > teslimUstSiniriResult.Value)
            {
                return Result.Failure(
                    $"Gelen miktar ({FormatAdet(request.GelenMiktar)}), aktif Grid sevk partisinin kalan miktarını ({FormatAdet(teslimUstSiniriResult.Value)}) aşamaz.",
                    400);
            }

            var aktifPartiKayitResult = GridUcKSevkPartisiKurali.AktifPartiKarsilamasiniKaydet(
                _unitOfWork,
                satir,
                null,
                request.GelenMiktar);
            if (!aktifPartiKayitResult.IsSuccess)
                return Result.Failure(aktifPartiKayitResult.Error!.Message, aktifPartiKayitResult.StatusCode);

            // Kümülatif toplama — parça parça gelebilir
            satir.GelenMiktar += request.GelenMiktar;
            satir.TeslimTarihi = TurkeyTime.Now;
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
                satir);
            if (!senkronizasyonResult.IsSuccess)
                return Result.Failure(senkronizasyonResult.Error!.Message, senkronizasyonResult.StatusCode);

            await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, satir.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "3K Teslim Alma",
                IslemTipiId = (int)IslemTipi.UcKTeslimAlindi,
                EskiDeger = $"GelenMiktar:{eskiGelenMiktar}, UcKDurum:{eskiUcKDurum}",
                YeniDeger = $"GelenMiktar:{satir.GelenMiktar}, UcKDurum:{satir.UcKDurumuId}",
                Aciklama = $"+{FormatAdet(request.GelenMiktar)} adet teslim alındı. {request.Aciklama}"
            });

            return Result.Success();
        }

        private static string FormatAdet(decimal value)
        {
            if (decimal.Truncate(value) == value)
                return decimal.Truncate(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
