using Application.Features.Authentication.Login;
using Application.Features.Authentication.RegisterUser;
using Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Cadastro e autenticacao de usuarios.</summary>
/// <param name="mediator">Mediador dos casos de uso.</param>
[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    /// <summary>Cadastra um novo usuario.</summary>
    /// <param name="request">E-mail e senha (12 a 128 caracteres).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <response code="201">Usuario criado. A resposta nunca contem senha nem hash.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="409">E-mail ja cadastrado.</response>
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await mediator.Send(new RegisterUserCommand(request.Email, request.Password), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Autentica o usuario e emite um access token (JWT).</summary>
    /// <param name="request">Credenciais.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <response code="200">Autenticado. Envie o token em <c>Authorization: Bearer</c>.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="401">Credenciais invalidas (resposta identica para usuario inexistente, senha incorreta ou conta inativa).</response>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return response is null
            ? Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect.")
            : Ok(response);
    }
}
