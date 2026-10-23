namespace Contracts.Authentication;

/// <summary>Token de acesso emitido apos autenticacao bem-sucedida.</summary>
/// <param name="AccessToken">JWT a ser enviado no cabecalho <c>Authorization: Bearer</c>.</param>
/// <param name="TokenType">Esquema de autorizacao. Sempre <c>Bearer</c>.</param>
/// <param name="ExpiresAt">Instante de expiracao do token (UTC).</param>
/// <param name="ExpiresInSeconds">Segundos restantes de validade no momento da emissao.</param>
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
