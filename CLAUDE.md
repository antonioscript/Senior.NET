# CLAUDE.md — Senior.NET

Instruções permanentes para o Claude Code neste repositório.
Estas regras se aplicam a **todas** as sessões, não precisam ser repetidas a cada conversa.

---

## O que é este projeto

Repositório de estudo e referência prática sobre o que se espera de um desenvolvedor **sênior em
.NET**: arquitetura, persistência, resiliência, observabilidade, testes, segurança, DevOps.

A ideia central é ter **mais de uma forma de resolver o mesmo problema**, lado a lado, comentada,
para servir de material de comparação — não um CRUD de tutorial, não "a melhor arquitetura
possível".

O roadmap completo (o que já existe e o que está planejado) fica em `README.md`. Antes de
implementar qualquer novo tópico, leia o `README.md` para saber onde o novo conteúdo se encaixa.

---

## Estrutura atual

```
docker-compose.yml                         # Toda infra local (hoje: Keycloak)
infra/                                     # Configs versionadas para cada serviço no compose
  keycloak/realm-export.json               #   Realm auto-importado pelo Keycloak

Apps/Api/SeniorApi/
├── Domain/                                # Entidades, value objects, regras. Zero deps externos.
├── Application/                           # Interfaces (repositórios, UoW). Implementadas em Database/.
├── Database/                              # Infraestrutura de persistência
│   ├── EntityFramework/                   #   ORM completo: DbContext, Fluent API, migrations
│   ├── Dapper/                            #   Micro-ORM: SQL manual, mapeamento manual
│   ├── MongoDb/                           #   (vazio — próximo tópico planejado)
│   └── Redis/                             #   (vazio — próximo tópico planejado)
└── SeniorApi/                             # Web API (composição de tudo acima)
    ├── Controllers/
    └── Security/                          #   JWT Keycloak: extensões de autenticação + role mapping
```

---

## Regras permanentes

### 1. Comentários com propósito educacional

Este repositório é **material de aprendizado de nível sênior**. Comentários não são opcionais.

Regras sobre o que comentar e o que não comentar:

**Sempre comentar:**
- O *porquê* de uma decisão que não é óbvia (`// Concorrência otimista: sem isso dois requests
  simultâneos podem sobrescrever dados um do outro silenciosamente`).
- Qual NuGet instalar e para que serve cada um — nas classes de configuração de infra
  (`AppDbContext`, `SqlConnectionFactory`, extensões de serviço, etc.). Formato esperado:
  ```csharp
  // Pacotes necessários (Database.csproj):
  //   Microsoft.EntityFrameworkCore.SqlServer — provider SQL Server: traduz LINQ para T-SQL
  //   Microsoft.EntityFrameworkCore.Design    — habilita `dotnet ef migrations add` em design-time
  //   Dapper                                  — micro-ORM: executa SQL e mapeia resultados para C#
  ```
- **Comandos de terminal relevantes**, sempre que uma classe de configuração ou feature tiver um
  comando de uso associado. Formato esperado no topo ou próximo à implementação:
  ```csharp
  // Comandos:
  //   dotnet tool install --global dotnet-ef          (instalar o CLI, uma vez por máquina)
  //   dotnet ef migrations add NomeDaMigration --project . --startup-project .
  //   dotnet ef database update                --project . --startup-project .
  ```
- Armadilhas e gotchas conhecidos (ex.: `aud` claim do Keycloak não inclui o client ID por padrão,
  `realm_access.roles` precisa de transformação manual para funcionar com `[Authorize(Roles=...)]`).
- Padrões não-óbvios: por que `PropertyAccessMode.Field`, por que `InternalsVisibleTo`, por que o
  Dapper precisa de `Rehydrate` interno, etc.
- Comparações entre alternativas: "EF Core faz isso automaticamente via change tracker; Dapper
  requer SQL explícito porque não tem change tracker."

**Nunca comentar:**
- O *que* o código faz quando os identificadores já dizem isso (`// Adds the customer to the list` 
  sobre um `_list.Add(customer)` — não faz sentido).
- Referências à tarefa ou sessão atual ("adicionado para Keycloak", "fix do issue #123").
- Blocos de comentário com múltiplos parágrafos explicando lógica trivial.

### 2. Toda infra local via Docker Compose

Qualquer serviço externo que o projeto precisar em desenvolvimento (banco de dados, identity server,
cache, message broker) deve ser adicionado ao `docker-compose.yml` na raiz do repositório. Não é
aceitável pedir para o desenvolvedor instalar SQL Server, Redis, Mongo, Keycloak etc. diretamente
na máquina.

**Regras de adição de um novo serviço:**

1. Adicione o serviço a `docker-compose.yml` com comentário explicando **o que é o serviço** e por
   que ele está aqui (o mesmo padrão do Keycloak já existente no arquivo).
2. Se o serviço precisar de configuração inicial (seed de dados, realm, scripts de schema), crie um
   arquivo versionado em `infra/<nome-do-servico>/` e monte como volume.
3. As strings de conexão padrão para dev ficam em `appsettings.json` e devem apontar para
   `localhost` com a porta mapeada no compose.
4. Ao adicionar o serviço ao compose, também atualize a seção "Como rodar" no `README.md`.

**Serviços planejados (adicionar quando o tópico for implementado):**

| Serviço    | Imagem sugerida                        | Porta padrão | Para que                    |
|------------|----------------------------------------|--------------|-----------------------------|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` | 1433  | EF Core + Dapper — **já no compose**, porta 1433, usuário `sa`, senha `Senior@2026!` |
| MongoDB    | `mongo:7`                              | 27017        | Tópico MongoDB              |
| Redis      | `redis:7-alpine`                       | 6379         | Tópico Redis / caching      |
| RabbitMQ   | `rabbitmq:3-management-alpine`         | 5672 / 15672 | Tópico MassTransit          |
| Jaeger     | `jaegertracing/all-in-one:latest`      | 16686        | Tópico OpenTelemetry        |
| Grafana    | `grafana/grafana:latest`               | 3000         | Tópico Observabilidade      |

> **Nota Docker Desktop:** nesta máquina o Docker Desktop **não inicia automaticamente**. Antes de
> rodar `docker compose up -d`, confirme que o Docker Desktop está aberto (ícone na bandeja do
> sistema, daemon respondendo a `docker ps`). Não tente iniciar via `Start-Process` — não funciona.

### 3. README.md é o mapa do projeto — sempre manter atualizado

O `README.md` na raiz é a fonte de verdade sobre o que o projeto cobre. Após qualquer implementação:

1. **Marcar o item como concluído** no checklist TODO (`- [ ]` → `- [x]`).
2. **Adicionar à seção "Status atual"** uma descrição do que foi implementado e onde fica o código,
   no mesmo estilo das entradas existentes.
3. **Adicionar um bloco na seção "Referência de implementação"** — um parágrafo curto no formato:
   ```markdown
   ### [Categoria] - Nome do Tópico
   O que é em 1-2 linhas.
   - **Ponto de entrada:** `Pasta/Arquivo.cs` → método ou classe principal
   - **Arquivo de config:** `appsettings.json` → seção relevante
   - **Exemplo de uso:** `Controllers/XController.cs` — o que demonstra
   ```
   O objetivo é que qualquer pessoa encontre o código em menos de 30 segundos, sem navegar o projeto
   inteiro. Não precisa repetir tudo que já está nos comentários do código — só os ponteiros.
4. **Criar uma seção detalhada** para tópicos que requerem contexto de execução (como rodar, testar,
   comandos curl). Ver "Autenticação com Keycloak" como modelo. Para tópicos puramente de código,
   o bloco da Referência de implementação já basta.
5. Se um novo serviço foi adicionado ao `docker-compose.yml`, adicionar à estrutura de arquivos no
   README e aos comandos de execução.

### 4. Commits — sem atribuição de IA

Ao criar commits neste repositório, **não adicionar** `Co-Authored-By: Claude` nem qualquer
mensagem que identifique o commit como gerado por IA. Usar apenas o autor git configurado
localmente. Escrever a mensagem de commit como se fosse qualquer outro commit humano do projeto.

---

## Padrão de implementação por tópico

Cada novo tópico adicionado a este repositório deve seguir esta sequência:

1. **Primer conceitual** — antes de escrever código, explicar na conversa (não num arquivo separado)
   o que é a tecnologia, os 3-4 termos que aparecem em todo lugar, e como ela se encaixa no que já
   existe. Isso é especialmente importante quando o usuário sinaliza que não conhece o tópico.

2. **Implementação comentada** — código com comentários educacionais conforme a regra 1 acima.

3. **Verificação end-to-end** — não basta compilar. O objetivo é sempre provar que funciona com
   uma requisição real, um teste rodando, ou um comando que retorna o resultado esperado.
   "Compilou sem erros" não é o critério de conclusão.

---

## Arquitetura e decisões já tomadas

Estas são decisões que **não devem ser revertidas** sem discussão explícita com o usuário:

- **Clean Architecture / Onion**: dependências apontam para dentro. `Domain` não referencia nada.
  `Application` referencia só `Domain`. `Database` referencia `Application` e `Domain`.
  `SeniorApi` (composição root) referencia tudo.

- **Rich Domain Model**: entidades com setters privados, construtores privados, factory methods
  (`Create`), invariantes de negócio dentro do próprio domínio.

- **Troca de ORM em uma linha**: `Program.cs` chama `AddEntityFrameworkPersistence` **ou**
  `AddDapperPersistence` — nunca os dois. Controllers, Application e Domain não sabem qual ORM está
  ativo. Isso deve ser preservado ao adicionar novos repositórios.

- **InternalsVisibleTo**: `Domain/AssemblyInfo.cs` expõe membros `internal` para `Database`,
  permitindo que os repositórios Dapper reconstruam agregados ricos via `Rehydrate` sem expor esses
  construtores ao mundo. Padrão a ser replicado se novos agregados forem adicionados.

- **Keycloak como identity server externo**: a API é apenas um *resource server* — nunca emite
  token, nunca autentica senha. `IClaimsTransformation` (KeycloakRoleClaimsTransformation) traduz
  `realm_access.roles` do Keycloak para `ClaimTypes.Role` do ASP.NET Core.

---

## Comandos úteis de referência rápida

```bash
# EF Core — gerar e aplicar migration
cd Apps/Api/SeniorApi/Database
dotnet ef migrations add NomeDaMigration --project . --startup-project .
dotnet ef database update              --project . --startup-project .

# Build completo da solução
dotnet build Apps/Api/SeniorApi/SeniorApi/SeniorApi.csproj

# Subir infra local
docker compose up -d

# Rodar a API
dotnet run --project Apps/Api/SeniorApi/SeniorApi

# Pegar token Keycloak (alice = admin, bob = customer)
curl -X POST http://localhost:8080/realms/senior-net/protocol/openid-connect/token \
  -d "client_id=senior-api&grant_type=password&username=alice&password=alice123&scope=openid"
```
