using Microsoft.Data.SqlClient;
using Infrastructure.Authentication;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    private const string ConnectionString = "Server=localhost,1433;Database=FinanceDb;User Id=sa;Password=unused;TrustServerCertificate=True";

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void DbContextIsConfiguredForSqlServerWithTheConfiguredConnectionString()
    {
        using var provider = BuildProvider(new() { ["ConnectionStrings:DefaultConnection"] = ConnectionString });
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        dbContext.Database.ProviderName.ShouldBe("Microsoft.EntityFrameworkCore.SqlServer");
        var actualConnectionString = dbContext.Database.GetConnectionString();

        actualConnectionString.ShouldNotBeNull();

        var actual = new SqlConnectionStringBuilder(
            actualConnectionString);

        var expected = new SqlConnectionStringBuilder(
            ConnectionString);

        actual.DataSource.ShouldBe(expected.DataSource);
        actual.InitialCatalog.ShouldBe(expected.InitialCatalog);
        actual.UserID.ShouldBe(expected.UserID);
        actual.Password.ShouldBe(expected.Password);
        actual.TrustServerCertificate.ShouldBe(
            expected.TrustServerCertificate);
    }

    [Fact]
    public void MissingConnectionStringFailsFastWithAClearMessage()
    {
        using var provider = BuildProvider([]);
        using var scope = provider.CreateScope();

        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            _ = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        });

        exception.Message.ShouldContain("DefaultConnection");
    }

    [Fact]
    public void TimeProviderIsTheSystemClock()
    {
        using var provider = BuildProvider([]);

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void JwtOptionsAreBoundFromConfiguration()
    {
        using var provider = BuildProvider(new()
        {
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:Secret"] = new string('s', 32),
            ["Jwt:AccessTokenMinutes"] = "10",
        });

        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        options.Issuer.ShouldBe("issuer");
        options.Audience.ShouldBe("audience");
        options.AccessTokenMinutes.ShouldBe(10);
        options.RefreshTokenDays.ShouldBe(7);
    }

    [Fact]
    public void JwtOptionsWithAShortSecretAreRejected()
    {
        using var provider = BuildProvider(new()
        {
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:Secret"] = "too-short",
        });
        var options = provider.GetRequiredService<IOptions<JwtOptions>>();

        Should.Throw<OptionsValidationException>(() =>
        {
            _ = options.Value;
        });
    }
}
