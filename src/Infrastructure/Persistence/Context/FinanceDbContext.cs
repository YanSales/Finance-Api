using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Context;

/// <summary>
/// Contexto EF Core da Finance API. As configuracoes de entidades ficam em classes
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> descobertas automaticamente neste assembly.
/// </summary>
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Valores monetarios: decimal(19,4) por convencao (ADR 0002).
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
    }
}
