using Domain.Entities;
using Domain.Enums;
using Infrastructure.Authentication;
using Infrastructure.Tests.Support;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Infrastructure.Tests;

public sealed class JwtAccessTokenServiceTests
{
    private const string PasswordHash = "stored-password-hash-must-never-be-in-the-token";

    // 64 caracteres: suficiente tambem para HS512 nos testes de rejeicao de algoritmo.
    private static readonly string Secret = new('k', 64);

    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private static JwtOptions CreateOptions(string? secret = null) => new()
    {
        Issuer = "finance-api-tests",
        Audience = "finance-api-tests-clients",
        Secret = secret ?? Secret,
        AccessTokenMinutes = 15,
    };

    private static JwtAccessTokenService CreateService(JwtOptions options, DateTimeOffset issuedAt) =>
        new(Options.Create(options), new FixedTimeProvider(issuedAt));

    private static Task<TokenValidationResult> ValidateAsync(string token, JwtOptions options) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, JwtTokenValidation.CreateParameters(options));

    private static User CreateUser(UserRole role = UserRole.User)
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);
        user.ChangeRole(role, Now);

        return user;
    }

    [Fact]
    public async Task IssueProducesATokenThatPassesTheSameValidationUsedByTheApi()
    {
        var options = CreateOptions();
        var user = CreateUser(UserRole.Admin);

        var issued = CreateService(options, DateTimeOffset.UtcNow).Issue(user);
        var result = await ValidateAsync(issued.Value, options);

        result.IsValid.ShouldBeTrue();
        result.Claims["sub"].ShouldBe(user.Id.ToString());
        result.Claims["email"].ShouldBe("ana@example.com");
        result.Claims["role"].ShouldBe("Admin");
        result.Claims.ContainsKey("jti").ShouldBeTrue();
    }

    [Fact]
    public void IssueSetsTheExpirationFromTheConfiguredLifetime()
    {
        var issued = CreateService(CreateOptions(), Now).Issue(CreateUser());

        issued.ExpiresAt.ShouldBe(Now.AddMinutes(15));
    }

    [Fact]
    public async Task IssueNeverPutsThePasswordHashInTheToken()
    {
        var options = CreateOptions();

        var issued = CreateService(options, DateTimeOffset.UtcNow).Issue(CreateUser());
        var result = await ValidateAsync(issued.Value, options);

        issued.Value.ShouldNotContain(PasswordHash);
        result.Claims.Values.Select(value => value.ToString()).ShouldNotContain(PasswordHash);
    }

    [Fact]
    public async Task ValidationRejectsAnExpiredToken()
    {
        var options = CreateOptions();

        var issued = CreateService(options, DateTimeOffset.UtcNow.AddHours(-2)).Issue(CreateUser());

        (await ValidateAsync(issued.Value, options)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidationRejectsATokenSignedWithAnotherSecret()
    {
        var token = CreateService(CreateOptions(new string('x', 64)), DateTimeOffset.UtcNow).Issue(CreateUser());

        (await ValidateAsync(token.Value, CreateOptions())).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidationRejectsAnotherIssuer()
    {
        var other = CreateOptions();
        other.Issuer = "someone-else";
        var token = CreateService(other, DateTimeOffset.UtcNow).Issue(CreateUser());

        (await ValidateAsync(token.Value, CreateOptions())).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidationRejectsAnotherAudience()
    {
        var other = CreateOptions();
        other.Audience = "another-audience";
        var token = CreateService(other, DateTimeOffset.UtcNow).Issue(CreateUser());

        (await ValidateAsync(token.Value, CreateOptions())).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidationRejectsATokenSignedWithADifferentAlgorithmEvenWithTheRightKey()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(JwtTokenValidation.CreateSigningKey(options), SecurityAlgorithms.HmacSha512),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        (await ValidateAsync(token, options)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidationRejectsAnUnsignedToken()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            NotBefore = now,
            Expires = now.AddMinutes(5),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        (await ValidateAsync(token, options)).IsValid.ShouldBeFalse();
    }
}
