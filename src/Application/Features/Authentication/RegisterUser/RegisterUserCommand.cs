using Contracts.Authentication;
using MediatR;

namespace Application.Features.Authentication.RegisterUser;

/// <summary>Cadastra um novo usuario com o papel padrao.</summary>
/// <param name="Email">E-mail informado (sera normalizado).</param>
/// <param name="Password">Senha em texto puro. Somente o hash e persistido.</param>
public sealed record RegisterUserCommand(string Email, string Password) : IRequest<RegisterResponse>;
