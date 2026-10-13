using Infrastructure.Authentication;
using Infrastructure.Health;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionName = "DefaultConnection";

    /// <summary>
    /// Registra os servicos da camada Infrastructure. A configuracao e lida de forma tardia
    /// (no momento da resolucao), o que permite sobrescritas de ambiente e de testes.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<FinanceDbContext>(options =>
            options.UseSqlServer(GetConnectionString(configuration)));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: null,
                tags: [HealthCheckTags.Ready],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(DefaultConnectionName)
        ?? throw new InvalidOperationException(
            $"Connection string '{DefaultConnectionName}' is not configured. " +
            "Set 'ConnectionStrings__DefaultConnection' (environment variable or user-secrets).");
}
