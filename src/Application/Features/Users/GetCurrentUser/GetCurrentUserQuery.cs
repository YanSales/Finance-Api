using Contracts.Users;
using MediatR;

namespace Application.Features.Users.GetCurrentUser;

/// <summary>
/// Consulta os dados do usuario autenticado. O resultado e <see langword="null"/> quando a conta nao existe
/// mais ou esta inativa.
/// </summary>
/// <param name="UserId">Identificador extraido das claims do token (nunca de entrada do cliente).</param>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse?>;
