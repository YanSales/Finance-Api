using System.Diagnostics;
using Api.Middleware;

namespace Api.Extensions;

internal static class ApiServiceCollectionExtensions
{
    /// <summary>Problem Details, tratamento global de excecoes e OpenAPI.</summary>
    public static IServiceCollection AddApiFoundation(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();

        return services;
    }
}
