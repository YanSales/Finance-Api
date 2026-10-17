using Testcontainers.MsSql;

namespace Infrastructure.Tests;

/// <summary>SQL Server real em container (requer Docker). Compartilhado pela collection "SqlServer".</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container =
        new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

    public string ConnectionString => container.GetConnectionString();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

[CollectionDefinition(SqlServerTestGroup.Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
