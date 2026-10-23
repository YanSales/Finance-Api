using System.Globalization;
using Infrastructure.Health;
using Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace Infrastructure.Tests;

[Collection(SqlServerTestGroup.Name)]
public sealed class DatabaseTests(SqlServerFixture sqlServer)
{
    private const string UnreachableConnectionString =
        "Server=127.0.0.1,1;Database=FinanceDb;User Id=sa;Password=unused;Connect Timeout=1;TrustServerCertificate=True";

    private static FinanceDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlServer(connectionString).Options);

    [Fact]
    public async Task MigrationsApplyToARealDatabase()
    {
        var connectionString = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = $"FinanceTests_{Guid.NewGuid():N}",
        }.ConnectionString;
        await using var dbContext = CreateContext(connectionString);

        await dbContext.Database.MigrateAsync();

        var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
        applied.ShouldContain(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
        applied.ShouldContain(migration => migration.EndsWith("_AddUsers", StringComparison.Ordinal));
        (await dbContext.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();

        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task UsersTableEnforcesUniqueEmailsInTheDatabase()
    {
        var connectionString = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = $"FinanceTests_{Guid.NewGuid():N}",
        }.ConnectionString;
        await using var dbContext = CreateContext(connectionString);
        await dbContext.Database.MigrateAsync();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'UX_Users_Email' AND is_unique = 1 AND object_id = OBJECT_ID('dbo.Users')",
            connection);

        Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture).ShouldBe(1);

        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task DatabaseHealthCheckIsHealthyWhenSqlServerIsReachable()
    {
        await using var dbContext = CreateContext(sqlServer.ConnectionString);

        var result = await new DatabaseHealthCheck(dbContext).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task DatabaseHealthCheckIsUnhealthyWhenSqlServerIsUnreachable()
    {
        await using var dbContext = CreateContext(UnreachableConnectionString);

        var result = await new DatabaseHealthCheck(dbContext).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("The database is not reachable.");
    }
}
