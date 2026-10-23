using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

/// <summary>
/// Ponto unico dos parametros criptograficos do JWT: o servico de emissao e a validacao do JWT Bearer
/// (Api) usam exatamente a mesma chave, algoritmo, emissor e publico.
/// </summary>
public static class JwtTokenValidation
{
    /// <summary>Unico algoritmo de assinatura aceito (HMAC-SHA256).</summary>
    public const string SigningAlgorithm = SecurityAlgorithms.HmacSha256;

    /// <summary>Nome da claim que carrega o papel do usuario.</summary>
    public const string RoleClaimType = "role";

    // Tolerancia pequena para diferenca de relogio entre servidores (o padrao do framework e 5 minutos).
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static SymmetricSecurityKey CreateSigningKey(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
    }

    /// <summary>Valida assinatura, algoritmo, emissor, publico e expiracao. Nada e desabilitado.</summary>
    public static TokenValidationParameters CreateParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(options),
            RequireSignedTokens = true,
            ValidAlgorithms = [SigningAlgorithm],
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
            RoleClaimType = RoleClaimType,
        };
    }
}
