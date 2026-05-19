using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: Rol + Menü tabanlı yetkilendirme.
    /// ISecuredRequest implemente eden Command/Query'lerde kullanıcının rollerini kontrol eder.
    /// RequiredMenuKod belirtilmişse menü bazlı yetki kontrolü de yapılır.
    /// Yetki yoksa Exception fırlatmaz; Result.Failure döner.
    /// </summary>
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRolService _rolService;

        public AuthorizationBehavior(ICurrentUserService currentUserService, IRolService rolService)
        {
            _currentUserService = currentUserService;
            _rolService = rolService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Eğer istek ISecuredRequest değilse, doğrudan geç
            if (request is not ISecuredRequest securedRequest)
            {
                return await next();
            }

            // Kullanıcı authenticate değilse
            if (!_currentUserService.IsAuthenticated)
            {
                return CreateFailureResult("Oturum açmanız gerekiyor.", 401);
            }

            // Menu kodu varsa hardcoded rol listesi devreye girmez; rol yonetimi tablosu karar verir.
            var menuKod = securedRequest.RequiredMenuKod;
            if (!string.IsNullOrEmpty(menuKod))
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                    return CreateFailureResult("Kullanıcı bilgisi alınamadı.", 401);

                // Query'ler icin R/W, command'ler icin W yetkisi kontrol edilir.
                var requiredYetkiTipi = GetRequiredYetkiTipi(typeof(TRequest));
                var hasMenuPermission = await _rolService.HasUserPermissionAsync(userId.Value, menuKod, requiredYetkiTipi, cancellationToken);
                if (!hasMenuPermission)
                {
                    return CreateFailureResult("Bu modül için yetkiniz bulunmuyor.", 403);
                }
            }
            else
            {
                // Geriye donuk uyumluluk: Menu kodu olmayan eski/ozel isteklerde rol listesi fallback olarak calisir.
                var requiredRoles = securedRequest.RequiredRoles;
                if (requiredRoles != null && requiredRoles.Length > 0)
                {
                    var userRoles = _currentUserService.Roles;
                    var hasRequiredRole = requiredRoles.Any(role =>
                        userRoles.Any(userRole => string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase)));

                    if (!hasRequiredRole)
                    {
                        return CreateFailureResult("Bu işlem için yetkiniz bulunmuyor.", 403);
                    }
                }
            }

            return await next();
        }

        private static YetkiTipi GetRequiredYetkiTipi(Type requestType)
        {
            return requestType.Name.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
                ? YetkiTipi.R
                : YetkiTipi.W;
        }

        /// <summary>
        /// TResponse tipinden uygun Failure Result'ı üretir.
        /// TResponse = Result ise Result.Failure, TResponse = Result&lt;T&gt; ise Result&lt;T&gt;.Failure döner.
        /// </summary>
        private static TResponse CreateFailureResult(string message, int code)
        {
            var responseType = typeof(TResponse);

            // TResponse doğrudan Result ise
            if (responseType == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(message, code);
            }

            // TResponse generic Result<T> ise (Reflection ile Failure metodu çağırılır)
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = responseType.GetMethod("Failure", new[] { typeof(string), typeof(int) });
                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { message, code })!;
                }
            }

            // Result pattern kullanmayan eski tip komutlar için fallback
            // Bu durum idealde olmamalı — tüm komutlar Result dönmeli
            throw new UnauthorizedAccessException(message);
        }
    }
}

