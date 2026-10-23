using Api.Extensions;
using Application.Features.Users.GetCurrentUser;
using Contracts.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Dados do usuario autenticado.</summary>
/// <param name="mediator">Mediador dos casos de uso.</param>
[ApiController]
[Authorize]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    /// <summary>Retorna o usuario autenticado (sem dados sensiveis).</summary>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <response code="200">Usuario autenticado.</response>
    /// <response code="401">Token ausente, invalido ou expirado, ou conta inexistente/inativa.</response>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await mediator.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);

        return response is null ? Unauthorized() : Ok(response);
    }
}
