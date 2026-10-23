using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Contracts.Authentication;
using Contracts.Users;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Api.IntegrationTests;

/// <summary>Fluxo HTTP completo de cadastro, login e autorizacao contra a API real e SQL Server real.</summary>
[Collection(SqlServerTestGroup.Name)]
public sealed class AuthenticationEndpointsTests : IAsyncLifetime, IDisposable
{
    private const string Password = "CorrectHorse-Battery-2026";

    private static readonly Uri RegisterUri = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri LoginUri = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri MeUri = new("/api/v1/users/me", UriKind.Relative);
    private static readonly Uri AdminProbeUri = new("/api/v1/test/admin-probe", UriKind.Relative);

    private readonly string connectionString;
    private readonly FinanceApiFactory factory;
    private readonly HttpClient client;

    public AuthenticationEndpointsTests(SqlServerFixture sqlServer)
    {
        ArgumentNullException.ThrowIfNull(sqlServer);

        // Um banco exclusivo por teste. O host da API nao toca o banco ate a primeira requisicao.
        connectionString = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = $"FinanceTests_{Guid.NewGuid():N}",
        }.ConnectionString;
        factory = new FinanceApiFactory(connectionString);
        client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    // O xUnit chama DisposeAsync (IAsyncLifetime) e depois Dispose (IDisposable). A API e encerrada primeiro,
    // para soltar suas conexoes, e so entao o banco exclusivo do teste e removido.
    public async Task DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    // A classe possui campos IDisposable (factory e client): CA1001 exige que ela propria seja IDisposable.
    // Aqui e apenas a rede de seguranca do caminho sincrono; ambos os Dispose sao idempotentes e ja foram
    // executados em DisposeAsync no fluxo normal.
    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private FinanceDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlServer(connectionString).Options);

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private async Task<RegisterResponse> RegisterAsync(string email)
    {
        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest(email, Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<RegisterResponse>()).ShouldNotBeNull();
    }

    private async Task<string> LoginAsync(string email)
    {
        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, Password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<LoginResponse>()).ShouldNotBeNull().AccessToken;
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task RegisterReturnsCreatedWithoutExposingThePasswordOrItsHash()
    {
        var email = NewEmail();

        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest(email.ToUpperInvariant(), Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotContain(Password);
        json.ShouldNotContain("hash", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);

        using var body = JsonDocument.Parse(json);
        body.RootElement.GetProperty("email").GetString().ShouldBe(email);
        body.RootElement.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RegisterStoresOnlyAHashOfThePassword()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        await using var dbContext = CreateDbContext();
        var stored = await dbContext.Users.SingleAsync(user => user.Email == email);

        stored.PasswordHash.ShouldNotBe(Password);
        stored.PasswordHash.ShouldNotContain(Password);
        stored.Role.ShouldBe(UserRole.User);
        stored.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterRejectsADuplicateEmailIgnoringCase()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest(email.ToUpperInvariant(), Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("not-an-email", "CorrectHorse-Battery-2026")]
    [InlineData("valid@example.com", "short")]
    public async Task RegisterRejectsInvalidPayloadsWithAProblemDetailsBody(string email, string password)
    {
        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest(email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task LoginReturnsABearerTokenForValidCredentials()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, Password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>()).ShouldNotBeNull();
        login.AccessToken.ShouldNotBeNullOrWhiteSpace();
        login.TokenType.ShouldBe("Bearer");
        login.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        login.ExpiresInSeconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LoginRejectsAWrongPasswordAndAnUnknownEmailWithIdenticalResponses()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var wrongPassword = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, "wrong-password-123"));
        using var unknownEmail = await client.PostAsJsonAsync(LoginUri, new LoginRequest(NewEmail(), Password));

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var wrongPasswordBody = JsonDocument.Parse(await wrongPassword.Content.ReadAsStringAsync());
        using var unknownEmailBody = JsonDocument.Parse(await unknownEmail.Content.ReadAsStringAsync());
        wrongPasswordBody.RootElement.GetProperty("title").GetString()
            .ShouldBe(unknownEmailBody.RootElement.GetProperty("title").GetString());
        wrongPasswordBody.RootElement.GetProperty("detail").GetString()
            .ShouldBe(unknownEmailBody.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task LoginRejectsAnInactiveUser()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        await using (var dbContext = CreateDbContext())
        {
            var user = await dbContext.Users.SingleAsync(candidate => candidate.Email == email);
            user.Deactivate(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeRequiresAuthentication()
    {
        using var response = await GetAsync(MeUri, bearerToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeRejectsAMalformedToken()
    {
        using var response = await GetAsync(MeUri, "this-is-not-a-jwt");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeRejectsAnExpiredToken()
    {
        var registered = await RegisterAsync(NewEmail());

        using var response = await GetAsync(MeUri, TestTokens.Expired(registered.Id, FinanceApiFactory.JwtSecret));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeRejectsATokenSignedWithAnotherKey()
    {
        var registered = await RegisterAsync(NewEmail());

        using var response = await GetAsync(MeUri, TestTokens.Valid(registered.Id, new string('x', 64)));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeRejectsAValidlySignedTokenForAUserThatDoesNotExist()
    {
        // Token valido (assinatura correta) para um usuario que nao existe: autenticado, mas sem conta -> 401.
        using var response = await GetAsync(MeUri, TestTokens.Valid(Guid.NewGuid(), FinanceApiFactory.JwtSecret));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeReturnsTheAuthenticatedUserWithoutSensitiveData()
    {
        var email = NewEmail();
        var registered = await RegisterAsync(email);
        var token = await LoginAsync(email);

        using var response = await GetAsync(MeUri, token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotContain("hash", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);

        var me = JsonSerializer.Deserialize<CurrentUserResponse>(json, JsonSerializerOptions.Web).ShouldNotBeNull();
        me.Id.ShouldBe(registered.Id);
        me.Email.ShouldBe(email);
        me.Role.ShouldBe("User");
    }

    [Fact]
    public async Task AdminEndpointWithoutATokenIsUnauthorized()
    {
        using var response = await GetAsync(AdminProbeUri, bearerToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpointIsForbiddenForAnAuthenticatedUserWithoutTheAdminRole()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var token = await LoginAsync(email);

        using var response = await GetAsync(AdminProbeUri, token);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpointIsAllowedForAnAdmin()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        await using (var dbContext = CreateDbContext())
        {
            var user = await dbContext.Users.SingleAsync(candidate => candidate.Email == email);
            user.ChangeRole(UserRole.Admin, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var token = await LoginAsync(email);

        using var response = await GetAsync(AdminProbeUri, token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ControllersWithoutAuthorizationAttributesAreProtectedByDefault()
    {
        var unannotatedUri = new Uri("/api/v1/test/unannotated-probe", UriKind.Relative);
        var email = NewEmail();
        await RegisterAsync(email);
        var token = await LoginAsync(email);

        using var anonymous = await GetAsync(unannotatedUri, bearerToken: null);
        using var authenticated = await GetAsync(unannotatedUri, token);

        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        authenticated.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnknownRoutesReturnNotFoundForAnonymousAndAuthenticatedCallers()
    {
        var unknownUri = new Uri("/does-not-exist", UriKind.Relative);
        var email = NewEmail();
        await RegisterAsync(email);
        var token = await LoginAsync(email);

        using var anonymous = await GetAsync(unknownUri, bearerToken: null);
        using var authenticated = await GetAsync(unknownUri, token);

        anonymous.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        authenticated.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublicEndpointsRemainReachableWithoutAToken()
    {
        using var live = await GetAsync(new Uri("/health/live", UriKind.Relative), bearerToken: null);
        using var openApi = await GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), bearerToken: null);

        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        openApi.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
