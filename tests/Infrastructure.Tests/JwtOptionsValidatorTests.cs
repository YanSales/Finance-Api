using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Infrastructure.Tests;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator validator = new();

    private static JwtOptions CreateOptions(string secret) => new()
    {
        Issuer = "issuer",
        Audience = "audience",
        Secret = secret,
    };

    [Fact]
    public void ValidateAcceptsAnOrdinarySecret()
    {
        validator.Validate(Options.DefaultName, CreateOptions(new string('s', 48))).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS")]
    [InlineData("change_me_to_a_long_random_secret_at_least_32_characters")]
    public void ValidateRejectsTheExamplePlaceholderSecret(string secret)
    {
        var result = validator.Validate(Options.DefaultName, CreateOptions(secret));

        result.Failed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "audience")]
    [InlineData("issuer", " ")]
    public void ValidateRejectsAMissingIssuerOrAudience(string issuer, string audience)
    {
        var options = CreateOptions(new string('s', 48));
        options.Issuer = issuer;
        options.Audience = audience;

        validator.Validate(Options.DefaultName, options).Failed.ShouldBeTrue();
    }
}
