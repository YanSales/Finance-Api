using Domain.Entities;

namespace Application.Abstractions.Persistence;

/// <summary>Persistencia do aggregate <see cref="User"/>.</summary>
public interface IUserRepository
{
    /// <summary>Busca por e-mail ja normalizado (<see cref="User.NormalizeEmail"/>). Somente leitura.</summary>
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Busca por identificador. Somente leitura.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o novo usuario.
    /// </summary>
    /// <exception cref="Application.Common.Exceptions.ConflictException">
    /// O e-mail ja esta cadastrado (inclusive quando detectado pela restricao de unicidade do banco em cadastros concorrentes).
    /// </exception>
    Task AddAsync(User user, CancellationToken cancellationToken);
}
