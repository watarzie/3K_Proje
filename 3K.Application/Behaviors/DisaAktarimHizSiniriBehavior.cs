using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: yanıtı <c>Result&lt;DosyaIcerigi&gt;</c> olan her istek (PDF/Excel/etiket üretimi ve
    /// dosya indirme) <see cref="IDisaAktarimHizSiniri"/> ile kullanıcı başına sınırlanır; kota bitince handler çalışmaz
    /// ve 429 döner. Dosya ucunu yanıt tipi belirler: yeni rapor ucu attribute ya da rota adı gerektirmeden kapsanır.
    /// Yetki ve doğrulamadan sonra çalışır; reddedilen ya da geçersiz istek kotadan düşmez. Kullanıcısız (sunucu içi)
    /// çağrılar sınırlanmaz: HTTP'den gelen dosya isteği oturum ister. Davranış her istek tipi için kurulur;
    /// <see cref="IDisaAktarimHizSiniri"/> host tarafından kaydedilmelidir (API: <c>AddApiRateLimiting</c>), yoksa hiçbir
    /// istek çözülemez. Bağımlılık bilerek isteğe bağlı değildir: kayıt unutulursa sınır sessizce kapanmaz.
    /// </summary>
    public class DisaAktarimHizSiniriBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public const string Mesaj = "Kısa sürede çok fazla rapor veya dosya istendi. Lütfen bir dakika sonra tekrar deneyin.";

        private static readonly bool DosyaIstegi = typeof(TResponse) == typeof(Result<DosyaIcerigi>);

        private readonly IDisaAktarimHizSiniri _hizSiniri;
        private readonly ICurrentUserService _currentUser;

        public DisaAktarimHizSiniriBehavior(IDisaAktarimHizSiniri hizSiniri, ICurrentUserService currentUser)
        {
            _hizSiniri = hizSiniri;
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!DosyaIstegi || _currentUser.UserId is not { } kullaniciId || _hizSiniri.IzinVer(kullaniciId))
                return await next();

            return PipelineResultFactory.TryCreateFailure<TResponse>(Mesaj, 429, null, out var ret)
                ? ret
                : throw new InvalidOperationException("Dışa aktarım reddi Result olarak oluşturulamadı.");
        }
    }
}
