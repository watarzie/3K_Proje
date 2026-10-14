using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K_API.Extensions;

namespace _3K.Application.Tests;

/// <summary>
/// API yanıt sözleşmesi: ön yüz başarılı yanıtı JSON olarak çözer. ASP.NET düz string değeri text/plain yazdığı için
/// (tırnaksız "https://…") hiçbir istek Result&lt;string&gt; döndürmez; tek değer de nesne içinde döner.
/// </summary>
public sealed class ApiYanitSozlesmesiTests
{
    [Fact]
    public void HicbirIstek_DuzStringSonucDondurmez()
    {
        var stringDonenler = typeof(Result).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
                                                   i.GetGenericArguments()[0] == typeof(Result<string>)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(stringDonenler.Count == 0, "Result<string> dönen istekler: " + string.Join(", ", stringDonenler));
    }

    [Fact]
    public void FotografBaglantisiSonucu_JsonNesnesiOlarakYazilir()
    {
        var yanit = Result<ProjeFotografBaglantisiDto>.Success(new ProjeFotografBaglantisiDto { FotografBaglantisi = "https://foto.ornek.com/pa727" })
            .ToActionResult();

        var ok = Assert.IsType<OkObjectResult>(yanit);
        Assert.IsNotType<string>(ok.Value);
        Assert.Equal("""{"fotografBaglantisi":"https://foto.ornek.com/pa727"}""", JsonSerializer.Serialize(ok.Value, JsonSerializerOptions.Web));
    }
}
