using Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

/// <summary>
/// Hash de senhas com o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core Identity (formato V3:
/// PBKDF2 com HMAC-SHA512, salt aleatorio de 128 bits e subchave de 256 bits). Nenhum algoritmo proprio.
/// </summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    // OWASP Password Storage Cheat Sheet: PBKDF2-HMAC-SHA512 com 210.000 iteracoes.
    internal const int IterationCount = 210_000;

    // O hasher do Identity recebe o usuario por compatibilidade de API, mas nao o utiliza.
    private static readonly object NoUser = new();

    private readonly PasswordHasher<object> hasher = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = IterationCount,
    }));

    // Hash de uma senha descartavel, usado para equalizar o tempo quando o usuario nao existe.
    private readonly string dummyHash;

    public IdentityPasswordHasher()
    {
        dummyHash = hasher.HashPassword(NoUser, Guid.NewGuid().ToString());
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return hasher.HashPassword(NoUser, password);
    }

    public bool Verify(string? passwordHash, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var result = hasher.VerifyHashedPassword(NoUser, passwordHash ?? dummyHash, password);

        // SuccessRehashNeeded (parametros antigos) tambem autentica; o rehash sera tratado quando os
        // parametros de custo mudarem.
        return passwordHash is not null
            && result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
