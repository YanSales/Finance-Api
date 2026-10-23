using System.Security.Claims;
using Application.Abstractions.Authentication;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

/// <summary>
/// Emite JWTs assinados com HMAC-SHA256. Claims minimas: <c>sub</c> (id do usuario), <c>email</c>,
/// <c>role</c> e <c>jti</c>. Nunca inclui senha, hash ou outros segredos.
/// </summary>
internal sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtOptions options;
    private readonly TimeProvider timeProvider;
    private readonly SigningCredentials signingCredentials;
    private readonly JsonWebTokenHandler tokenHandler = new();

    public JwtAccessTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.options = options.Value;
        this.timeProvider = timeProvider;
        signingCredentials = new SigningCredentials(
            JwtTokenValidation.CreateSigningKey(this.options),
            JwtTokenValidation.SigningAlgorithm);
    }

    public IssuedAccessToken Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtTokenValidation.RoleClaimType, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = issuedAt.UtcDateTime,
            IssuedAt = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingCredentials,
        };

        return new IssuedAccessToken(tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
