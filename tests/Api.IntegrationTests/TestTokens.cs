using System.Security.Claims;
using Infrastructure.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.IntegrationTests;

/// <summary>Fabrica JWTs de teste (validos, expirados ou assinados com outra chave) para exercitar a validacao da API.</summary>
internal static class TestTokens
{
    private const string Issuer = "finance-api";
    private const string Audience = "finance-api-clients";

    public static string Create(Guid userId, string secret, TimeSpan notBeforeOffset, TimeSpan expiresOffset)
    {
        var options = new JwtOptions { Issuer = Issuer, Audience = Audience, Secret = secret };
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtTokenValidation.RoleClaimType, "User"),
            ]),
            NotBefore = now.Add(notBeforeOffset),
            Expires = now.Add(expiresOffset),
            SigningCredentials = new SigningCredentials(
                JwtTokenValidation.CreateSigningKey(options),
                JwtTokenValidation.SigningAlgorithm),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static string Valid(Guid userId, string secret) =>
        Create(userId, secret, TimeSpan.FromMinutes(-1), TimeSpan.FromMinutes(10));

    public static string Expired(Guid userId, string secret) =>
        Create(userId, secret, TimeSpan.FromHours(-2), TimeSpan.FromHours(-1));
}
