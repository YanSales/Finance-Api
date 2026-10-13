using System.Net;
using System.Text.Json;
using Shouldly;

namespace Api.IntegrationTests;

public sealed class ApiFoundationTests
{
    private const string UnusedConnectionString =
        "Server=127.0.0.1,1;Database=FinanceDb;User Id=sa;Password=unused;Connect Timeout=1;TrustServerCertificate=True";

    [Fact]
    public async Task UnknownRouteReturnsProblemDetails()
    {
        await using var factory = new FinanceApiFactory(UnusedConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/does-not-exist", UriKind.Relative));
        request.Headers.Add("Accept", "application/json");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        body.RootElement.TryGetProperty("traceId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task OpenApiDocumentIsServedInDevelopment()
    {
        await using var factory = new FinanceApiFactory(UnusedConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
