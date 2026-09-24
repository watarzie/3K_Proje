using System.Reflection;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Models;
using _3K_API.Controllers;

namespace _3K.Application.Tests;

public sealed class FinansOzelIsContractTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OzelIs_HamJson_Controller_Command_TalepEdenVeSerbestTuruKorur(bool includeRequester)
    {
        var fields = new Dictionary<string, object?>
        {
            ["isTuru"] = "Özel saha montajı", ["musteri"] = "Sentetik müşteri", ["isAdi"] = "Kurulum",
            ["miktar"] = 2m, ["birim"] = "Adet", ["isTarihi"] = "2026-09-19T00:00:00",
            ["hesaplamaYontemi"] = 2, ["raporGrubu"] = "Özel İş", ["birimFiyat"] = 500m,
            ["paraBirimi"] = "EUR", ["kdvOrani"] = 20m
        };
        if (includeRequester) { fields["talepEdenKisi"] = "Test kişi"; fields["talepEdenBolum"] = "Satın alma"; }
        var model = JsonSerializer.Deserialize<FinansOzelIsKaydetModel>(JsonSerializer.Serialize(fields), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var mediator = DispatchProxy.Create<IMediator, CaptureMediator>();
        var capture = (CaptureMediator)mediator;
        var result = await new FinansController(mediator).OzelIsOlustur(model, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        var command = Assert.IsType<FinansIsKaydiOlusturCommand>(capture.Request);
        Assert.Equal("Özel saha montajı", command.Model.OzelIsTuru);
        Assert.Equal(includeRequester ? "Test kişi" : null, command.Model.TalepEdenKisi);
        Assert.Equal(includeRequester ? "Satın alma" : null, command.Model.TalepEdenBolum);
        Assert.True((await new FinansIsKaydiOlusturCommandValidator().ValidateAsync(command)).IsValid);
    }

    [Fact]
    public void DetayManuelNetTutar_ParasalIzinsizNullKalir()
    {
        var json = JsonSerializer.SerializeToNode(new FinansIsKaydiModel { ManuelNetTutar = 1200m }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        FinansAlanProjeksiyonu.Uygula(json, AlanErisimYetkileri.Yok);
        Assert.Null(json!["manuelNetTutar"]);
    }

    public class CaptureMediator : DispatchProxy
    {
        public object? Request { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Request = args![0];
            return Task.FromResult(Result<FinansIsKaydiModel>.Success(new FinansIsKaydiModel()));
        }
    }
}
