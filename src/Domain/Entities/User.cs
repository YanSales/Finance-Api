using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Usuario da plataforma (identidade usada na autenticacao). Aggregate root.
/// O e-mail e sempre armazenado normalizado e a senha somente como hash (nunca em texto puro).
/// </summary>
public sealed class User
{
    /// <summary>Tamanho maximo de um e-mail (RFC 5321).</summary>
    public const int EmailMaxLength = 254;

    // Construtor sem parametros reservado ao EF Core (materializacao).
    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>E-mail normalizado (ver <see cref="NormalizeEmail"/>). Unico na plataforma.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Hash da senha produzido por um algoritmo de hash de senhas com salt. Nunca expor.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    /// <summary>Usuarios inativos nao podem autenticar.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Forma canonica do e-mail usada para identificacao e unicidade: sem espacos nas pontas e em minusculas
    /// (cultura invariante).
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>Cria um usuario ativo com o papel <see cref="UserRole.User"/>.</summary>
    /// <exception cref="DomainException">E-mail ou hash de senha invalidos.</exception>
    public static User Create(string email, string passwordHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        var normalizedEmail = NormalizeEmail(email);

        if (!IsValidEmail(normalizedEmail))
        {
            throw new DomainException("The email address is invalid.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("The password hash is required.");
        }

        return new User
        {
            Id = Guid.CreateVersion7(now),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>Altera o papel. Nao ha endpoint publico para isso: uso administrativo/seed.</summary>
    /// <exception cref="DomainException">Papel inexistente.</exception>
    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException("The user role is invalid.");
        }

        if (Role == role)
        {
            return;
        }

        Role = role;
        UpdatedAt = now;
    }

    // Validacao estrutural minima (um unico '@' com texto dos dois lados, sem espacos). A verificacao
    // de entrega real exigiria confirmacao por e-mail, fora do escopo desta fase.
    private static bool IsValidEmail(string normalizedEmail)
    {
        if (normalizedEmail.Length is 0 or > EmailMaxLength)
        {
            return false;
        }

        if (normalizedEmail.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var separatorIndex = normalizedEmail.IndexOf('@', StringComparison.Ordinal);

        return separatorIndex > 0
            && separatorIndex == normalizedEmail.LastIndexOf('@')
            && separatorIndex < normalizedEmail.Length - 1;
    }
}
