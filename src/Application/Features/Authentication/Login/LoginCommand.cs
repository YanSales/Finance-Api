using Contracts.Authentication;
using MediatR;

namespace Application.Features.Authentication.Login;

/// <summary>
/// Autentica um usuario. O resultado e <see langword="null"/> para qualquer falha de credenciais
/// (usuario inexistente, senha incorreta ou usuario inativo), sem distinguir o motivo.
/// </summary>
/// <param name="Email">E-mail informado.</param>
/// <param name="Password">Senha em texto puro.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse?>;
