using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>
    /// Aplica migrations somente quando <c>Database:MigrateOnStartup</c> for true (padrao: false).
    /// Destinado ao ambiente local do Docker Compose; nao habilitar em producao.
    /// </summary>
    public static async Task ApplyMigrationsIfConfiguredAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
