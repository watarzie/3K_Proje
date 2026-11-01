using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Rapor dosyaları.</summary>
public sealed class FinansRaporController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("raporlar/onizleme/{tur}")]
    public async Task<ActionResult> Onizleme(
        string tur,
        [FromQuery] FinansFilterRequest request,
        [FromQuery] FinansIsRaporuKapsami kapsam = FinansIsRaporuKapsami.Birlesik,
        [FromQuery] FinansGiderRaporuGruplamasi gruplama = FinansGiderRaporuGruplamasi.Duz,
        CancellationToken cancellationToken = default)
        => (await Mediator.Send(new FinansRaporOnizlemeQuery { Tur = tur, Kapsam = kapsam, Gruplama = gruplama, Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    [HttpGet("raporlar/ozet/{tur}/{format}")]
    public async Task<ActionResult> OzetRapor(string tur, string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => await FileResultAsync(new FinansOzetRaporuDosyaQuery { Tur = tur, Format = format, Filtre = request.ToModel() }, cancellationToken);

    [HttpGet("raporlar/isler/{format}")]
    public async Task<ActionResult> IsRaporu(
        string format,
        [FromQuery] FinansFilterRequest request,
        [FromQuery] FinansIsRaporuKapsami kapsam = FinansIsRaporuKapsami.Birlesik,
        CancellationToken cancellationToken = default)
        => await FileResultAsync(new FinansIsRaporuDosyaQuery { Format = format, Kapsam = kapsam, Filtre = request.ToModel() }, cancellationToken);

    [HttpGet("raporlar/giderler/{format}")]
    public async Task<ActionResult> GiderRaporu(
        string format,
        [FromQuery] FinansFilterRequest request,
        [FromQuery] FinansGiderRaporuGruplamasi gruplama = FinansGiderRaporuGruplamasi.Duz,
        CancellationToken cancellationToken = default)
        => await FileResultAsync(new FinansGiderRaporuDosyaQuery { Format = format, Gruplama = gruplama, Filtre = request.ToModel() }, cancellationToken);

    /// <summary>Finansal durum (gelir–gider) raporu: Kontrol Paneliyle aynı tutarlar, kırılımlar ve aylık trend.</summary>
    [HttpGet("raporlar/finansal-durum/{format}")]
    public async Task<ActionResult> FinansalDurumRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => await FileResultAsync(new FinansFinansalDurumRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

    [HttpGet("raporlar/alacaklar/{format}")]
    public async Task<ActionResult> AlacakRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => await FileResultAsync(new FinansAlacakRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

    [HttpGet("raporlar/siparis-durumu/{format}")]
    public async Task<ActionResult> SiparisDurumRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => await FileResultAsync(new FinansSiparisDurumRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

    [HttpGet("raporlar/aylik/{format}")]
    public async Task<ActionResult> AylikRapor(
        string format,
        [FromQuery] int yil,
        [FromQuery] int ay,
        [FromQuery] string[]? gruplar,
        CancellationToken cancellationToken)
        => await FileResultAsync(new FinansAylikRaporDosyaQuery
        {
            Format = format,
            Yil = yil,
            Ay = ay,
            Gruplar = gruplar ?? Array.Empty<string>()
        }, cancellationToken);

    private async Task<ActionResult> FileResultAsync(FinansQuery<DosyaIcerigi> query, CancellationToken cancellationToken)
    {
        return (await Mediator.Send(query, cancellationToken)).ToFileResult();
    }
}
