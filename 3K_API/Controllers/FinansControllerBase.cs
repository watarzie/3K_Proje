using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace _3K_API.Controllers;

/// <summary>
/// Finans uçlarının ortak tabanı: kimlik doğrulaması ve "api/finans" rotası. Yetki yalnız sekme düzeyindedir ve
/// isteklerde (FinansCommand/FinansQuery.Sekmeler) denetlenir; yanıtlarda alan maskelemesi yoktur.
/// </summary>
[ApiController]
[Authorize]
[Route("api/finans")]
public abstract class FinansControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;
}
