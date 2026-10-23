namespace Application.Features.Authentication;

/// <summary>
/// Politica de senha. Foco em comprimento (orientacao NIST SP 800-63B), sem regras de composicao.
/// O limite superior evita custo excessivo de hashing com entradas gigantes.
/// </summary>
internal static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;
}
