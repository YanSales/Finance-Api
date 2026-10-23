using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

/// <summary>Inicia a API real, apontando para a connection string informada. Segredos sao falsos, de teste.</summary>
internal sealed class FinanceApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>Segredo JWT de teste (nao e um segredo real). Permite forjar tokens validos/invalidos nos testes.</summary>
    public static readonly string JwtSecret = new('t', 64);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Secret"] = JwtSecret,
                ["Database:MigrateOnStartup"] = "false",
            }));

        // Expoe, somente nos testes, um endpoint protegido pela politica AdminOnly (a API ainda nao possui
        // nenhum endpoint administrativo real) para exercitar a diferenca entre 401 e 403.
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(AdminProbeController).Assembly));
    }
}
