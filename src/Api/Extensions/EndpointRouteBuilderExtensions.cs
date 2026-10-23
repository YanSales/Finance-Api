using Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Api.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// /health: todos os checks. /health/live: apenas processo vivo (nenhum check, nao depende do banco).
    /// /health/ready: checks com a tag "ready" (SQL Server). Unhealthy responde 503.
    /// Todos sao publicos (AllowAnonymous): orquestradores nao possuem token, e a marcacao permanece valida
    /// caso uma convencao de autorizacao global seja aplicada no futuro.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health").AllowAnonymous();

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static _ => false,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(HealthCheckTags.Ready),
        }).AllowAnonymous();

        return endpoints;
    }
}
