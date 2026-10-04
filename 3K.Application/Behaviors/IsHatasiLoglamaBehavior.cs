using System.Collections;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: başarısız sonuçlanan isteği (iş kuralı, doğrulama, yetki reddi,
    /// bulunamadı, çakışma) tek satır Warning olarak loglar. Pipeline'ın en dışındadır; böylece
    /// <see cref="UnhandledExceptionBehavior{TRequest,TResponse}"/>'ın istisnadan çevirdiği retler de görülür.
    /// 500 zaten orada Error olarak (girdisiyle) loglandığı için tekrar yazılmaz; başarılı istekte ek iş yapılmaz.
    /// Satır istek adını, kullanıcıyı, hata mesajını ve <see cref="IstekGirdisi"/> ile maskelenmiş girdiyi taşır:
    /// parola, token ve doğrulama kodları "***" olur, dosya içeriği yazılmaz.
    /// </summary>
    public class IsHatasiLoglamaBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const int AzamiSorunUzunlugu = 600;

        private readonly ILogger<IsHatasiLoglamaBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUser;

        public IsHatasiLoglamaBehavior(
            ILogger<IsHatasiLoglamaBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUser)
        {
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next();

            if (response is Result { IsSuccess: false, Error: { } hata } && hata.Code != 500)
            {
                _logger.LogWarning(
                    "İş hatası {StatusCode} · {RequestName} · Kullanıcı {UserId} ({UserName}) · \"{HataMesaji}\"{Sorunlar} · Girdi: {Girdi}",
                    hata.Code,
                    typeof(TRequest).Name,
                    _currentUser.UserId?.ToString() ?? "-",
                    _currentUser.KullaniciAdi ?? "-",
                    hata.Message,
                    SorunlariYaz(hata.Issues),
                    IstekGirdisi.Yaz(request));
            }

            return response;
        }

        /// <summary>Doğrulama/revizyon sorun listesi kısaltılarak tek satıra yazılır.</summary>
        private static string SorunlariYaz(object? sorunlar)
        {
            if (sorunlar == null || (sorunlar is ICollection koleksiyon && koleksiyon.Count == 0))
                return string.Empty;

            string metin;
            try
            {
                metin = JsonSerializer.Serialize(sorunlar);
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }

            return " · Sorunlar: " + (metin.Length > AzamiSorunUzunlugu ? metin[..AzamiSorunUzunlugu] + "…" : metin);
        }
    }
}
