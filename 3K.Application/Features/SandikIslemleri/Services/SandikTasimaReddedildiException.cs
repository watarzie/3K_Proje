using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Services;

// Result.Failure tek başına UnitOfWork transaction'ını geri almaz.
internal sealed class SandikTasimaReddedildiException(Result sonuc) : Exception(sonuc.Error?.Message)
{
    public Result Sonuc { get; } = sonuc;
}
