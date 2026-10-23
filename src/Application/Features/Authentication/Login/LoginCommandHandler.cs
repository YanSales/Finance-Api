using Application.Abstractions.Authentication;
using Application.Abstractions.Persistence;
using Contracts.Authentication;
using Domain.Entities;
using MediatR;

namespace Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokens,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, LoginResponse?>
{
    private const string BearerTokenType = "Bearer";

    public async Task<LoginResponse?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        // A senha e sempre verificada (com hash "dummy" quando o usuario nao existe) e o estado de ativacao
        // so e avaliado depois: o resultado e o tempo de resposta nao distinguem os motivos da falha.
        var passwordIsValid = passwordHasher.Verify(user?.PasswordHash, request.Password);

        if (user is null || !passwordIsValid || !user.IsActive)
        {
            return null;
        }

        var token = accessTokens.Issue(user);
        var expiresInSeconds = (int)Math.Max(0, (token.ExpiresAt - timeProvider.GetUtcNow()).TotalSeconds);

        return new LoginResponse(token.Value, BearerTokenType, token.ExpiresAt, expiresInSeconds);
    }
}
