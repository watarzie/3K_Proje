using _3K.Application.Common;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>Saha aktarım motorunun reddini çağıranın yanıt tipine durum kodu ve madde listesiyle aynen taşır.</summary>
internal static class SahaAktarimSonucEslemesi
{
    public static Result<T> Basarisiz<T>(Result kaynak)
    {
        var mesaj = kaynak.Error?.Message ?? "Saha aktarımı tamamlanamadı.";
        return kaynak.Error?.Issues is { } maddeler
            ? Result<T>.Failure(mesaj, kaynak.StatusCode, maddeler)
            : Result<T>.Failure(mesaj, kaynak.StatusCode);
    }
}
