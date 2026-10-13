using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.IntegrationTests;

/// <summary>Inicia a API real, apontando para a connection string informada. Segredos sao falsos, de teste.</summary>
internal sealed class FinanceApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Secret"] = new string('t', 64),
                ["Database:MigrateOnStartup"] = "false",
            }));
    }
}
