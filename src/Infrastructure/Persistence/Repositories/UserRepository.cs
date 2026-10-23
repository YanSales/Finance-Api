using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(FinanceDbContext dbContext) : IUserRepository
{
    // Numeros de erro do SQL Server: 2601 = indice unico violado; 2627 = constraint UNIQUE/PK violada.
    private const int DuplicateKeyIndexErrorNumber = 2601;
    private const int DuplicateKeyConstraintErrorNumber = 2627;

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            // Cadastro concorrente com o mesmo e-mail: perdeu a corrida para o indice unico.
            throw new ConflictException("The email address is already registered.", exception);
        }
    }

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is SqlException
        {
            Number: DuplicateKeyIndexErrorNumber or DuplicateKeyConstraintErrorNumber,
        };
}
