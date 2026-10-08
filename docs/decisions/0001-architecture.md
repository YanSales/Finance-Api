# ADR 0001 — Arquitetura da aplicação

**Status:** aceita

## Contexto

A Finance API é uma API financeira que deve ser segura, consistente e capaz de evoluir para um produto
comercial. O contexto completo está em `FINANCE_API_CONTEXT_MASTER.md`. A arquitetura precisa proteger
regras financeiras, isolar infraestrutura e continuar simples enquanto o produto é pequeno.

## Decisão

**Clean Architecture + DDD pragmático**, em uma única solução (`Finance.Api.slnx`), com projetos nomeados
sem prefixo:

| Projeto          | Responsabilidade                                                                  |
|------------------|-----------------------------------------------------------------------------------|
| `Domain`         | Entidades, agregados, value objects, regras e exceções de domínio. Sem dependências. |
| `Application`    | Casos de uso (commands/queries), validações, abstrações.                          |
| `Infrastructure` | EF Core, SQL Server, Stored Procedures, autenticação, serviços externos.          |
| `Contracts`      | Contratos HTTP públicos (requests/responses).                                     |
| `Api`            | Composition Root: HTTP, middleware, DI, OpenAPI, tratamento de erros.             |

Testes em `tests/`: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `Api.IntegrationTests`.

Direção das dependências:

```text
Api ──► Application ──► Domain
 │          │
 │          └──► Contracts
 └──► Infrastructure ──► Application, Domain
```

- Regras de negócio vivem em Domain/Application. Controllers são finos.
- SQL Server é o banco principal; EF Core para escrita e consultas simples; Dapper/Stored Procedures
  somente onde trouxerem benefício real (relatórios, agregações, operações críticas).
- CQRS é pragmático (commands modificam, queries leem), sem event sourcing nem barramentos.
- Não são adicionados componentes (Redis, mensageria, etc.) sem necessidade comprovada.

## Consequências

- Fronteiras claras e testáveis; o Domain pode ser testado sem infraestrutura.
- O `Api` referencia `Infrastructure` apenas para compor a DI.
- Há mais projetos do que em um CRUD simples; o custo é aceito em troca de isolamento.
