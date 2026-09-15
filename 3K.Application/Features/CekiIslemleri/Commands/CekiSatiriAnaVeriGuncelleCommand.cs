using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiSatiriAnaVeriGuncelleCommand : IRequest<Result<CekiSatiriAnaVeriGuncelleDto>>, ISecuredRequest
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public int BirimId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
    }

    public class CekiSatiriAnaVeriGuncelleDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public decimal? OrijinalIstenenAdet { get; set; }
        public int BirimId { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
    }

    public class CekiSatiriAnaVeriGuncelleCommandHandler
        : IRequestHandler<CekiSatiriAnaVeriGuncelleCommand, Result<CekiSatiriAnaVeriGuncelleDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public CekiSatiriAnaVeriGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IDurumHesaplaService durumHesaplaService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _durumHesaplaService = durumHesaplaService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<CekiSatiriAnaVeriGuncelleDto>> Handle(
            CekiSatiriAnaVeriGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var validation = Validate(request);
            if (validation != null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(validation);

            // Ana miktar, tahsis ve varsa yeni sandık aynı işlemde kalıcılaşmalıdır.
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionToken => GuncelleAsync(request, transactionToken),
                cancellationToken);
        }

        private async Task<Result<CekiSatiriAnaVeriGuncelleDto>> GuncelleAsync(
            CekiSatiriAnaVeriGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var satirRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await satirRepo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure("Ceki satiri bulunamadi.", 404);

            if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(
                    _sahaTamamlamaService, satir, cancellationToken))
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(SahaAktarimBlokajHelper.SandikMesaji, 409);

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

            var ceki = await _unitOfWork.GetRepository<Ceki>().GetByIdAsync(satir.CekiId);
            if (ceki == null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure("Ceki bulunamadi.", 404);

            var minimumAdet = GetMinimumAllowedIstenenAdet(satir);
            if (request.IstenenAdet < minimumAdet)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(
                    $"Miktar islenmis miktardan kucuk olamaz. Minimum: {minimumAdet}");

            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await icerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id)).ToList();
            var miktarDegisti = request.IstenenAdet != satir.IstenenAdet;
            var tekTamTahsis = icerikler.Count == 1 &&
                (icerikler[0].TahsisMiktari <= 0 || icerikler[0].TahsisMiktari == satir.IstenenAdet);

            if (miktarDegisti)
            {
                var konulanToplam = icerikler.Sum(i => Math.Max(i.KonulanAdet, 0));
                if (request.IstenenAdet < konulanToplam)
                    return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(
                        $"Miktar sandıklara konulan toplam miktardan küçük olamaz. Minimum: {konulanToplam}");

                // Parçalı tahsiste hangi sandıktan miktar düşüleceğine bu ekran karar veremez.
                var toplamTahsis = icerikler.Sum(i => SandikTahsisHelper.HesaplaSandikMiktari(satir, i, icerikler.Count));
                if (!tekTamTahsis && request.IstenenAdet < satir.IstenenAdet && request.IstenenAdet < toplamTahsis)
                    return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(
                        $"Miktar mevcut sandık tahsisleri toplamından ({toplamTahsis}) küçük olamaz. Önce sandık tahsislerini düzenleyin.");
            }

            var oldCekideSandikNo = Normalize(satir.CekideGecenSandikNo);
            var oldEffectiveSandikNo = Normalize(string.IsNullOrWhiteSpace(satir.FiiliSandikNo)
                ? satir.CekideGecenSandikNo
                : satir.FiiliSandikNo);
            var newCekideSandikNo = Normalize(request.SandikNo);
            var shouldSyncFiiliSandik = string.IsNullOrWhiteSpace(satir.FiiliSandikNo) ||
                string.Equals(Normalize(satir.FiiliSandikNo), oldCekideSandikNo, StringComparison.OrdinalIgnoreCase);
            var newEffectiveSandikNo = shouldSyncFiiliSandik ? newCekideSandikNo : oldEffectiveSandikNo;
            var sandikDegisti = !string.Equals(oldEffectiveSandikNo, newEffectiveSandikNo, StringComparison.OrdinalIgnoreCase);

            Sandik? hedefSandik = null;
            if (sandikDegisti)
            {
                if (icerikler.Count > 1)
                    return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(
                        "Birden fazla sandığa tahsis edilmiş ürünün sandığı bu ekrandan değiştirilemez. Sandık ürün taşıma işlemini kullanın.");

                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                hedefSandik = (await sandikRepo.FindAsync(s =>
                    s.ProjeId == ceki.ProjeId && s.SandikNo == newEffectiveSandikNo)).FirstOrDefault();

                if (hedefSandik != null && SandikSevkKilidiHelper.SandikKilitliMi(hedefSandik))
                    return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

                if (hedefSandik == null)
                {
                    hedefSandik = new Sandik
                    {
                        ProjeId = ceki.ProjeId,
                        SandikNo = newEffectiveSandikNo,
                        TipId = (int)SandikTipi.AhsapKapali,
                        DurumId = (int)SandikDurum.Hazirlaniyor,
                        DepoLokasyonId = (int)DepoLokasyon.Belirsiz
                    };
                    await sandikRepo.AddAsync(hedefSandik);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            satir.SiraNo = request.SiraNo;
            satir.OlcuResmiPozNo = string.IsNullOrWhiteSpace(request.OlcuResmiPozNo)
                ? null
                : request.OlcuResmiPozNo.Trim();
            satir.BarkodNo = request.BarkodNo.Trim();
            satir.Aciklama = request.Aciklama.Trim();
            // Yalnızca gerçek miktar değişikliğini işaretle. İlk değere geri dönülse de
            // dolu alan korunur; böylece düzenleme bilgisi kaybolmaz.
            if (miktarDegisti)
                satir.OrijinalIstenenAdet ??= satir.IstenenAdet;
            satir.IstenenAdet = request.IstenenAdet;
            if (miktarDegisti)
                MiktarDegisikligiSonrasiGridDurumunuGuncelle(satir);
            satir.BirimId = request.BirimId;
            satir.CekideGecenSandikNo = newCekideSandikNo;

            if (shouldSyncFiiliSandik)
                satir.FiiliSandikNo = newCekideSandikNo;

            if (miktarDegisti && tekTamTahsis)
            {
                // Tek tam tahsis ana miktarı izler; bilinçli parçalı tahsisler aynen korunur.
                var icerik = icerikler[0];
                icerik.TahsisMiktari = request.IstenenAdet;
                icerik.EksikAdet = Math.Max(icerik.TahsisMiktari - icerik.KonulanAdet, 0);
            }

            foreach (var icerik in icerikler)
            {
                if (hedefSandik != null)
                    icerik.SandikId = hedefSandik.Id;
                icerik.BirimId = request.BirimId;
                icerikRepo.Update(icerik);
            }

            _durumHesaplaService.HesaplaKalanVeDurum(satir);
            satirRepo.Update(satir);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CekiSatiriAnaVeriGuncelleDto>.Success(new CekiSatiriAnaVeriGuncelleDto
            {
                CekiSatiriId = satir.Id,
                SiraNo = satir.SiraNo,
                OlcuResmiPozNo = satir.OlcuResmiPozNo,
                BarkodNo = satir.BarkodNo,
                Aciklama = satir.Aciklama,
                IstenenAdet = satir.IstenenAdet,
                OrijinalIstenenAdet = satir.OrijinalIstenenAdet,
                BirimId = satir.BirimId,
                Birim = ((Birim)satir.BirimId).ToString(),
                SandikNo = newEffectiveSandikNo
            });
        }

        private static void MiktarDegisikligiSonrasiGridDurumunuGuncelle(CekiSatiri satir)
        {
            // Çeki ihtiyacının değişmesi yeni bir fiziksel teslim değildir. Gelen,
            // sevk edilen ve 3K'da karşılanan miktarlar ile aktif parti korunur.
            // Yalnız normal Grid kabulünün Tam/Eksik etiketi yeni ihtiyacı izler;
            // böylece tamamlanmış eski teslim, artan ihtiyacın devam sevkini kilitlemez.
            // İptal, Grid kapandı, trafo ve diğer iş akışları burada yeniden açılmaz.
            if (satir.TrafoSevkAdet != 0 || satir.GridGelenAdet <= 0 ||
                (satir.GridDurumuId != (int)GridDurum.TamGeldi &&
                 satir.GridDurumuId != (int)GridDurum.EksikGeldi))
                return;

            satir.GridDurumuId = satir.GridGelenAdet >= satir.IstenenAdet
                ? (int)GridDurum.TamGeldi
                : (int)GridDurum.EksikGeldi;
        }

        private static decimal GetMinimumAllowedIstenenAdet(CekiSatiri satir)
        {
            var gridIslenen = satir.GridGelenAdet + satir.TrafoSevkAdet;
            var ucKIslenen = satir.GelenMiktar +
                satir.StokKarsilanan +
                satir.ProjeKarsilanan +
                satir.TedarikciKarsilanan -
                satir.ProjeGonderilen;

            return Math.Max(gridIslenen, Math.Max(ucKIslenen, 0));
        }

        private static string? Validate(CekiSatiriAnaVeriGuncelleCommand request)
        {
            if (request.CekiSatiriId <= 0)
                return "Ceki satiri zorunludur.";
            if (request.SiraNo <= 0)
                return "Sira no sifirdan buyuk olmalidir.";
            if (string.IsNullOrWhiteSpace(request.BarkodNo))
                return "Barkod zorunludur.";
            if (string.IsNullOrWhiteSpace(request.Aciklama))
                return "Aciklama zorunludur.";
            if (request.IstenenAdet <= 0)
                return "Miktar sifirdan buyuk olmalidir.";
            if (!Enum.IsDefined(typeof(Birim), request.BirimId))
                return "Gecersiz birim.";
            if (string.IsNullOrWhiteSpace(request.SandikNo))
                return "Sandik no zorunludur.";

            return null;
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
