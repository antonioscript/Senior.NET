# Senior.NET

Repositório de estudo e referência prática sobre o que normalmente se espera do dia a dia de um(a)
desenvolvedor(a) **sênior em .NET**: arquitetura, persistência, resiliência, observabilidade,
testes e tudo o que cerca a construção de uma API "de verdade" - não um CRUD de tutorial.

A ideia não é ter "a melhor arquitetura possível", e sim ter, no mesmo repositório, **mais de uma
forma de resolver o mesmo problema**, lado a lado, comentada, para servir de material de consulta
e comparação (ex.: EF Core vs Dapper, Controllers vs Minimal APIs, etc.).

## Onde está o código

```
docker-compose.yml           # Infra local (SQL Server + Keycloak)
infra/keycloak/              # Realm pré-configurado, importado automaticamente pelo Keycloak

Apps/Api/
├── SeniorApi/               # API 1 — Clean Architecture + Controllers
│   ├── Domain/              #   Entidades, value objects, interfaces de repositório (IAggregateRoot)
│   ├── Application/         #   IUnitOfWork, ILinqShowcaseRepository
│   ├── Database/            #   Persistência: EF Core e Dapper lado a lado
│   │   ├── EntityFramework/ #     DbContext, Fluent API, migrations, LINQ showcase
│   │   ├── Dapper/          #     SQL manual, multi-mapping
│   │   ├── MongoDb/         #     (vazio — ver TODO)
│   │   └── Redis/           #     (vazio — ver TODO)
│   └── SeniorApi/           #   Host: Controllers, Keycloak JWT, DI composition root
│       └── Security/        #     JWT Bearer + role claims transformation
│
└── VerticalSliceApi/        # API 2 — Vertical Slice + Minimal APIs + MediatR
    └── Features/            #   Uma pasta por feature, sem camadas horizontais
        ├── Customers/       #     GetCustomers, GetCustomerById, CreateCustomer
        └── Products/        #     GetProducts
```

Cada classe de configuração (`Database/EntityFramework/AppDbContext.cs`,
`Database/Dapper/SqlConnectionFactory.cs`, e os arquivos em `Configurations/`) tem comentários
explicando **o que cada pacote NuGet faz**, por que ele é necessário e qual a decisão de design
por trás do código - a intenção é que esses comentários expliquem o "porquê", não o "o quê".

## Status atual

- [x] Domain: `Customer`, `Product`, `Order`/`OrderItem` (aggregate root), value object `Email`,
      `Entity`/`AuditableEntity` como base, enum `OrderStatus`.
- [x] Application: abstrações de persistência (`ICustomerRepository`, `IProductRepository`,
      `IOrderRepository`, `IUnitOfWork`) - implementadas duas vezes, uma por ORM.
- [x] **EF Core**: `AppDbContext`, `IEntityTypeConfiguration<T>` por entidade (owned type para
      `Email`, conversão de enum, `rowversion` para concorrência otimista, navegação via backing
      field), `IDesignTimeDbContextFactory` para rodar `dotnet ef` sem o host web, migration
      inicial gerada e versionada (`EntityFramework/Migrations/`).
- [x] **Dapper**: `SqlConnectionFactory`, repositórios com SQL parametrizado, multi-mapping
      manual (`Order` + `OrderItem` via `LEFT JOIN`/`splitOn`), reconstrução do domínio rico via
      fábricas internas `Rehydrate` (ver `Domain/AssemblyInfo.cs`), concorrência otimista feita à
      mão comparando `RowVersion` no `WHERE`.
- [x] Script SQL de referência (`Database/Scripts/001_InitialSchema.sql`) - o mesmo schema que a
      migration do EF Core gera, para o caminho Dapper-only.
- [x] DI: `AddEntityFrameworkPersistence` / `AddDapperPersistence` em `Program.cs` - troca de ORM é
      uma linha, nada em `Application`/`Domain`/Controllers muda.
- [x] **SQL Server em container**: movido de instância local para `docker-compose.yml`
      (`mcr.microsoft.com/mssql/server:2022-latest`, porta 1433). Migrations aplicadas
      automaticamente no startup via `db.Database.MigrateAsync()` em `Program.cs` — sem precisar
      rodar `dotnet ef database update` manualmente.
- [x] **LINQ showcase** (`Database/EntityFramework/Repositories/EfLinqShowcaseRepository.cs`,
      exposto em `SeniorApi/Controllers/LinqShowcaseController.cs`): 10 técnicas LINQ com EF Core,
      cada uma com comentário explicando o SQL gerado e o trade-off — projection (`Select`),
      paginação (`Skip`/`Take`), agregação (`GroupBy`/`Sum`/`Count`), `Any`/`All`, `SelectMany`,
      `AsSplitQuery`, `FromSql` e compiled queries.
- [x] **Keycloak**: servidor de identidade rodando via `docker-compose.yml`, realm pré-configurado
      (`infra/keycloak/realm-export.json`), API validando os tokens JWT que ele emite
      (`SeniorApi/Security/`), com tradução das roles do Keycloak para o modelo de roles do
      ASP.NET Core e dois endpoints de exemplo (`MeController`, `CustomersController`). Ver seção
      [Autenticação com Keycloak](#autenticação-com-keycloak) abaixo.

## TODO - o que esse projeto pretende cobrir

Marcado o que já existe; o resto é o roadmap de tópicos "sênior" a explorar aqui dentro.

### Persistência / ORMs
- [x] Entity Framework Core (Fluent API, migrations, change tracker, concorrência otimista)
- [x] LINQ com EF Core (projection, pagination, GroupBy, Any/All, SelectMany, AsSplitQuery, FromSql, compiled queries)
- [x] Dapper (SQL manual, multi-mapping, sem change tracker)
- [ ] MongoDB (driver oficial) - documento vs relacional, quando faz sentido
- [ ] Redis - cache distribuído, cache-aside pattern, invalidação
- [ ] Comparação de performance (BenchmarkDotNet) entre EF Core e Dapper para as mesmas queries
- [ ] EF Core: `AsSplitQuery` vs query única, `AsNoTracking`, interceptors (auditoria automática),
      compiled queries, bulk operations
- [ ] Dapper: `Dapper.Contrib`/`Dapper.FluentMap`, prepared statements, paginação eficiente

### Arquitetura e padrões
- [x] Repository Pattern (Microsoft's approach: interfaces no Domain, por aggregate root, sem generic IRepository<T>)
- [ ] CQRS com MediatR (separar leitura de escrita explicitamente)
- [ ] Result Pattern / `OneOf` (erros como valores, sem exceptions para fluxo de controle)
- [ ] Specification Pattern (queries compostas e reutilizáveis)
- [ ] Domain Events + Outbox Pattern (consistência eventual entre agregados/serviços)
- [ ] Validation pipeline com FluentValidation (e onde ela vive: Application vs API)
- [ ] Modular Monolith vs Microsserviços - quando migrar e por quê

### API
- [x] Controllers (SeniorApi) — class-based, Clean Architecture, todos os tópicos anteriores
- [x] Minimal APIs + Vertical Slice + MediatR (VerticalSliceApi) — feature folders, sem camadas, CQRS nativo
- [ ] Versionamento de API (URL, header, media type)
- [ ] `ProblemDetails` para erros padronizados (RFC 9457)
- [ ] Rate limiting (`Microsoft.AspNetCore.RateLimiting`)
- [ ] Output/Response caching
- [ ] Idempotência — `Idempotency-Key` no header, deduplicação de requests (evitar side effects duplicados em caso de retry)
- [ ] gRPC (contrato, streaming, quando preferir sobre REST)
- [ ] GraphQL (HotChocolate) como alternativa

### Segurança
- [x] JWT Bearer validando tokens de um provedor OIDC externo (Keycloak)
- [x] OAuth2 / OpenID Connect via Keycloak (realm, client, roles, discovery document)
- [ ] Authorization policies customizadas (além de `[Authorize(Roles = ...)]`) - `IAuthorizationHandler`,
      policy baseada em recurso (ex.: "só o próprio customer pode editar seus dados")
- [ ] ASP.NET Core Identity (cenário sem IdP externo, para comparar com o caminho Keycloak)
- [ ] Client Credentials flow (autenticação serviço-a-serviço, sem usuário envolvido)
- [ ] Secrets management (User Secrets em dev, Key Vault/Parameter Store em produção)
- [ ] Data Protection API

### Concorrência e exclusão mútua
- [ ] Distributed lock com SQL Server (`sp_getapplock` — lock de aplicação gerenciado pelo próprio banco)
- [ ] Distributed lock com Redis (RedLock.net — lock cross-instância via TTL, sem ponto único de falha)
- [ ] SemaphoreSlim — limitar paralelismo dentro de um processo (ex.: throttle de chamadas a API externa)

### Resiliência
- [ ] Polly: retry, circuit breaker, timeout, bulkhead
- [ ] `HttpClientFactory` com políticas de resiliência integradas
- [ ] Health checks (`/health`, dependências externas)

### Observabilidade
- [ ] Logging estruturado com Serilog (sinks, enrichers, correlação de request)
- [ ] OpenTelemetry (traces, métricas, integração com Grafana/Jaeger)
- [ ] `Application Insights` ou equivalente

### Mensageria e processamento assíncrono
- [ ] MassTransit + RabbitMQ/Kafka (pub/sub, integration events)
- [ ] Background jobs: `IHostedService`, Hangfire ou Quartz.NET
- [ ] Padrão Outbox/Inbox para garantir entrega exactly-once-ish

### Testes
- [ ] Testes unitários (xUnit + FluentAssertions) para regras de domínio
- [ ] Testes de integração com `WebApplicationFactory`
- [ ] Testcontainers (SQL Server/Mongo/Redis reais em container, não mocks, nos testes de
      integração dos repositórios)
- [ ] Testes de arquitetura (ex.: `Domain` não pode referenciar `Database`)

### DevOps / Infraestrutura
- [ ] Dockerfile para a API (multi-stage build, imagem mínima)
- [ ] CI/CD pipeline com GitHub Actions (build → testes → lint → deploy)
- [ ] Feature flags

## Referência de implementação

Mapa rápido de onde cada conceito vive no código. O objetivo é encontrar qualquer coisa em menos de
30 segundos, sem precisar navegar o projeto inteiro.

---

### [Arquitetura] Vertical Slice + Minimal APIs + MediatR (VerticalSliceApi)
Cada feature é uma pasta auto-contida com Query/Command, Handler e Endpoint no mesmo arquivo.
Sem camadas, sem repository interfaces. O Handler acessa o DbContext diretamente.
- **Ponto de entrada:** `Apps/Api/VerticalSliceApi/Program.cs` — AddMediatR + AddEndpoints + MapEndpoints
- **Auto-discovery de endpoints:** `Features/EndpointExtensions.cs` + `Features/IEndpoint.cs`
- **Feature completa de exemplo:** `Features/Customers/GetCustomers.cs` — comentário no topo compara com SeniorApi
- **Command (escrita) de exemplo:** `Features/Customers/CreateCustomer.cs` — SaveChangesAsync direto no handler
- **Porta:** 5000 (HTTP) — rodar com `dotnet run --project Apps/Api/VerticalSliceApi`

---

### [Arquitetura] Repository Pattern (Microsoft's approach)
Interfaces de repositório vivem no `Domain` (por aggregate root), não na Application. Nenhum
`IRepository<T>` genérico. O marker `IAggregateRoot` torna a regra visível em compile time.
- **Por que no Domain:** `Domain/Common/IAggregateRoot.cs` — comentário explica o padrão completo e o anti-padrão genérico
- **Aggregate roots marcados:** `Domain/Customers/Customer.cs:10`, `Domain/Orders/Order.cs:11`, `Domain/Products/Product.cs:6`
- **Interfaces de repositório:** `Domain/Customers/ICustomerRepository.cs`, `Domain/Orders/IOrderRepository.cs`, `Domain/Products/IProductRepository.cs`
- **IUnitOfWork** (application concern, não domain): `Application/Abstractions/Persistence/IUnitOfWork.cs`

---

### [Persistência] EF Core
ORM completo: gera SQL a partir de LINQ, controla estado das entidades via change tracker, e evolui
o schema via migrations versionadas.
- **DbContext:** `Database/EntityFramework/AppDbContext.cs:43` — `ApplyConfigurationsFromAssembly` na linha 58
- **Configurações Fluent API:** `Database/EntityFramework/Configurations/` — uma classe por entidade
- **Design-time factory** (permite `dotnet ef` sem rodar o host): `Database/EntityFramework/AppDbContextFactory.cs:16`
- **Repositórios:** `Database/EntityFramework/Repositories/Ef*.cs` — implementam as interfaces de `Domain/`
- **Registro no DI:** `Database/EntityFramework/EntityFrameworkServiceCollectionExtensions.cs:17` → `AddEntityFrameworkPersistence()`
- **Auto-migration no startup:** `SeniorApi/Program.cs:34` → `db.Database.MigrateAsync()`
- **Migration gerada:** `Database/EntityFramework/Migrations/`

---

### [Persistência] Dapper
Micro-ORM: SQL escrito à mão, sem change tracker. Troca de uma linha em `Program.cs` → mesma API,
outro ORM (implementa as mesmas interfaces de `Domain/`).
- **Fábrica de conexão:** `Database/Dapper/SqlConnectionFactory.cs`
- **Mapeamento de linhas SQL:** `Database/Dapper/Rows.cs` — nullable para LEFT JOINs (ver comentário)
- **Repositórios:** `Database/Dapper/Repositories/Dapper*.cs`
- **Schema manual:** `Database/Scripts/001_InitialSchema.sql`
- **Registro no DI:** `Database/Dapper/DapperServiceCollectionExtensions.cs:14` → `AddDapperPersistence()`

---

### [Persistência] LINQ com EF Core
10 técnicas LINQ, cada uma com comentário explicando o SQL gerado e o trade-off.
- **Implementação + comentários:** `Database/EntityFramework/Repositories/EfLinqShowcaseRepository.cs`
- **Interface:** `Application/Abstractions/Persistence/ILinqShowcaseRepository.cs`
- **Endpoints para testar:** `SeniorApi/Controllers/LinqShowcaseController.cs` → `GET /api/linq/*`
- **Técnicas:** `Select` (projection), `Skip`/`Take` (paginação), `GroupBy`/`Sum`, `Any`/`All`, `SelectMany`, `AsSplitQuery`, `FromSql`, compiled query

---

### [Segurança] Keycloak / JWT Bearer
Keycloak é um servidor de identidade externo (Java, roda em container). Implementa OAuth2/OIDC:
emite tokens JWT assinados. A API **nunca vê senha** — só valida o JWT recebido no header
`Authorization: Bearer`. É apenas um *resource server*.
- **Setup do JWT Bearer:** `SeniorApi/Security/KeycloakAuthenticationExtensions.cs:36` → `AddKeycloakAuthentication()` — comentário na linha 8 explica Authority, JWKS, audience
- **Tradução de roles** (`realm_access.roles` → `ClaimTypes.Role`): `SeniorApi/Security/KeycloakRoleClaimsTransformation.cs:17` — parsing do JSON na linha 34
- **Realm pré-configurado** (client, roles `admin`/`customer`, usuários `alice`/`bob`): `infra/keycloak/realm-export.json`
- **Registro no DI:** `SeniorApi/Program.cs:23` → `builder.Services.AddKeycloakAuthentication(builder.Configuration)`
- **Config de dev:** `SeniorApi/appsettings.json` → seção `"Keycloak"` (Authority, Audience, RequireHttpsMetadata)
- **Exemplo `[Authorize]` e `[Authorize(Roles = "admin")]`:** `SeniorApi/Controllers/MeController.cs:20` e `CustomersController.cs:34`

---

## Como rodar — VerticalSliceApi

```bash
# Pré-requisito: SQL Server rodando (docker compose up -d) e SeniorApi rodado ao menos uma vez
# para aplicar as migrations (ou rode SeniorApi primeiro para criar o banco).

dotnet run --project Apps/Api/VerticalSliceApi
```

Endpoints disponíveis (sem autenticação — foco no padrão arquitetural):
```
GET  http://localhost:5000/api/customers
GET  http://localhost:5000/api/customers/{id}
POST http://localhost:5000/api/customers     body: { "name": "Alice", "email": "alice@test.com" }
GET  http://localhost:5000/api/products
```

Curls de teste:
```bash
curl http://localhost:5000/api/customers
curl http://localhost:5000/api/products
curl -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Test User","email":"test@example.com"}'
```

> **Nota:** ambas as APIs apontam para o mesmo banco (`SeniorApiDb`). Os dados criados no
> VerticalSliceApi aparecem no SeniorApi e vice-versa. Isso é intencional — o ponto de comparação
> é a arquitetura, não o dado.

---

## Como rodar (fluxo completo)

```bash
# 1. Sobe SQL Server + Keycloak (Docker Desktop precisa estar aberto)
docker compose up -d

# 2. Roda a API — migrations EF Core são aplicadas automaticamente no startup
dotnet run --project Apps/Api/SeniorApi/SeniorApi
```

A API cria o banco e aplica todas as migrations na inicialização. Não precisa de nenhum passo
manual de `dotnet ef database update`.

Endpoints LINQ disponíveis (requerem token — ver seção Keycloak abaixo):
```
GET /api/linq/products/summary
GET /api/linq/orders/paged?status=Pending&page=1&pageSize=5
GET /api/linq/revenue-by-customer
GET /api/linq/orders/count-by-status
GET /api/linq/products/has-low-stock?threshold=5
GET /api/linq/products/all-in-stock
GET /api/linq/customers/{id}/all-items
GET /api/linq/customers/with-orders-split
GET /api/linq/products/search?term=laptop
GET /api/linq/customers/by-email?email=alice@example.com
```

## Como gerar/aplicar a migration do EF Core

```bash
cd Apps/Api/SeniorApi/Database
dotnet ef migrations add NomeDaMigration --project . --startup-project .
dotnet ef database update              --project . --startup-project .
```

O `AppDbContextFactory` (design-time factory) permite usar o próprio projeto `Database` como
`--startup-project` - não é preciso referenciar `Microsoft.EntityFrameworkCore.Design` no projeto
Web nem subir o host inteiro só para gerar uma migration.

## Para rodar o caminho Dapper-only

Sem migrations: aplique `Database/Scripts/001_InitialSchema.sql` direto no banco e troque, em
`SeniorApi/Program.cs`, a linha `AddEntityFrameworkPersistence` por `AddDapperPersistence`.

## Autenticação com Keycloak

### Conceitos básicos (se você nunca usou Keycloak)

Keycloak é um **servidor de identidade** (Java, roda no próprio container, não é uma lib do seu
projeto). Ele implementa OAuth2 e OpenID Connect (OIDC), que são protocolos - não produtos -
para "fulano provou quem é e tem permissão X". Quatro termos que aparecem em todo lugar:

- **Realm**: um inquilino isolado dentro do Keycloak - usuários, roles e clients de um realm não
  enxergam os de outro. Este projeto usa um único realm, `senior-net`
  (`infra/keycloak/realm-export.json`).
- **Client**: a aplicação que vai pedir tokens ao Keycloak em nome de um usuário (ou de si mesma).
  Aqui o client é `senior-api`, configurado como *público* (sem segredo) e com
  *Direct Access Grants* habilitado - **isso é uma escolha deliberada para facilitar testes via
  curl sem precisar de um frontend com redirect/browser**. Em produção, um frontend real usaria o
  Authorization Code flow com PKCE; serviço-a-serviço usaria Client Credentials. Nenhum dos dois
  precisa de senha de usuário, então não estão implementados aqui ainda (ver TODO de Segurança).
- **Token de acesso (access token)**: um JWT assinado pelo Keycloak, com prazo de expiração curto,
  que prova "esse usuário se autenticou, tem essas roles, esse token vale até tal hora". A
  `SeniorApi` nunca vê a senha do usuário - só recebe esse token já pronto no header
  `Authorization: Bearer <token>` e confere a assinatura/validade.
- **Realm roles**: `admin` e `customer`, atribuídas aos dois usuários de teste (`alice`/`bob`) já
  no `realm-export.json`. Elas chegam dentro do token num claim aninhado, `realm_access.roles` -
  **não** como roles "soltas" no formato que o ASP.NET Core espera por padrão. É por isso que
  existe `SeniorApi/Security/KeycloakRoleClaimsTransformation.cs`: ele lê esse JSON aninhado e
  projeta cada role para o formato que `[Authorize(Roles = "admin")]` sabe ler.

A API (`SeniorApi`) é só o que se chama de **resource server**: ela nunca emite token, nunca
mostra tela de login, só valida o que recebe. Quem faz login e emite o token é sempre o Keycloak.

### Como rodar

```bash
# na raiz do repo
docker compose up -d
```

Isso sobe o Keycloak em `http://localhost:8080` e, por causa de `start-dev --import-realm`, já
importa o realm `senior-net` (client, roles, usuários `alice`/`bob`) automaticamente - sem precisar
clicar em nada no console de administração. Console de admin: `http://localhost:8080` (usuário
`admin`/`admin`, esse é só o admin do *Keycloak em si*, não tem relação com o realm `senior-net`).

Depois suba a API normalmente (`dotnet run --project Apps/Api/SeniorApi/SeniorApi`).

### Como testar (obter um token e chamar a API)

```bash
# 1. Pega um token para a usuária "alice" (role: admin)
curl -X POST http://localhost:8080/realms/senior-net/protocol/openid-connect/token \
  -d "client_id=senior-api" \
  -d "grant_type=password" \
  -d "username=alice" \
  -d "password=alice123" \
  -d "scope=openid"
# resposta: { "access_token": "...", "expires_in": 600, ... }

# 2. Usa esse token para chamar a API
TOKEN="<cole o access_token aqui>"
curl http://localhost:5177/api/me -H "Authorization: Bearer $TOKEN"
curl http://localhost:5177/api/me/admin-only -H "Authorization: Bearer $TOKEN"   # 200, alice é admin
curl http://localhost:5177/api/customers -H "Authorization: Bearer $TOKEN"      # 200, só precisa estar logado

# 3. Repita com bob/bob123 (role: customer) e note a diferença:
#    /api/me/admin-only -> 403 (autenticado, mas sem a role)
#    sem token nenhum    -> 401 em qualquer rota [Authorize] (nem chegou a autenticar)
```

`grant_type=password` (Resource Owner Password Credentials) só está habilitado aqui porque é a
forma mais simples de tirar um token via curl sem um frontend - não é o fluxo recomendado para
aplicações reais com usuário final (use Authorization Code + PKCE) nem está habilitado para
clients confidenciais/serviços (use Client Credentials).
