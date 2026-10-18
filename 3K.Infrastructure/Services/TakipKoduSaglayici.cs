using _3K.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// İsteğin takip kodunu okur: CorrelationIdMiddleware X-Correlation-ID değerini HttpContext.TraceIdentifier'a da
    /// yazar. Değer istemciden gelebildiği için kalıcı kayda yalnız harf, rakam ve tireden oluşan en fazla 64 karakterlik
    /// kod verilir (ön yüzdeki gösterim kuralıyla aynı); HTTP isteği yoksa veya biçim uymuyorsa null.
    /// </summary>
    public class TakipKoduSaglayici(IHttpContextAccessor httpContextAccessor) : ITakipKoduSaglayici
    {
        public string? TakipKodu =>
            httpContextAccessor.HttpContext?.TraceIdentifier is { Length: > 0 and <= 64 } kod &&
            kod.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
                ? kod
                : null;
    }
}
