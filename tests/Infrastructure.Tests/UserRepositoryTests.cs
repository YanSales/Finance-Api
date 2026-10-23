using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Infrastructure.Tests;

/// <summary>Persistencia de usuarios contra SQL Server real (migrations aplicadas em um banco exclusivo por teste).</summary>
[Collection(SqlServerTestGroup.Name)]
public sealed class UserRepositoryTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private string connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        connectionString = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = $"FinanceTests_{Guid.NewGuid():N}",
        }.ConnectionString;

        await using var dbContext = CreateContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = CreateContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    private FinanceDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlServer(connectionString).Options);

    [Fact]
    public async Task AddedUsersCanBeFoundByEmailAndByIdWithAllTheirFields()
    {
        var user = User.Create("Ana@Example.com", "stored-hash", Now);
        user.ChangeRole(UserRole.Admin, Now.AddMinutes(1));

        await using (var writeContext = CreateContext())
        {
            await new UserRepository(writeContext).AddAsync(user, CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var repository = new UserRepository(readContext);

        var byEmail = await repository.FindByEmailAsync("ana@example.com", CancellationToken.None);
        var byId = await repository.FindByIdAsync(user.Id, CancellationToken.None);

        var found = byEmail.ShouldNotBeNull();
        byId.ShouldNotBeNull().Id.ShouldBe(user.Id);
        found.Id.ShouldBe(user.Id);
        found.Email.ShouldBe("ana@example.com");
        found.PasswordHash.ShouldBe("stored-hash");
        found.Role.ShouldBe(UserRole.Admin);
        found.IsActive.ShouldBeTrue();
        found.CreatedAt.ShouldBe(Now);
        found.UpdatedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public async Task LookupsReturnNullWhenTheUserDoesNotExist()
    {
        await using var dbContext = CreateContext();
        var repository = new UserRepository(dbContext);

        (await repository.FindByEmailAsync("nobody@example.com", CancellationToken.None)).ShouldBeNull();
        (await repository.FindByIdAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeNull();
        (await repository.ExistsByEmailAsync("nobody@example.com", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByEmailIsTrueAfterTheUserIsAdded()
    {
        await using var dbContext = CreateContext();
        var repository = new UserRepository(dbContext);

        await repository.AddAsync(User.Create("ana@example.com", "hash", Now), CancellationToken.None);

        (await repository.ExistsByEmailAsync("ana@example.com", CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task AddingAnExistingEmailIsRejectedByTheUniqueIndexAsAConflict()
    {
        await using (var firstContext = CreateContext())
        {
            await new UserRepository(firstContext).AddAsync(User.Create("ana@example.com", "hash", Now), CancellationToken.None);
        }

        await using var secondContext = CreateContext();

        await Should.ThrowAsync<ConflictException>(() =>
            new UserRepository(secondContext).AddAsync(User.Create("ana@example.com", "hash", Now), CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentRegistrationsOfTheSameEmailCreateExactlyOneUser()
    {
        const int attempts = 8;

        var outcomes = await Task.WhenAll(Enumerable.Range(0, attempts).Select(async _ =>
        {
            await using var dbContext = CreateContext();

            try
            {
                await new UserRepository(dbContext).AddAsync(User.Create("race@example.com", "hash", Now), CancellationToken.None);

                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        }));

        outcomes.Count(succeeded => succeeded).ShouldBe(1);

        await using var verificationContext = CreateContext();
        (await verificationContext.Users.CountAsync(user => user.Email == "race@example.com")).ShouldBe(1);
    }
}
