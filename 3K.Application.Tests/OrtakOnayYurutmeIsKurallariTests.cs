using System.Reflection;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public class OrtakOnayYurutmeIsKurallariTests
{
    [Theory]
    [InlineData("yetki", 403)][InlineData("sonuclanmis", 409)][InlineData("yarisiKaybetti", 409)]
    public async Task KararYetkisiVeyaSahiplenmeYoksaKomutCalistirilmaz(string reason, int status)
    {
        var data = new Fixture();
        data.YetkiVar = reason != "yetki";
        data.KararAlinabilir = reason != "yarisiKaybetti";
        if (reason == "sonuclanmis") data.Pending.Durum = OnayDurumu.Onaylandi;
        Assert.Equal(status, (await data.Run()).StatusCode);
        Assert.Equal(0, data.Executed);
        Assert.Null(data.Completed);
        Assert.Equal(reason == "yarisiKaybetti" ? 1 : 0, data.Claimed);
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task KararVeYurutmeAyriKaydedilir_IsKuraliBasarisizsaBasariliSayilmaz(bool success)
    {
        var data = new Fixture { Downstream = success ? Result.Success() : Result.Failure("tahsis yetersiz", 409) };
        var result = await data.Run();
        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(OnayDurumu.Onaylandi, data.Pending.Durum);
        Assert.Equal(success ? OnayCalistirmaDurumu.Basarili : OnayCalistirmaDurumu.Basarisiz, data.Completed);
        Assert.Equal(1, data.Executed);
        Assert.True(data.ExecutionWasApproved);
        Assert.False(data.Execution.IsExecutingApprovedCommand);
        Assert.Equal(1, data.Claimed);
        if (!success) Assert.Contains("tahsis yetersiz", result.Error!.Message);
    }

    [Theory]
    [InlineData("kod")][InlineData("referans")][InlineData("proje")][InlineData("payload")][InlineData("komutTipi")]
    public async Task KayitliKomutVeOnayBaglamiUyusmuyorsaFizikselIslemeGecilmez(string tamper)
    {
        var data = new Fixture();
        switch (tamper)
        {
            case "kod": data.Pending.IslemKodu = "BASKA_ISLEM"; break;
            case "referans": data.Pending.ReferansId = 999; break;
            case "proje": data.Pending.ProjeId = 999; break;
            case "payload": data.Pending.PayloadJson = "{"; break;
            case "komutTipi": data.Pending.CommandType = typeof(OrtakYetkiOnayIsKurallariTests.AyarliCommand).AssemblyQualifiedName!; break;
        }
        var result = await data.Run();
        Assert.False(result.IsSuccess);
        Assert.Equal(0, data.Executed);
        Assert.Equal(OnayDurumu.Onaylandi, data.Pending.Durum);
        Assert.Equal(OnayCalistirmaDurumu.Basarisiz, data.Completed);
    }

    private sealed class Fixture
    {
        public OnayBekleyenIslem Pending { get; }
        public ApprovalExecutionContext Execution { get; } = new();
        public bool YetkiVar { get; set; } = true;
        public bool KararAlinabilir { get; set; } = true;
        public Result Downstream { get; set; } = Result.Success();
        public int Executed { get; private set; }
        public int Claimed { get; private set; }
        public bool ExecutionWasApproved { get; private set; }
        public OnayCalistirmaDurumu? Completed { get; private set; }

        public Fixture()
        {
            var command = new SandikKilidiAcCommand { SandikId = 10 };
            var reference = command.GetApprovalReference();
            Pending = new() { Id = 20, TalepEdenKullaniciId = 8, Durum = OnayDurumu.Bekliyor,
                CommandType = command.GetType().AssemblyQualifiedName!, PayloadJson = JsonSerializer.Serialize(command),
                IslemKodu = command.GetApprovalOperationCode(), ProjeId = reference.ProjeId,
                ReferansTipi = reference.ReferansTipi, ReferansId = reference.ReferansId };
        }

        public Task<Result> Run()
        {
            var repo = Proxy<IOnayIslemRepository>((method, args) =>
            {
                if (method == nameof(IOnayIslemRepository.GetByIdNoTrackingAsync)) return Task.FromResult<OnayBekleyenIslem?>(Pending);
                if (method == nameof(IOnayIslemRepository.OnayKarariniAlVeCalistirmayiBaslatAsync))
                {
                    Claimed++;
                    if (KararAlinabilir) Pending.Durum = OnayDurumu.Onaylandi;
                    return Task.FromResult(KararAlinabilir);
                }
                if (method == nameof(IOnayIslemRepository.CalistirmayiTamamlaAsync))
                { Completed = (OnayCalistirmaDurumu)args![2]!; return Task.FromResult(true); }
                throw new NotSupportedException(method);
            });
            var mediator = Proxy<IMediator>((method, _) =>
            {
                if (method != nameof(IMediator.Send)) throw new NotSupportedException(method);
                Executed++; ExecutionWasApproved = Execution.IsExecutingApprovedCommand;
                return Task.FromResult<object?>(Downstream);
            });
            var yetki = Proxy<IOnayYetkiService>((method, args) =>
            {
                if (method != nameof(IOnayYetkiService.KullaniciIslemOnaylayabilirMiAsync)) throw new NotSupportedException(method);
                Assert.Equal(7, args![0]); Assert.Equal(Pending.IslemKodu, args[1]); Assert.Equal(8, args[2]);
                return Task.FromResult(YetkiVar);
            });
            var sse = Proxy<ISseNotifier>((method, _) => method == nameof(ISseNotifier.BroadcastApprovalUpdateAsync)
                ? Task.CompletedTask : throw new NotSupportedException(method));
            return new IslemOnaylaCommandHandler(repo, mediator, new OrtakUser(), Execution, yetki, sse,
                NullLogger<IslemOnaylaCommandHandler>.Instance).Handle(new() { OnayBekleyenIslemId = 20 }, default);
        }
    }

    private static T Proxy<T>(Func<string, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, CallbackProxy>();
        ((CallbackProxy)(object)proxy).Callback = invoke;
        return proxy;
    }

    public class CallbackProxy : DispatchProxy
    {
        public Func<string, object?[]?, object?> Callback { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Callback(method!.Name, args);
    }
}
