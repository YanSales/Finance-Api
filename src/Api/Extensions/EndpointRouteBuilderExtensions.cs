using Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Api.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// /health: todos os checks. /health/live: apenas processo vivo (nenhum check, nao depende do banco).
    /// /health/ready: checks com a tag "ready" (SQL Server). Unhealthy responde 503.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static _ => false,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(HealthCheckTags.Ready),
        });

        return endpoints;
    }
}
