using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Authentication;

/// <summary>
/// Configuracao JWT (secao <c>Jwt</c>). O <see cref="Secret"/> nunca vai em arquivos versionados:
/// use variavel de ambiente (<c>Jwt__Secret</c>) ou user-secrets. Validada na inicializacao.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
