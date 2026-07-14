using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.Events;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands;

public sealed class YedekCekiYukleCommandHandler
    : IRequestHandler<YedekCekiYukleCommand, Result<CekiYuklemeResultDto>>
{
    private readonly IYedekCekiImportService _importService;
    private readonly IPublisher _publisher;

    public YedekCekiYukleCommandHandler(
        IYedekCekiImportService importService,
        IPublisher publisher)
    {
        _importService = importService;
        _publisher = publisher;
    }

    public async Task<Result<CekiYuklemeResultDto>> Handle(
        YedekCekiYukleCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var import = await _importService.ImportAsync(
                request.ExcelDosya,
                request.DosyaAdi,
                request.KullaniciId,
                cancellationToken);

            var result = new CekiYuklemeResultDto
            {
                CekiId = import.CekiId,
                ProjeId = import.ProjeId,
                ProjeNo = import.ProjeNo,
                SatirSayisi = import.SatirSayisi,
                SandikSayisi = import.SandikSayisi,
                Mesaj = $"{import.SatirSayisi} yedek ürün satırı okundu ve 1 numaralı sandığa tahsis edildi."
            };

            await _publisher.Publish(new CekiDosyasiYuklendiEvent(
                import.CekiId,
                import.ProjeId,
                import.ProjeNo,
                request.DosyaAdi,
                request.KullaniciId,
                RevizyonMu: false,
                SatirSayisi: import.SatirSayisi,
                SandikSayisi: import.SandikSayisi,
                ProjeTipiId: (int)ProjeTipi.Yedek), CancellationToken.None);

            return Result<CekiYuklemeResultDto>.Success(result);
        }
        catch (CekiImportValidationException exception)
        {
            return Result<CekiYuklemeResultDto>.Failure(exception.Message, 400);
        }
        catch (CekiImportConflictException exception)
        {
            return Result<CekiYuklemeResultDto>.Failure(exception.Message, 409);
        }
    }
}
