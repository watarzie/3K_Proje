using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
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

        public SandiklardanSahaProjesiOlusturCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<ProjeDto>> Handle(SandiklardanSahaProjesiOlusturCommand request, CancellationToken cancellationToken)
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

            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await icerikRepo.FindAsync(i =>
                    sandikIds.Contains(i.SandikId) &&
                    i.CekiSatiriId.HasValue))
                .ToList();

            if (icerikler.Count == 0)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda sahaya aktarılabilecek ürün bulunamadı.");

            var cekiSatiriIds = icerikler
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = (await cekiSatiriRepo.FindAsync(cs =>
                    cekiSatiriIds.Contains(cs.Id) &&
                    !cs.KaynakCekiSatiriId.HasValue))
                .ToDictionary(cs => cs.Id);

            if (satirlar.Count == 0)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda normal proje kaynaklı aktarılabilir ürün bulunamadı.");

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiIds = satirlar.Values.Select(s => s.CekiId).Distinct().ToList();
            var cekiler = (await cekiRepo.FindAsync(c =>
                    cekiIds.Contains(c.Id) &&
                    c.ProjeId == request.KaynakProjeId))
                .ToDictionary(c => c.Id);

            var projeDisiSatirVar = satirlar.Values.Any(s => !cekiler.ContainsKey(s.CekiId));
            if (projeDisiSatirVar)
                return Result<ProjeDto>.Failure("Seçilen sandıklarda kaynak proje ile eşleşmeyen ürünler var.");

            var aktifTamamlamaMap = await _sahaTamamlamaService.GetAktifTamamlamaMapAsync(satirlar.Keys, cancellationToken);
            var sandikTaslaklari = new List<EksikSahaSandikDto>();

            foreach (var sandik in sandiklar)
            {
                var urunler = icerikler
                    .Where(i => i.SandikId == sandik.Id && i.CekiSatiriId.HasValue && satirlar.ContainsKey(i.CekiSatiriId.Value))
                    .GroupBy(i => i.CekiSatiriId!.Value)
                    .Select(g =>
                    {
                        var kaynakSatir = satirlar[g.Key];
                        var planlanan = aktifTamamlamaMap.GetValueOrDefault(kaynakSatir.Id);
                        var aktarilabilirMiktar = Math.Max(kaynakSatir.KalanMiktar - planlanan, 0);

                        return aktarilabilirMiktar <= 0
                            ? null
                            : new EksikSahaUrunDto
                            {
                                CekiSatiriId = kaynakSatir.Id,
                                KaynakProjeId = request.KaynakProjeId,
                                Miktar = aktarilabilirMiktar,
                                Aciklama = $"Kaynak sandık: {sandik.SandikNo}"
                            };
                    })
                    .Where(u => u != null)
                    .Select(u => u!)
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
    }
}
