using Application.Abstractions.Authentication;
using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Domain.Entities;

namespace Application.Tests.Support;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    /// <summary>Simula a perda da corrida para o indice unico do banco no momento de persistir.</summary>
    public bool ThrowConflictOnAdd { get; set; }

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Email == normalizedEmail));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Id == id));

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Any(user => user.Email == normalizedEmail));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        if (ThrowConflictOnAdd)
        {
            throw new ConflictException("The email address is already registered.");
        }

        Users.Add(user);

        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public const string Prefix = "hashed:";

    public int VerifyCalls { get; private set; }

    public string? LastVerifiedHash { get; private set; }

    public string Hash(string password) => Prefix + password;

    public bool Verify(string? passwordHash, string password)
    {
        VerifyCalls++;
        LastVerifiedHash = passwordHash;

        return passwordHash is not null && passwordHash == Prefix + password;
    }
}

internal sealed class FakeAccessTokenService(TimeProvider timeProvider) : IAccessTokenService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public IssuedAccessToken Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new IssuedAccessToken($"token-for-{user.Id}", timeProvider.GetUtcNow().Add(Lifetime));
    }
}
