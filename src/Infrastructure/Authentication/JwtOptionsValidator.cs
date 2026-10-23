using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

/// <summary>
/// Validacoes de <see cref="JwtOptions"/> alem das DataAnnotations. Rejeita o valor de exemplo do
/// <c>.env.example</c>: um segredo publico equivale a nenhum segredo, entao a API nao deve iniciar com ele.
/// </summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const string PlaceholderPrefix = "CHANGE_ME";

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Secret.StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                "Jwt:Secret still has the example placeholder value. Configure a unique random secret.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Jwt:Issuer and Jwt:Audience are required.");
        }

        return ValidateOptionsResult.Success;
    }
}
