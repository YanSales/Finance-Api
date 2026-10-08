# ADR 0002 — Decisões da Foundation (Fase 1)

**Status:** aceita

## Contexto

A Fase 1 prepara a base técnica antes de qualquer feature de negócio. As decisões abaixo foram tomadas
deliberadamente após a análise inicial do repositório.

## Decisões

### MediatR 12.5.0 mantido

Application usa MediatR **12.5.0** e a versão foi **mantida deliberadamente**. Não atualizar nem substituir
nesta fase. Versões posteriores adotaram licenciamento comercial; qualquer upgrade exige nova decisão e
revisão dos termos de licença.

### Application → Contracts

A referência `Application → Contracts` é mantida. Os contratos fazem parte da estrutura definida para a
API e não se justifica, agora, uma camada extra de DTOs. Consequência: casos de uso podem usar tipos de
`Contracts`, então ele deve conter somente contratos públicos, nunca modelos internos.

### Assertions: Shouldly 4.2.1

`FluentAssertions` 8.x passou a exigir licença paga para uso comercial, o que conflita com o objetivo
comercial do projeto. Foi substituído por **Shouldly** (BSD-2-Clause). Não havia testes escritos, então
nenhuma migração de testes foi necessária.

### Logging: Serilog

`Serilog.AspNetCore`, configurado por `appsettings` (`Serilog`) com sink de console e request logging.
Sem sinks adicionais por enquanto; formato JSON e sinks externos entram quando houver deploy.

### TimeProvider

Uso do `TimeProvider` nativo (`TimeProvider.System` registrado na Infrastructure). Proibido `DateTime.Now`
em código de aplicação; testes futuros podem substituir a implementação.

### Política de warnings e documentação

- `TreatWarningsAsErrors` e `Nullable` permanecem ligados em todos os projetos.
- Documentação XML (`GenerateDocumentationFile`, CS1591) é exigida **somente em `Api` e `Contracts`**, que
  expõem a superfície documentada (OpenAPI/contratos). Types da Api que não precisam ser públicos são `internal`.
- Severidades explícitas em `.editorconfig` para CA1812 (tipos instanciados por DI), CA2007 (sem
  SynchronizationContext no ASP.NET Core), CA1515 (controllers/testes públicos) e CA1822 em testes.

### Quebras de linha

LF em todo o repositório (`.gitattributes` com `eol=lf` e `.editorconfig` com `end_of_line = lf`).
`.bat/.cmd` permanecem CRLF.

### Infrastructure usa o shared framework do ASP.NET Core

`Infrastructure` referencia `Microsoft.AspNetCore.App` (health checks, Options, Hosting) em vez de vários
pacotes avulsos. Por isso o pacote `Microsoft.Extensions.Identity.Core` foi removido: ele já faz parte
do framework e será usado na Fase 2 (hash de senha).

### Persistência

- `FinanceDbContext` sem entidades. A migration `InitialCreate` é vazia e valida o pipeline.
- Convenção global `decimal(19,4)` para valores monetários (decisão conceitual do contexto mestre §69).
- Migrations não são aplicadas automaticamente por padrão. `Database:MigrateOnStartup=true` existe apenas
  para o ambiente local do Docker Compose.

### OpenAPI e Health Checks

- OpenAPI nativo do .NET 10 (`Microsoft.AspNetCore.OpenApi`), exposto somente em Development.
- `/health/live` não executa checks; `/health/ready` executa os checks com a tag `ready` (SQL Server);
  `/health` executa todos.

## Consequências

- Base estável e verificável por CI antes da Fase 2 (Authentication).
- Dependências extras só entram com justificativa registrada em ADR.
