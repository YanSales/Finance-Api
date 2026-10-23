# ADR 0003 — Autenticação e autorização (Fase 2)

**Status:** aceita

## Contexto

A Fase 2 entrega a base de identidade: cadastro, login e proteção de endpoints, para que as próximas
features financeiras possam associar dados a um proprietário (ownership) com segurança.

## Decisões

### JWT Bearer, access token de curta duração

- Assinatura **HMAC-SHA256** (`HS256`), único algoritmo aceito (`ValidAlgorithms`); tokens `none`/HS512 são rejeitados.
- Validação de assinatura, emissor, público e expiração sempre ligada. `ClockSkew` de 30 s (padrão do framework: 5 min).
- Claims mínimas: `sub` (id do usuário), `email`, `role`, `jti`. Nunca senha, hash ou segredos.
- `MapInboundClaims = false`: as claims mantêm os nomes do token (`sub`, `role`).
- Emissão (`JwtAccessTokenService`) e validação (`JwtTokenValidation.CreateParameters`) usam o mesmo ponto de
  configuração em `Infrastructure.Authentication`, evitando divergência de chave/algoritmo/emissor/público.
- Configuração: `Jwt:Secret` (≥ 32 caracteres, nunca versionado), `Jwt:Issuer`, `Jwt:Audience`,
  `Jwt:AccessTokenMinutes`. Falha na inicialização se inválida (já existente) e agora também se o segredo
  for o placeholder do `.env.example` (`CHANGE_ME...`), por ser público.

### Hash de senha: ASP.NET Core Identity `PasswordHasher`

Sem algoritmo próprio e sem pacote novo (`Microsoft.Extensions.Identity.Core` faz parte do shared framework,
conforme ADR 0002). Formato V3: PBKDF2-HMAC-SHA512, salt de 128 bits, **210.000 iterações** (OWASP).
No login de usuário inexistente é verificado um hash descartável para equalizar o tempo de resposta.

### Política de senha

Comprimento 12–128, sem regras de composição (NIST SP 800-63B). O máximo evita hashing de entradas gigantes.
O login não aplica essa política e não valida o formato do e-mail (não revela regras).

### Anti-enumeração no login

Usuário inexistente, senha incorreta e conta inativa retornam a mesma resposta (`401`, mesmo título/detalhe).
No cadastro, e-mail duplicado retorna `409`: sem confirmação de e-mail isso é inerente; a mitigação
(rate limiting) está listada como pendência.

### Unicidade do e-mail

E-mail normalizado (trim + minúsculas, cultura invariante) e **índice único `UX_Users_Email`**. A checagem prévia
na aplicação só falha rápido; a garantia real é o índice. O repositório converte a violação (erros 2601/2627
do SQL Server) em `ConflictException` → `409`, cobrindo cadastros concorrentes.

### Autorização

- **Autenticação por padrão nos controllers**: `MapControllers().RequireAuthorization()` em `Program.cs`; os
  endpoints públicos declaram `[AllowAnonymous]` (cadastro e login). Health checks e OpenAPI são explicitamente
  anônimos. **Não se usa `FallbackPolicy`**: ela é aplicada também a requisições sem endpoint correspondente e
  fazia rotas inexistentes responderem `401` em vez de `404` (falha detectada na suíte de integração). Limitação
  consciente: endpoints que não sejam de controller (minimal APIs futuras) precisam de `RequireAuthorization()`
  explícito ou de um grupo de rotas com a convenção.
- Papéis mínimos (`User`, `Admin`) e a política `AdminOnly`, só para demonstrar `401` (sem/ inválido token) vs
  `403` (autenticado sem permissão). Cadastro público sempre cria `User`; não existe endpoint para promover.
  Não há RBAC/permissões granulares.
- O id do usuário vem **sempre** da claim `sub` do token validado, nunca de parâmetros do cliente.

### Persistência

Tabela `Users` (Id GUID v7 gerado pelo domínio, Email, PasswordHash, Role como texto, IsActive, CreatedAt,
UpdatedAt em `datetimeoffset`). Migration `AddUsers`. Repositório próprio do aggregate (sem generic repository
nem Unit of Work). Leituras com `AsNoTracking`.

### Validação no pipeline

`ValidationBehavior` (MediatR 12.5.0 mantido) executa os validators do FluentValidation antes dos handlers;
`ValidationException` → `400` pelo `GlobalExceptionHandler` existente.

## Fora do escopo (decisões adiadas)

Refresh tokens/rotação/revogação, recuperação de senha, confirmação de e-mail, MFA, rate limiting dos
endpoints de autenticação, bloqueio por tentativas, esquema `Bearer` na documentação OpenAPI, rehash
automático ao mudar o custo, verificação de usuário ativo a cada request (hoje só em `users/me` e no login).

## Consequências

- Features futuras já nascem protegidas por padrão e podem obter o dono via claim `sub`.
- Um token continua válido até expirar (≤ 15 min) mesmo que a conta seja desativada; revogação fica para a fase de refresh tokens.
