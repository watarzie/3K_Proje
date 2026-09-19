using System.IO.Compression;
using System.Text;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public sealed class GetTopluEksikUrunlerRaporuQueryHandler
        : IRequestHandler<GetTopluEksikUrunlerRaporuQuery, Result<byte[]>>
    {
        private const long MaksimumToplamHamRaporBoyutu = 100L * 1024 * 1024;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPdfService _pdfService;
        private readonly ICurrentUserService _currentUser;
        private readonly IRolService _rolService;

        public GetTopluEksikUrunlerRaporuQueryHandler(
            IUnitOfWork unitOfWork,
            IPdfService pdfService,
            ICurrentUserService currentUser,
            IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
            _currentUser = currentUser;
            _rolService = rolService;
        }

        public async Task<Result<byte[]>> Handle(
            GetTopluEksikUrunlerRaporuQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await new Validators.GetTopluEksikUrunlerRaporuQueryValidator()
                .ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<byte[]>.Failure(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)));

            var projeIds = request.ProjeIds.ToList();

            cancellationToken.ThrowIfCancellationRequested();
            var projeEntities = await _unitOfWork.GetRepository<Proje>()
                .FindAsync(proje => projeIds.Contains(proje.Id));
            cancellationToken.ThrowIfCancellationRequested();

            var projeler = projeEntities
                .Select(proje => new
                {
                    proje.Id,
                    proje.ProjeNo,
                    proje.ProjeTipiId
                })
                .ToList();

            var projeMap = projeler.ToDictionary(proje => proje.Id);
            var bulunamayanIds = projeIds
                .Where(id => !projeMap.ContainsKey(id))
                .ToList();

            if (bulunamayanIds.Count > 0)
            {
                return Result<byte[]>.Failure(
                    $"Şu projeler bulunamadı: {string.Join(", ", bulunamayanIds)}.",
                    404);
            }

            var kapsamaUymayanProjeler = projeler
                .Where(proje => proje.ProjeTipiId != (int)request.ProjeTipi)
                .Select(proje => $"{proje.ProjeNo} ({proje.Id})")
                .ToList();

            if (kapsamaUymayanProjeler.Count > 0)
            {
                return Result<byte[]>.Failure(
                    $"Seçilen rapor kapsamına uymayan projeler: {string.Join(", ", kapsamaUymayanProjeler)}.", 403);
            }

            // Önce bütün seçim doğrulanır; bir proje yetkisizse hiçbir rapor üretilmez.
            foreach (var proje in projeler)
            {
                if (!await EksikUrunlerRaporYetkisi.YetkiliMiAsync(
                    (ProjeTipi)proje.ProjeTipiId, _currentUser, _rolService, cancellationToken))
                    return Result<byte[]>.Failure($"{proje.ProjeNo} projesinin eksik raporunu okuma yetkiniz bulunmuyor.", 403);
            }

            var uzanti = request.DosyaTuru == EksikUrunlerRaporDosyaTuru.Pdf
                ? "pdf"
                : "xlsx";

            using var zipStream = new MemoryStream();
            long toplamHamRaporBoyutu = 0;
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var index = 0; index < projeIds.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var projeId = projeIds[index];
                    var proje = projeMap[projeId];
                    var raporBytes = request.DosyaTuru == EksikUrunlerRaporDosyaTuru.Pdf
                        ? await _pdfService.EksikUrunlerRaporuPdfOlusturAsync(projeId, cancellationToken)
                        : await _pdfService.EksikUrunlerRaporuExcelOlusturAsync(projeId, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (raporBytes.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"{proje.ProjeNo} projesi için oluşturulan eksik raporu boş döndü.");
                    }

                    if (raporBytes.LongLength > MaksimumToplamHamRaporBoyutu - toplamHamRaporBoyutu)
                    {
                        return Result<byte[]>.Failure(
                            "Seçilen projelerin toplam ham rapor boyutu 100 MiB sınırını aşıyor. Daha az proje seçerek tekrar deneyin.");
                    }

                    toplamHamRaporBoyutu += raporBytes.LongLength;

                    var guvenliProjeNo = GuvenliDosyaParcasiOlustur(proje.ProjeNo);
                    var raporAdi = request.ProjeTipi == ProjeTipi.Saha ? "SevkSonrasiEksikRaporu" : "EksikRaporu";
                    var entryAdi = $"{index + 1:D2}_{guvenliProjeNo}_{projeId}_{raporAdi}.{uzanti}";
                    var entry = archive.CreateEntry(entryAdi, CompressionLevel.Fastest);

                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(raporBytes, cancellationToken);
                }
            }

            return Result<byte[]>.Success(zipStream.ToArray());
        }

        private static string GuvenliDosyaParcasiOlustur(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Proje";

            var builder = new StringBuilder();
            var sonKarakterAltCizgi = false;

            foreach (var karakter in value.Trim().Normalize(NormalizationForm.FormKC))
            {
                if (char.IsLetterOrDigit(karakter) || karakter is '-' or '_')
                {
                    builder.Append(karakter);
                    sonKarakterAltCizgi = false;
                    continue;
                }

                if (!sonKarakterAltCizgi)
                {
                    builder.Append('_');
                    sonKarakterAltCizgi = true;
                }
            }

            var sonuc = builder.ToString().Trim('_');
            if (sonuc.Length == 0)
                return "Proje";

            return sonuc.Length <= 80 ? sonuc : sonuc[..80];
        }
    }
}
