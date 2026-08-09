using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class SandiklardanSahaProjesiOlusturCommand : IRequest<Result<ProjeDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "sahaya-aktar";

        public int KaynakProjeId { get; set; }
        public int? HedefSahaProjeId { get; set; }
        public List<int> SandikIds { get; set; } = new();
        public string? ProjeNo { get; set; }
        public string? Aciklama { get; set; }
    }

    public class SandiklardanSahaProjesiOlusturCommandHandler : IRequestHandler<SandiklardanSahaProjesiOlusturCommand, Result<ProjeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ISandikService _sandikService;

        public SandiklardanSahaProjesiOlusturCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ISahaTamamlamaService sahaTamamlamaService,
            ISandikService sandikService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _sahaTamamlamaService = sahaTamamlamaService;
            _sandikService = sandikService;
        }

        public async Task<Result<ProjeDto>> Handle(SandiklardanSahaProjesiOlusturCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(
                    async transactionCancellationToken =>
                    {
                        var result = await HandleInTransactionAsync(request, transactionCancellationToken);
                        if (!result.IsSuccess)
                            throw new SandikBazliSahaAktarimRollbackException(result);

                        return result;
                    },
                    cancellationToken);
            }
            catch (SandikBazliSahaAktarimRollbackException exception)
            {
                return exception.Result;
            }
        }

        private async Task<Result<ProjeDto>> HandleInTransactionAsync(
            SandiklardanSahaProjesiOlusturCommand request,
            CancellationToken cancellationToken)
        {
            var sandikIds = request.SandikIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (request.KaynakProjeId <= 0)
                return Result<ProjeDto>.Failure("Kaynak proje bilgisi bulunamadı.");

            if (sandikIds.Count == 0)
                return Result<ProjeDto>.Failure("Sahaya aktarmak için en az bir sandık seçilmelidir.");

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var kaynakProje = await projeRepo.GetByIdAsync(request.KaynakProjeId);

            if (kaynakProje == null)
                return Result<ProjeDto>.Failure("Kaynak proje bulunamadı.", 404);

            if (kaynakProje.ProjeTipiId != (int)ProjeTipi.Normal)
                return Result<ProjeDto>.Failure("Sandık aktarımı sadece normal projelerden saha projesine yapılabilir.");

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = (await sandikRepo.FindAsync(s =>
                    sandikIds.Contains(s.Id) &&
                    s.ProjeId == request.KaynakProjeId))
                .OrderBy(s => ExtractNumber(s.SandikNo))
                .ThenBy(s => s.SandikNo)
                .ToList();

            if (sandiklar.Count != sandikIds.Count)
                return Result<ProjeDto>.Failure("Seçilen sandıklardan bazıları kaynak proje altında bulunamadı.");

            if (sandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi))
                return Result<ProjeDto>.Failure("Sevk edilmiş sandıklar sahaya aktarılamaz.");

            var etkinIcerikMap = await _sandikService.GetEtkinSandikIcerikleriAsync(
                sandikIds,
                cancellationToken);
            var etkinIcerikler = sandiklar
                .SelectMany(s => etkinIcerikMap
                    .GetValueOrDefault(s.Id, Array.Empty<SandikIcerik>())
                    .Where(SandikBazliSahaAktarimGuvenlikKural.IncelenecekIcerikMi))
                .ToList();

            if (etkinIcerikler.Count == 0)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda sahaya aktarılabilecek ürün bulunamadı.");

            var cekiSatiriIds = etkinIcerikler
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = cekiSatiriIds.Count == 0
                ? new Dictionary<int, CekiSatiri>()
                : (await cekiSatiriRepo.FindAsync(cs =>
                        cekiSatiriIds.Contains(cs.Id) &&
                        !cs.KaynakCekiSatiriId.HasValue))
                    .ToDictionary(cs => cs.Id);

            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var tumFizikselIcerikler = cekiSatiriIds.Count == 0
                ? new List<SandikIcerik>()
                : (await icerikRepo.FindAsync(i =>
                        i.CekiSatiriId.HasValue &&
                        cekiSatiriIds.Contains(i.CekiSatiriId.Value)))
                    .ToList();

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiIds = satirlar.Values.Select(s => s.CekiId).Distinct().ToList();
            var cekiler = (await cekiRepo.FindAsync(c =>
                    cekiIds.Contains(c.Id) &&
                    c.ProjeId == request.KaynakProjeId))
                .ToDictionary(c => c.Id);

            var projeDisiSatirVar = satirlar.Values.Any(s => !cekiler.ContainsKey(s.CekiId));
            if (projeDisiSatirVar)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda kaynak proje ile eşleşmeyen ürünler var.");

            var aktifTamamlamaMap = satirlar.Count == 0
                ? new Dictionary<int, decimal>()
                : await _sahaTamamlamaService.GetAktifTamamlamaMapAsync(satirlar.Keys, cancellationToken);
            var dogrulama = SandikBazliSahaAktarimGuvenlikKural.Dogrula(
                sandiklar,
                etkinIcerikMap,
                satirlar,
                tumFizikselIcerikler,
                aktifTamamlamaMap);

            if (!dogrulama.Basarili)
            {
                var hataMesaji =
                    "Seçili sandıklar sahaya aktarılamadı. Geri alınması gereken ürün/saha işlemleri veya uygun olmayan " +
                    "sandık tahsisleri bulunuyor. İşlemleri geri alıp tahsisleri kontrol ederek tekrar deneyin.";

                return Result<ProjeDto>.Failure(hataMesaji, 409, dogrulama.Engeller);
            }

            var adaylarBySandik = dogrulama.Adaylar
                .GroupBy(a => a.SandikId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var sandikTaslaklari = new List<EksikSahaSandikDto>();

            foreach (var sandik in sandiklar)
            {
                var urunler = adaylarBySandik
                    .GetValueOrDefault(sandik.Id, new List<SandikBazliSahaAktarimAdayi>())
                    .Select(aday => new EksikSahaUrunDto
                    {
                        CekiSatiriId = aday.CekiSatiriId,
                        KaynakProjeId = request.KaynakProjeId,
                        KaynakSandikId = sandik.Id,
                        Miktar = aday.Miktar,
                        Aciklama = $"{SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix} {sandik.SandikNo}"
                    })
                    .ToList();

                if (urunler.Count == 0)
                    continue;

                sandikTaslaklari.Add(new EksikSahaSandikDto
                {
                    SandikNo = sandik.SandikNo,
                    SandikIsmi = sandik.Ad,
                    En = sandik.En,
                    Boy = sandik.Boy,
                    Yukseklik = sandik.Yukseklik,
                    NetKg = sandik.NetKg,
                    GrossKg = sandik.GrossKg,
                    Urunler = urunler
                });
            }

            if (sandikTaslaklari.Count == 0)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda sahaya aktarılabilecek kalan ürün bulunamadı.");

            var sandikNolari = sandikTaslaklari.Select(s => s.SandikNo).ToList();
            var aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? $"Kaynak proje {kaynakProje.ProjeNo} sandıkları sahaya aktarıldı: {string.Join(", ", sandikNolari)}"
                : request.Aciklama.Trim();

            return await _mediator.Send(new EksiklerdenSahaProjesiOlusturCommand
            {
                KaynakProjeId = request.KaynakProjeId,
                HedefSahaProjeId = request.HedefSahaProjeId,
                ProjeNo = request.ProjeNo,
                Aciklama = aciklama,
                Sandiklar = sandikTaslaklari
            }, cancellationToken);
        }

        private static int ExtractNumber(string value)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }

        private sealed class SandikBazliSahaAktarimRollbackException : Exception
        {
            public SandikBazliSahaAktarimRollbackException(Result<ProjeDto> result)
                : base(result.Error?.Message ?? "Sandik bazli saha aktarimi geri alindi.")
            {
                Result = result;
            }

            public Result<ProjeDto> Result { get; }
        }
    }
}
