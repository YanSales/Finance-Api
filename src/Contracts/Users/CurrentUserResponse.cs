namespace Contracts.Users;

/// <summary>Dados do usuario autenticado. Nunca contem senha nem hash de senha.</summary>
/// <param name="Id">Identificador estavel do usuario.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="Role">Papel do usuario (<c>User</c> ou <c>Admin</c>).</param>
/// <param name="CreatedAt">Instante de criacao da conta (UTC).</param>
public sealed record CurrentUserResponse(Guid Id, string Email, string Role, DateTimeOffset CreatedAt);
