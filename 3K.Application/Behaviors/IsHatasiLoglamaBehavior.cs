using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
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
    /// 500 zaten orada Error olarak loglandığı için tekrar yazılmaz; başarılı istekte ek iş yapılmaz.
    /// İstek içeriği loglanmaz: yalnız istek adı, kullanıcı ve sayısal kimlik alanları (…Id, …Idler)
    /// yazılır. Kimliği doğrulanmamış (public) isteklerde (giriş, 2FA) kimlik alanı da yazılmaz.
    /// </summary>
    public class IsHatasiLoglamaBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const int AzamiListeElemani = 10;
        private const int AzamiSorunUzunlugu = 600;

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> KimlikAlanlari = new();

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
                    "İş hatası {StatusCode} · {RequestName} · Kullanıcı {UserId} ({UserName}) · {Kimlikler} · \"{HataMesaji}\"{Sorunlar}",
                    hata.Code,
                    typeof(TRequest).Name,
                    _currentUser.UserId?.ToString() ?? "-",
                    _currentUser.KullaniciAdi ?? "-",
                    request is ISecuredRequest ? KimlikleriYaz(request) : "-",
                    hata.Message,
                    SorunlariYaz(hata.Issues));
            }

            return response;
        }

        /// <summary>
        /// Yalnız tam sayı kimlikler okunur (int, int? ve int listeleri); metin, tutar, parola gibi
        /// alanlar hiçbir zaman yazılmaz. Tip başına alan listesi bir kez hesaplanır.
        /// </summary>
        private static string KimlikleriYaz(TRequest request)
        {
            var alanlar = KimlikAlanlari.GetOrAdd(request.GetType(), tip => tip
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 && KimlikAlaniMi(p))
                .ToArray());

            var parcalar = new List<string>();
            foreach (var alan in alanlar)
            {
                var deger = alan.GetValue(request);
                switch (deger)
                {
                    case int sayi:
                        parcalar.Add($"{alan.Name}={sayi}");
                        break;
                    case IEnumerable<int> liste:
                        var elemanlar = liste.Take(AzamiListeElemani + 1).ToList();
                        if (elemanlar.Count == 0) break;
                        var metin = string.Join(",", elemanlar.Take(AzamiListeElemani));
                        parcalar.Add($"{alan.Name}=[{metin}{(elemanlar.Count > AzamiListeElemani ? ",…" : "")}]");
                        break;
                }
            }

            return parcalar.Count == 0 ? "-" : string.Join(", ", parcalar);
        }

        private static bool KimlikAlaniMi(PropertyInfo alan)
        {
            var tip = alan.PropertyType;
            var sayisal = tip == typeof(int) || tip == typeof(int?);
            var sayiListesi = tip != typeof(string) && typeof(IEnumerable<int>).IsAssignableFrom(tip);
            return (sayisal && alan.Name.EndsWith("Id", StringComparison.Ordinal)) ||
                   (sayiListesi && (alan.Name.EndsWith("Idler", StringComparison.Ordinal) ||
                                    alan.Name.EndsWith("Ids", StringComparison.Ordinal)));
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
