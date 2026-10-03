using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// Merkezi yetki adımı. Gereksinimler sunucu kodunda tanımlıdır (RequestMenuPermissionResolver
    /// veya isteğin açık bildirimi) ve bütün modüllerde aynı değerlendiriciyle kontrol edilir.
    /// X-Menu-Kod başlığı, route veya gövde değeri yetki kaynağı değildir.
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

            if (_currentUserService.UserId is not int userId)
                return CreateFailureResult("Kullanıcı bilgisi alınamadı.", 401);

            if (MenuAuthorizationExceptions.IsOwnAuthenticatedData(request))
                return await next();

            // Bu bağlam yalnız onay handler'ında kayıt/karar/meta veri kontrollerinden
            // sonra açılır. Onay yetkisi, talep edenin ekran yazma izniyle aynı değildir.
            if (_approvalExecutionContext?.IsExecutingApprovedCommand == true &&
                request is IApprovalOperation && RequestMenuPermissionResolver.HasServerDefinition(request))
                return await next();

            var policy = _permissionResolver != null
                ? await _permissionResolver.ResolveAsync(request, cancellationToken)
                : DeclaredPermissions(request);

            if (policy.Groups.Count == 0)
            {
                // Tanımlı işlemde boş politika, kayıt bulunamadığı veya başka bir projeye ait olduğu anlamına gelir.
                return CreateFailureResult(RequestMenuPermissionResolver.HasServerDefinition(request)
                    ? "Kayıt bulunamadı veya bu işlem için yetkiniz bulunmuyor."
                    : "Bu işlem için sunucu yetki tanımı bulunamadı.", 403);
            }

            foreach (var group in policy.Groups)
            {
                if (group.Requirements.Count == 0 || !Enum.IsDefined(group.Match) ||
                    group.Requirements.Any(x => string.IsNullOrWhiteSpace(x.MenuKod) ||
                        x.YetkiTipi is not (YetkiTipi.R or YetkiTipi.W)))
                    return CreateFailureResult("Geçersiz sunucu yetki tanımı.", 403);

                var anyGranted = false;
                foreach (var requirement in group.Requirements.Distinct())
                {
                    var granted = await _rolService.HasUserPermissionAsync(userId,
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
                return new([new([new(single.RequiredMenuKod, single.RequiredYetkiTipi)])]);
            return RequestMenuPermissions.Denied;
        }

        private static TResponse CreateFailureResult(string message, int code) =>
            PipelineResultFactory.TryCreateFailure<TResponse>(message, code, null, out var failure)
                ? failure
                : throw new UnauthorizedAccessException(message);
    }
}
