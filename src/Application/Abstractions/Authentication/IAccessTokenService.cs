using Domain.Entities;

namespace Application.Abstractions.Authentication;

/// <summary>Emissao de access tokens. A validacao dos tokens e feita na borda HTTP (JWT Bearer).</summary>
public interface IAccessTokenService
{
    /// <summary>Emite um access token de curta duracao para o usuario.</summary>
    IssuedAccessToken Issue(User user);
}

/// <summary>Token emitido e seu instante de expiracao (UTC).</summary>
/// <param name="Value">Valor do token.</param>
/// <param name="ExpiresAt">Instante de expiracao.</param>
public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);
