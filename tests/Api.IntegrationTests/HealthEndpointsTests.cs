using System.Net;
using Shouldly;

namespace Api.IntegrationTests;

public sealed class HealthEndpointsTests
{
    private const string UnreachableConnectionString =
        "Server=127.0.0.1,1;Database=FinanceDb;User Id=sa;Password=unused;Connect Timeout=1;TrustServerCertificate=True";

    [Fact]
    public async Task LiveIsOkEvenWhenTheDatabaseIsDown()
    {
        await using var factory = new FinanceApiFactory(UnreachableConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyIsServiceUnavailableWhenTheDatabaseIsDown()
    {
        await using var factory = new FinanceApiFactory(UnreachableConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthIsServiceUnavailableWhenTheDatabaseIsDown()
    {
        await using var factory = new FinanceApiFactory(UnreachableConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}

[Collection(SqlServerCollection.Name)]
public sealed class HealthEndpointsWithDatabaseTests(SqlServerFixture sqlServer)
{
    [Fact]
    public async Task ReadyIsOkWhenTheDatabaseIsReachable()
    {
        await using var factory = new FinanceApiFactory(sqlServer.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
