using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace _3K_API.Controllers;

/// <summary>
/// Finans uçlarının ortak tabanı: kimlik doğrulaması, "api/finans" rotası ve bütün yanıtlara
/// uygulanan alan izni projeksiyonu (FinansAlanErisimFiltresi) burada tanımlanır.
/// </summary>
[ApiController]
[Authorize]
[Route("api/finans")]
[TypeFilter(typeof(FinansAlanErisimFiltresi))]
public abstract class FinansControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;
}
