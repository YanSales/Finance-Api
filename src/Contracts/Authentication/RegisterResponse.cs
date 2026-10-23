namespace Contracts.Authentication;

/// <summary>Usuario cadastrado. Nunca contem senha nem hash de senha.</summary>
/// <param name="Id">Identificador estavel do usuario.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="CreatedAt">Instante de criacao (UTC).</param>
public sealed record RegisterResponse(Guid Id, string Email, DateTimeOffset CreatedAt);
