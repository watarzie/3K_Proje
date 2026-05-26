using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// Central authorization pipeline.
    /// ISecuredRequest only marks the request; the required menu comes from the
    /// active UI context and the permission decision is read from RolYetkileri.
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
            if (request is not ISecuredRequest)
                return await next();

            if (!_currentUserService.IsAuthenticated)
                return CreateFailureResult("Oturum açmanız gerekiyor.", 401);

            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return CreateFailureResult("Kullanıcı bilgisi alınamadı.", 401);

            var menuKod = _currentUserService.MenuKod;
            if (string.IsNullOrWhiteSpace(menuKod))
                return CreateFailureResult("Yetki bağlamı alınamadı.", 403);

            var requiredYetkiTipi = GetRequiredYetkiTipi(typeof(TRequest));
            var hasPermission = await _rolService.HasUserPermissionAsync(
                userId.Value,
                menuKod,
                requiredYetkiTipi,
                cancellationToken);

            if (!hasPermission)
                return CreateFailureResult("Bu modül için yetkiniz bulunmuyor.", 403);

            return await next();
        }

        private static YetkiTipi GetRequiredYetkiTipi(Type requestType)
        {
            return requestType.Name.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
                ? YetkiTipi.R
                : YetkiTipi.W;
        }

        private static TResponse CreateFailureResult(string message, int code)
        {
            var responseType = typeof(TResponse);

            if (responseType == typeof(Result))
                return (TResponse)(object)Result.Failure(message, code);

            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = responseType.GetMethod("Failure", new[] { typeof(string), typeof(int) });
                if (failureMethod != null)
                    return (TResponse)failureMethod.Invoke(null, new object[] { message, code })!;
            }

            var isSuccessProperty = responseType.GetProperty("IsSuccess");
            var messageProperty = responseType.GetProperty("Message");
            if (responseType.GetConstructor(Type.EmptyTypes) != null &&
                isSuccessProperty?.CanWrite == true &&
                isSuccessProperty.PropertyType == typeof(bool) &&
                messageProperty?.CanWrite == true &&
                messageProperty.PropertyType == typeof(string))
            {
                var response = Activator.CreateInstance(responseType)!;
                isSuccessProperty.SetValue(response, false);
                messageProperty.SetValue(response, message);
                return (TResponse)response;
            }

            throw new UnauthorizedAccessException(message);
        }
    }
}
