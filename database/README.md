# database

O **schema é definido pelas migrations do EF Core** (`src/Infrastructure/Persistence/Migrations`).
Esta pasta não duplica o schema; ela abriga apenas SQL que o EF não modela.

| Pasta         | Uso                                                                                          |
|---------------|----------------------------------------------------------------------------------------------|
| `procedures/` | Stored Procedures versionadas (`sp_*.sql`). Aplicadas via `migrationBuilder.Sql(...)` em migrations. |
| `scripts/`    | Scripts auxiliares manuais (ex.: diagnóstico). Nunca substituem migrations.                  |

## Comandos (a partir da raiz do repositório)

```bash
dotnet tool restore

# criar migration
dotnet ef migrations add <Nome> -p src/Infrastructure -s src/Api -o Persistence/Migrations

# aplicar no banco configurado em ConnectionStrings__DefaultConnection
dotnet ef database update -p src/Infrastructure -s src/Api

# verificar se o modelo diverge das migrations
dotnet ef migrations has-pending-model-changes -p src/Infrastructure -s src/Api
```

Com Docker Compose, as migrations são aplicadas na inicialização da API
(`Database__MigrateOnStartup=true`, somente nesse ambiente local).
