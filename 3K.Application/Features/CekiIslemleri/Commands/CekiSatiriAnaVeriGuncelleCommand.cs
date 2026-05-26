using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiSatiriAnaVeriGuncelleCommand : IRequest<Result<CekiSatiriAnaVeriGuncelleDto>>, IAdminOnlyRequest
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
        public int BirimId { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
    }

    public class CekiSatiriAnaVeriGuncelleCommandHandler
        : IRequestHandler<CekiSatiriAnaVeriGuncelleCommand, Result<CekiSatiriAnaVeriGuncelleDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDurumHesaplaService _durumHesaplaService;

        public CekiSatiriAnaVeriGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IDurumHesaplaService durumHesaplaService)
        {
            _unitOfWork = unitOfWork;
            _durumHesaplaService = durumHesaplaService;
        }

        public async Task<Result<CekiSatiriAnaVeriGuncelleDto>> Handle(
            CekiSatiriAnaVeriGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var validation = Validate(request);
            if (validation != null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(validation);

            var satirRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await satirRepo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure("Ceki satiri bulunamadi.", 404);

            var ceki = await _unitOfWork.GetRepository<Ceki>().GetByIdAsync(satir.CekiId);
            if (ceki == null)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure("Ceki bulunamadi.", 404);

            var minimumAdet = GetMinimumAllowedIstenenAdet(satir);
            if (request.IstenenAdet < minimumAdet)
                return Result<CekiSatiriAnaVeriGuncelleDto>.Failure(
                    $"Miktar islenmis miktardan kucuk olamaz. Minimum: {minimumAdet}");

            var oldCekideSandikNo = Normalize(satir.CekideGecenSandikNo);
            var oldEffectiveSandikNo = Normalize(string.IsNullOrWhiteSpace(satir.FiiliSandikNo)
                ? satir.CekideGecenSandikNo
                : satir.FiiliSandikNo);
            var newCekideSandikNo = Normalize(request.SandikNo);
            var shouldSyncFiiliSandik = string.IsNullOrWhiteSpace(satir.FiiliSandikNo) ||
                string.Equals(Normalize(satir.FiiliSandikNo), oldCekideSandikNo, StringComparison.OrdinalIgnoreCase);

            satir.SiraNo = request.SiraNo;
            satir.OlcuResmiPozNo = string.IsNullOrWhiteSpace(request.OlcuResmiPozNo)
                ? null
                : request.OlcuResmiPozNo.Trim();
            satir.BarkodNo = request.BarkodNo.Trim();
            satir.Aciklama = request.Aciklama.Trim();
            satir.IstenenAdet = request.IstenenAdet;
            satir.BirimId = request.BirimId;
            satir.CekideGecenSandikNo = newCekideSandikNo;

            if (shouldSyncFiiliSandik)
                satir.FiiliSandikNo = newCekideSandikNo;

            var newEffectiveSandikNo = Normalize(string.IsNullOrWhiteSpace(satir.FiiliSandikNo)
                ? satir.CekideGecenSandikNo
                : satir.FiiliSandikNo);

            if (!string.Equals(oldEffectiveSandikNo, newEffectiveSandikNo, StringComparison.OrdinalIgnoreCase))
                await MoveSandikIcerikleriAsync(ceki.ProjeId, satir.Id, newEffectiveSandikNo, request.BirimId);
            else
                await SyncSandikIcerikBirimAsync(satir.Id, request.BirimId);

            _durumHesaplaService.HesaplaKalanVeDurum(satir);
            satirRepo.Update(satir);

            await _unitOfWork.SaveChangesAsync();

            return Result<CekiSatiriAnaVeriGuncelleDto>.Success(new CekiSatiriAnaVeriGuncelleDto
            {
                CekiSatiriId = satir.Id,
                SiraNo = satir.SiraNo,
                OlcuResmiPozNo = satir.OlcuResmiPozNo,
                BarkodNo = satir.BarkodNo,
                Aciklama = satir.Aciklama,
                IstenenAdet = satir.IstenenAdet,
                BirimId = satir.BirimId,
                Birim = ((Birim)satir.BirimId).ToString(),
                SandikNo = newEffectiveSandikNo
            });
        }

        private async Task MoveSandikIcerikleriAsync(int projeId, int cekiSatiriId, string sandikNo, int birimId)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var hedefSandik = (await sandikRepo.FindAsync(s =>
                    s.ProjeId == projeId &&
                    s.SandikNo == sandikNo))
                .FirstOrDefault();

            if (hedefSandik == null)
            {
                hedefSandik = new Sandik
                {
                    ProjeId = projeId,
                    SandikNo = sandikNo,
                    TipId = (int)SandikTipi.AhsapKapali,
                    DurumId = (int)SandikDurum.Bos,
                    DepoLokasyonId = (int)DepoLokasyon.Belirsiz
                };

                await sandikRepo.AddAsync(hedefSandik);
                await _unitOfWork.SaveChangesAsync();
            }

            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await icerikRepo.FindAsync(si => si.CekiSatiriId == cekiSatiriId);

            foreach (var icerik in icerikler)
            {
                icerik.SandikId = hedefSandik.Id;
                icerik.BirimId = birimId;
                icerikRepo.Update(icerik);
            }
        }

        private async Task SyncSandikIcerikBirimAsync(int cekiSatiriId, int birimId)
        {
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await icerikRepo.FindAsync(si => si.CekiSatiriId == cekiSatiriId);

            foreach (var icerik in icerikler)
            {
                icerik.BirimId = birimId;
                icerikRepo.Update(icerik);
            }
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
