namespace Contracts.Authentication;

/// <summary>Dados para cadastro de um novo usuario.</summary>
/// <param name="Email">E-mail do usuario. Sera normalizado (minusculas) e deve ser unico.</param>
/// <param name="Password">Senha em texto puro (12 a 128 caracteres). Nunca e armazenada nem retornada.</param>
public sealed record RegisterRequest(string Email, string Password);
