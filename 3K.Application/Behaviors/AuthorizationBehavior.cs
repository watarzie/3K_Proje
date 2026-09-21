using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// Central authorization pipeline.
    /// ISecuredRequest marks the request; requirements are defined by server code.
    /// X-Menu-Kod is never an authorization source.
    /// </summary>
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRolService _rolService;
        private readonly IRequestMenuPermissionResolver? _permissionResolver;
        private readonly IApprovalExecutionContext? _approvalExecutionContext;

        public AuthorizationBehavior(ICurrentUserService currentUserService, IRolService rolService,
            IRequestMenuPermissionResolver? permissionResolver = null,
            IApprovalExecutionContext? approvalExecutionContext = null)
        {
            _currentUserService = currentUserService;
            _rolService = rolService;
            _permissionResolver = permissionResolver;
            _approvalExecutionContext = approvalExecutionContext;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (MenuAuthorizationExceptions.IsPublic(request) || MenuAuthorizationExceptions.IsServerInternal(request))
                return await next();

            if (request is not ISecuredRequest && !MenuAuthorizationExceptions.IsOwnAuthenticatedData(request))
                return CreateFailureResult("Bu işlem için sunucu yetki tanımı bulunamadı.", 403);

            if (!_currentUserService.IsAuthenticated)
                return CreateFailureResult("Oturum açmanız gerekiyor.", 401);

            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return CreateFailureResult("Kullanıcı bilgisi alınamadı.", 401);

            if (MenuAuthorizationExceptions.IsOwnAuthenticatedData(request))
                return await next();

            // Bu bağlam yalnız onay handler'ında kayıt/karar/meta veri kontrollerinden
            // sonra açılır. Onay yetkisi, talep edenin ekran yazma izniyle aynı değildir.
            var granularModule = request.GetType().Namespace?.StartsWith("_3K.Application.Features.AmbalajIslemleri", StringComparison.Ordinal) == true ||
                request.GetType().Namespace?.StartsWith("_3K.Application.Features.FinansIslemleri", StringComparison.Ordinal) == true;
            if (_approvalExecutionContext?.IsExecutingApprovedCommand == true && !granularModule &&
                request is IApprovalOperation && RequestMenuPermissionResolver.HasServerDefinition(request))
                return await next();

            if (_approvalExecutionContext?.IsExecutingApprovedCommand == true && granularModule)
            {
                if (_approvalExecutionContext.InitiatorUserId is not int initiator || initiator <= 0)
                    return CreateFailureResult("Onay işleminin başlatan kullanıcısı doğrulanamadı.", 403);
                userId = initiator;
            }

            // Üretim/finans kök erişimi ile bağımsız eylem izni birlikte gerekir.
            // Kökün kişisel açık reddi, elde kalmış alt izinle API'den aşılamaz.
            // Diğer modüllerin mevcut All/Any sözleşmelerine ek gereksinim koymayız.
            if (granularModule)
            {
                var root = request.GetType().Namespace!.StartsWith("_3K.Application.Features.AmbalajIslemleri", StringComparison.Ordinal)
                    ? YetkiKodlari.Ambalaj.Listele : YetkiKodlari.Finans.Modul;
                if (!await _rolService.HasUserPermissionAsync(userId.Value, root, YetkiTipi.R, cancellationToken))
                    return CreateFailureResult("Bu modüle erişim yetkiniz bulunmuyor.", 403);
            }

            var policy = _permissionResolver != null
                ? await _permissionResolver.ResolveAsync(request, cancellationToken)
                : DeclaredPermissions(request);

            if (policy.Groups.Count == 0)
                return CreateFailureResult("Bu işlem için sunucu yetki tanımı bulunamadı.", 403);

            foreach (var group in policy.Groups)
            {
                if (group.Requirements.Count == 0 || !Enum.IsDefined(group.Match) ||
                    group.Requirements.Any(x => string.IsNullOrWhiteSpace(x.MenuKod) ||
                        x.YetkiTipi is not (YetkiTipi.R or YetkiTipi.W)))
                    return CreateFailureResult("Geçersiz sunucu yetki tanımı.", 403);

                var anyGranted = false;
                // Aynı scoped DbContext: kontroller kasıtlı olarak sıralıdır.
                foreach (var requirement in group.Requirements.Distinct())
                {
                    var granted = await _rolService.HasUserPermissionAsync(userId.Value,
                        requirement.MenuKod, requirement.YetkiTipi, cancellationToken);
                    if (!granted && group.Match == MenuPermissionMatch.All)
                        return CreateFailureResult("Bu işlem için gerekli yetkileriniz bulunmuyor.", 403);
                    anyGranted |= granted;
                    if (granted && group.Match == MenuPermissionMatch.Any)
                        break;
                }
                if (!anyGranted)
                    return CreateFailureResult("Bu işlem için gerekli yetkileriniz bulunmuyor.", 403);
            }

            return await next();
        }

        internal static RequestMenuPermissions DeclaredPermissions(object request)
        {
            if (request is IRequiresMenuPermissions multiple && multiple.RequiredMenuPermissions.Count > 0)
                return new([new(multiple.RequiredMenuPermissions, multiple.PermissionMatch)]);
            if (request is IRequiresMenuPermission single && !string.IsNullOrWhiteSpace(single.RequiredMenuKod))
                return new([new([new(single.RequiredMenuKod,
                    request.GetType().Name.EndsWith("Query", StringComparison.Ordinal) ? YetkiTipi.R : YetkiTipi.W)])]);
            return RequestMenuPermissions.Denied;
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
