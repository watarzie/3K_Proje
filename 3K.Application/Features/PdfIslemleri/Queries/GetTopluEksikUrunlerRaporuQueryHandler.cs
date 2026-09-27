using System.IO.Compression;
using System.Text;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public sealed class GetTopluEksikUrunlerRaporuQueryHandler(IUnitOfWork unitOfWork, IPdfService pdfService)
        : IRequestHandler<GetTopluEksikUrunlerRaporuQuery, Result<DosyaIcerigi>>
    {
        private const long MaksimumToplamHamRaporBoyutu = 100L * 1024 * 1024;

        public async Task<Result<DosyaIcerigi>> Handle(
            GetTopluEksikUrunlerRaporuQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await new Validators.GetTopluEksikUrunlerRaporuQueryValidator()
                .ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<DosyaIcerigi>.Failure(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)));

            var projeIds = request.ProjeIds.ToList();

            cancellationToken.ThrowIfCancellationRequested();
            var projeMap = (await unitOfWork.GetRepository<Proje>().FindAsync(proje => projeIds.Contains(proje.Id)))
                .ToDictionary(proje => proje.Id, proje => new { proje.ProjeNo, proje.ProjeTipiId });
            cancellationToken.ThrowIfCancellationRequested();

            var bulunamayanIds = projeIds.Where(id => !projeMap.ContainsKey(id)).ToList();
            if (bulunamayanIds.Count > 0)
            {
                return Result<DosyaIcerigi>.Failure(
                    $"Şu projeler bulunamadı: {string.Join(", ", bulunamayanIds)}.", 404);
            }

            var kapsamaUymayanProjeler = projeMap
                .Where(proje => proje.Value.ProjeTipiId != (int)request.ProjeTipi)
                .Select(proje => $"{proje.Value.ProjeNo} ({proje.Key})")
                .ToList();
            if (kapsamaUymayanProjeler.Count > 0)
            {
                return Result<DosyaIcerigi>.Failure(
                    $"Seçilen rapor kapsamına uymayan projeler: {string.Join(", ", kapsamaUymayanProjeler)}.", 403);
            }

            var pdfMi = request.DosyaTuru == EksikUrunlerRaporDosyaTuru.Pdf;
            var uzanti = pdfMi ? "pdf" : "xlsx";
            var raporAdi = RaporDosyaAdi.EksikRaporAdi((int)request.ProjeTipi);

            using var zipStream = new MemoryStream();
            long toplamHamRaporBoyutu = 0;
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var index = 0; index < projeIds.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var projeId = projeIds[index];
                    var proje = projeMap[projeId];
                    var raporBytes = pdfMi
                        ? await pdfService.EksikUrunlerRaporuPdfOlusturAsync(projeId, cancellationToken)
                        : await pdfService.EksikUrunlerRaporuExcelOlusturAsync(projeId, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (raporBytes.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"{proje.ProjeNo} projesi için oluşturulan eksik raporu boş döndü.");
                    }

                    if (raporBytes.LongLength > MaksimumToplamHamRaporBoyutu - toplamHamRaporBoyutu)
                    {
                        return Result<DosyaIcerigi>.Failure(
                            "Seçilen projelerin toplam ham rapor boyutu 100 MiB sınırını aşıyor. Daha az proje seçerek tekrar deneyin.");
                    }

                    toplamHamRaporBoyutu += raporBytes.LongLength;

                    var entryAdi = $"{index + 1:D2}_{GuvenliDosyaParcasiOlustur(proje.ProjeNo)}_{projeId}_{raporAdi}.{uzanti}";
                    var entry = archive.CreateEntry(entryAdi, CompressionLevel.Fastest);

                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(raporBytes, cancellationToken);
                }
            }

            var format = pdfMi ? "PDF" : "Excel";
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Zip(zipStream.ToArray(),
                $"TopluEksikRaporlari_{format}_{RaporDosyaAdi.TarihSaat()}.zip"));
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
