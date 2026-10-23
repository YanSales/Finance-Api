using Api.Authorization;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Api.Extensions;

internal static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// JWT Bearer + autorizacao. Os parametros de validacao vem de <see cref="JwtOptions"/> (lidos de forma tardia,
    /// ja validados na inicializacao) e sao os mesmos usados na emissao do token. A exigencia de autenticacao por
    /// padrao e aplicada aos controllers em <c>Program.cs</c> (<c>MapControllers().RequireAuthorization()</c>), e nao
    /// por FallbackPolicy, que tambem atingiria rotas inexistentes (401 no lugar de 404).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.TokenValidationParameters = JwtTokenValidation.CreateParameters(jwt.Value);

                // Mantem os nomes originais das claims ("sub", "role") em vez do mapeamento legado para ClaimTypes.*.
                bearer.MapInboundClaims = false;
                bearer.SaveToken = false;
            });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Domain.Enums.UserRole.Admin.ToString()));

        return services;
    }
}
