using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public sealed class GetTopluTalepFormuQueryHandler(IUnitOfWork unitOfWork, ITalepFormuRaporService talepFormu)
        : IRequestHandler<GetTopluTalepFormuQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetTopluTalepFormuQuery request, CancellationToken cancellationToken)
        {
            var validation = await new Validators.GetTopluTalepFormuQueryValidator().ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<DosyaIcerigi>.Failure(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)));

            var projeIds = request.ProjeIds.ToList();
            var projeMap = (await unitOfWork.GetRepository<Proje>().FindAsync(proje => projeIds.Contains(proje.Id)))
                .ToDictionary(proje => proje.Id, proje => new { proje.ProjeNo, proje.ProjeTipiId });
            cancellationToken.ThrowIfCancellationRequested();

            var bulunamayanIds = projeIds.Where(id => !projeMap.ContainsKey(id)).ToList();
            if (bulunamayanIds.Count > 0)
                return Result<DosyaIcerigi>.Failure($"Şu projeler bulunamadı: {string.Join(", ", bulunamayanIds)}.", 404);

            var kapsamaUymayanlar = projeMap.Where(proje => proje.Value.ProjeTipiId != (int)request.ProjeTipi)
                .Select(proje => $"{proje.Value.ProjeNo} ({proje.Key})").ToList();
            if (kapsamaUymayanlar.Count > 0)
                return Result<DosyaIcerigi>.Failure($"Seçilen talep formu kapsamına uymayan projeler: {string.Join(", ", kapsamaUymayanlar)}.", 403);

            var excel = await talepFormu.TopluTalepFormuExcelAsync(projeIds, cancellationToken);
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(excel, $"Talep_Formu_{RaporDosyaAdi.TarihSaat()}.xlsx"));
        }
    }
}
