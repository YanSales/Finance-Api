using Application.Abstractions.Authentication;
using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Contracts.Authentication;
using Domain.Entities;
using MediatR;

namespace Application.Features.Authentication.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IRequestHandler<RegisterUserCommand, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = User.NormalizeEmail(request.Email);

        // Verificacao previa apenas para falhar rapido e evitar hashing desnecessario. A garantia real de
        // unicidade e o indice unico do banco: o repositorio converte a violacao em ConflictException.
        if (await users.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("The email address is already registered.");
        }

        var user = User.Create(email, passwordHasher.Hash(request.Password), timeProvider.GetUtcNow());

        await users.AddAsync(user, cancellationToken);

        return new RegisterResponse(user.Id, user.Email, user.CreatedAt);
    }
}
