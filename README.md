# Finance API

API REST financeira (pessoal e, futuramente, empresarial) construída em .NET com foco em segurança,
consistência financeira, rastreabilidade e evolução incremental. O contexto completo do projeto está em
[`FINANCE_API_CONTEXT_MASTER.md`](FINANCE_API_CONTEXT_MASTER.md).

> **Status: Fase 1 (Foundation).** Não há funcionalidades de negócio ainda: nem autenticação, nem contas,
> nem transações. Existe a base técnica (DI, erros, health checks, OpenAPI, persistência, CI).

## Stack

.NET 10 · ASP.NET Core · EF Core 10 (SQL Server) · Dapper · MediatR 12.5.0 · FluentValidation · Serilog ·
OpenAPI nativo · xUnit · Shouldly · Testcontainers · Docker / Docker Compose · GitHub Actions

## Arquitetura

Clean Architecture + DDD pragmático ([ADR 0001](docs/decisions/0001-architecture.md)).

```text
src/
├── Api/             Composition Root: HTTP, middleware, Problem Details, health, OpenAPI
├── Application/     Casos de uso, validações (MediatR, FluentValidation)
├── Domain/          Regras de negócio, sem dependências de frameworks
├── Infrastructure/  EF Core, SQL Server, autenticação, serviços externos
└── Contracts/       Contratos HTTP públicos
tests/
├── Domain.Tests · Application.Tests · Infrastructure.Tests · Api.IntegrationTests
database/            Stored Procedures e scripts auxiliares (o schema vem das migrations)
docs/decisions/      ADRs
```

Decisões da fundação: [ADR 0002](docs/decisions/0002-foundation-decisions.md).

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) conforme `global.json` (10.0.x)
- Docker (Compose e Testcontainers)

## Configuração local

Segredos **nunca** são versionados.

```bash
cp .env.example .env     # ajuste SA_PASSWORD e JWT_SECRET
```

Para rodar a API fora do Docker (`dotnet run`), informe os segredos via user-secrets:

```bash
cd src/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FinanceDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Secret" "<segredo com pelo menos 32 caracteres>"
```

Variáveis equivalentes: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`. A API falha na inicialização
se o `Jwt:Secret` for inválido (mínimo de 32 caracteres) ou se a connection string estiver ausente.

## Executar com Docker

```bash
docker compose up -d --build
```

Sobe o SQL Server e a API em `http://localhost:8080`. A API aguarda o SQL Server ficar saudável e aplica as
migrations na inicialização (`Database__MigrateOnStartup=true`, somente neste ambiente local).

## Executar a API localmente

```bash
docker compose up -d sqlserver
dotnet tool restore
dotnet ef database update -p src/Infrastructure -s src/Api
dotnet run --project src/Api          # http://localhost:5127
```

## Testes

```bash
dotnet test Finance.Api.slnx
```

Testes de Infrastructure e Api usam SQL Server real via Testcontainers, então o **Docker precisa estar em execução**.

## Endpoints disponíveis

| Endpoint            | Descrição                                                                 |
|---------------------|---------------------------------------------------------------------------|
| `/health/live`      | Liveness: o processo está vivo. Não depende do banco.                     |
| `/health/ready`     | Readiness: dependências críticas (SQL Server). `503` se indisponível.      |
| `/health`           | Todos os health checks.                                                   |
| `/openapi/v1.json`  | Documento OpenAPI (somente em Development).                               |

Erros seguem Problem Details (RFC 7807); detalhes internos e stack traces nunca são retornados ao cliente.
Exemplos de chamadas em [`src/Api/Api.http`](src/Api/Api.http).

## Banco de dados

Migrations do EF Core em `src/Infrastructure/Persistence/Migrations`. Comandos e convenções em
[`database/README.md`](database/README.md).

## Estratégia de desenvolvimento

Evolução em fases (Foundation → Authentication → Accounts → Transactions → ...), conforme o contexto mestre.
Cada feature segue: regra de negócio → domínio → caso de uso → persistência → API → testes → documentação.
Prioridade: correção financeira, segurança e integridade dos dados. Commits seguem Conventional Commits e
decisões relevantes são registradas em ADRs.
