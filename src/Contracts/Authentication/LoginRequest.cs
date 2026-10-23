namespace Contracts.Authentication;

/// <summary>Credenciais para autenticacao.</summary>
/// <param name="Email">E-mail cadastrado.</param>
/// <param name="Password">Senha em texto puro.</param>
public sealed record LoginRequest(string Email, string Password);
