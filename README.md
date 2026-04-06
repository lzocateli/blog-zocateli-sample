# BlogSamples — Repositório de Exemplos do Blog

Repositório consolidado com **todos** os exemplos de código dos artigos do blog [zocate.li](https://zocate.li/), organizados por **domínio técnico**.

> Cada pasta representa um domínio (API Design, Autenticação, Mensageria, etc.) e contém classes de exemplo extraídas diretamente dos artigos.

## Repositório

```bash
git clone https://github.com/lzocateli/blog.git
cd blog/sample/dotnet-blog-sample
```

## Pré-requisitos

| Tecnologia | Versão | Obrigatório | Uso |
|-----------|--------|-------------|-----|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | **10.0.201+** (fixada via `global.json`) | Sim | Projeto principal, Blazor WASM e testes |
| [Node.js](https://nodejs.org/) | 22 LTS | Sim | Frontend Angular, Angular CLI |
| [Angular CLI](https://angular.dev/) | 19+ | Sim | Projetos frontend Angular |
| [Bun](https://bun.sh/) | latest | Não | Runtime alternativo (`search-ui-bun`) |
| [Deno](https://deno.land/) | latest | Não | Runtime alternativo (`search-ui-deno`) |
| [Python](https://www.python.org/) | 3.10+ | Não | Exemplos OAuth (Flask + MSAL) |
| [uv](https://docs.astral.sh/uv/) | latest | Não | Gerenciamento de pacotes Python |

> **Dev Container**: O Dockerfile em `.devcontainer/` já instala todas essas dependências automaticamente.

---

## Projeto .NET (Principal)

### Restaurar dependências

```bash
dotnet restore BlogSamples.sln
```

### Compilar

```bash
dotnet build BlogSamples.sln
```

### Executar a API

```bash
dotnet run --project src/BlogSamples
```

A API estará disponível em:
- HTTP: `http://localhost:5101`
- HTTPS: `https://localhost:7063`

### Executar testes

```bash
dotnet test BlogSamples.sln
```

### Configuração opcional — Application Insights

Para o domínio Logging funcionar com Application Insights, configure em `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://xxx.applicationinsights.azure.com/"
  }
}
```

Ou via variável de ambiente:

```bash
APPLICATIONINSIGHTS__CONNECTIONSTRING="InstrumentationKey=xxx;..."
```

---

## Frontend Angular (Exemplos de referência)

Os projetos em `frontend/` são **exemplos de código** extraídos dos artigos — não são aplicações Angular CLI completas. Para utilizá-los em um projeto Angular real:

### auth-spa — SPA com MSAL (Entra ID)

Artigo: [Autenticação e Autorização: JWT, OAuth2 e OpenID Connect](https://zocate.li/posts/2026/autenticacao-autorizacao-jwt-oauth2-openid/)

```bash
# Criar projeto Angular (se necessário)
ng new auth-spa --standalone
cd auth-spa

# Instalar dependências MSAL
npm install @azure/msal-angular @azure/msal-browser

# Copiar os arquivos de exemplo
cp -r ../blog/sample/dotnet-blog-sample/frontend/auth-spa/src/app/* src/app/
```

Pacotes necessários: `@azure/msal-angular`, `@azure/msal-browser`

### bff-spa — SPA com BFF (sem MSAL)

Artigo: [BFF Backend For Frontend: Segurança em SPAs](https://zocate.li/posts/2026/bff-backend-for-frontend-seguranca/)

```bash
ng new bff-spa --standalone
cd bff-spa

# Sem dependências externas — usa HttpClient nativo com XSRF
# Copiar os arquivos de exemplo
cp -r ../blog/sample/dotnet-blog-sample/frontend/bff-spa/src/app/* src/app/
```

Sem pacotes extras — usa `HttpClient` nativo do Angular com configuração XSRF.

### search-ui — Busca reativa com debounce

Artigo: [Full-Text Search em API REST](https://zocate.li/posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/)

```bash
ng new search-ui --standalone
cd search-ui

# Sem dependências externas — usa RxJS nativo do Angular
# Copiar os arquivos de exemplo
cp -r ../blog/sample/dotnet-blog-sample/frontend/search-ui/src/* src/
```

Sem pacotes extras — usa `RxJS` (já incluso no Angular).

---

## Frontend Blazor WASM (CRUD com Radzen)

Artigo: [Zero JavaScript: CRUD Completo com Blazor WASM e Radzen](https://zocate.li/posts/2026/blazor-wasm-crud-radzen-tutorial-dotnet10/)

Aplicação Blazor WebAssembly Standalone com Radzen Blazor para CRUD de Produtos e Categorias.

### Executar API + Blazor WASM

**Terminal 1 — API:**
```bash
dotnet run --project src/BlogSamples
# http://localhost:5101/docs → Swagger com endpoints Produtos e Categorias
```

**Terminal 2 — Blazor WASM:**
```bash
dotnet run --project frontend/blazor-wasm
# http://localhost:5200 → Dashboard
```

Pacotes: `Radzen.Blazor` (MIT, incluído no projeto).

---

## Python (Exemplos OAuth)

Artigo: [Autenticação e Autorização: JWT, OAuth2 e OpenID Connect](https://zocate.li/posts/2026/autenticacao-autorizacao-jwt-oauth2-openid/)

### Criar ambiente virtual e instalar dependências

```bash
cd sample/dotnet-blog-sample/python

# Criar e ativar ambiente virtual
python -m venv .venv

# Windows
.venv\Scripts\Activate.ps1

# Linux/macOS
source .venv/bin/activate

# Instalar dependências
pip install flask msal requests
```

### oauth_flask_example.py — Authorization Code Flow

Aplicação Flask com login interativo via Azure Entra ID.

```bash
# Configurar variáveis de ambiente
$env:AZURE_CLIENT_ID="<seu-client-id>"
$env:AZURE_CLIENT_SECRET="<seu-client-secret>"
$env:AZURE_TENANT_ID="<seu-tenant-id>"
$env:FLASK_SECRET_KEY="<chave-secreta>"

# Executar
python oauth_flask_example.py
```

### oauth_daemon_example.py — Client Credentials (M2M)

Serviço daemon sem interação de usuário (machine-to-machine).

```bash
# Configurar variáveis de ambiente
$env:AZURE_CLIENT_ID="<seu-client-id>"
$env:AZURE_CLIENT_SECRET="<seu-client-secret>"
$env:AZURE_TENANT_ID="<seu-tenant-id>"

# Executar
python oauth_daemon_example.py
```

---

## SQL (Scripts Full-Text Search)

Artigo: [Full-Text Search em API REST](https://zocate.li/posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/)

Scripts de setup para Full-Text Search em diferentes bancos de dados. Execute no cliente SQL do banco correspondente:

| Arquivo | Banco |
|---------|-------|
| `sql/fts-sqlserver-setup.sql` | SQL Server |
| `sql/fts-postgres-setup.sql` | PostgreSQL |
| `sql/fts-oracle-setup.sql` | Oracle |

## Artigos e Domínios

| # | Domínio | Artigo | Pasta |
|---|---------|--------|-------|
| 1 | ApiDesign | [Design de APIs REST: Verbos HTTP e Parameter Binding](https://zocate.li/posts/2025/design-api-rest-verbos-http-parameter-binding-aspnet-core/) | `src/BlogSamples/ApiDesign/` |
| 2 | AsyncParallel | [Programação Assíncrona em C#: async/await e Threads](https://zocate.li/posts/2025/programacao-assincrona-csharp-async-await/) | `src/BlogSamples/AsyncParallel/` |
| 3 | Authentication | [Keycloak: Autenticação Grátis com Container e C#](https://zocate.li/posts/2025/keycloak-autenticacao-gratuita-container-csharp/) | `src/BlogSamples/Authentication/Keycloak/` |
| 4 | Messaging | [Gargalo em Banco de Dados: Mensageria e Paginação](https://zocate.li/posts/2026/gargalo-banco-dados-efcore-mensageria-paginacao/) | `src/BlogSamples/Messaging/` |
| 5 | Authentication | [Autenticação e Autorização: JWT, OAuth2 e OpenID Connect](https://zocate.li/posts/2026/autenticacao-autorizacao-jwt-oauth2-openid/) | `src/BlogSamples/Authentication/EntraId/` + `frontend/auth-spa/` + `python/` |
| 6 | AsyncParallel | [Paralelismo em C#: Parallel, PLINQ e Tasks na Prática](https://zocate.li/posts/2025/paralelismo-csharp-parallel-tasks/) | `src/BlogSamples/AsyncParallel/` |
| 7 | DesignPatterns | [Padrões GoF: Código à Nuvem, Monólito ao Microserviço](https://zocate.li/posts/2025/arquitetura-software-gof-padroes-cloud-microservicos/) | `src/BlogSamples/DesignPatterns/` |
| 8 | Logging | [Log Sem Contexto é Ruído: Logging Dinâmico e Estruturado no .NET 8](https://zocate.li/posts/2026/logging-estruturado-dinamico-dotnet8-azure-appinsights/) | `src/BlogSamples/Logging/` |
| 9 | DataAccess | [Paginação em APIs REST com C# e EF Core](https://zocate.li/posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/) | `src/BlogSamples/DataAccess/Pagination/` |
| 10 | DataAccess | [EF Core Migrations: Multi-Projeto, Secrets e Scaffolding](https://zocate.li/posts/2026/efcore-migrations-multi-projeto-secrets-scaffolding/) | `src/BlogSamples/DataAccess/Migrations/` |
| 11 | DataAccess | [EF Core 8 Fluent API: Mapeamento e Desacoplamento](https://zocate.li/posts/2026/efcore-fluent-api-mapeamento-desacoplamento/) | `src/BlogSamples/DataAccess/FluentApi/` |
| 12 | Workers | [.NET Worker e Background Service: Alto Volume](https://zocate.li/posts/2026/dotnet-worker-background-service-processamento-alto-volume/) | `src/BlogSamples/Workers/` |
| 13 | Authentication | [BFF Backend For Frontend: Segurança em SPAs](https://zocate.li/posts/2026/bff-backend-for-frontend-seguranca/) | `src/BlogSamples/Authentication/Bff/` + `frontend/bff-spa/` |
| 14 | DataAccess | [Full-Text Search em API REST: C#, SQL Server e PostgreSQL](https://zocate.li/posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/) | `src/BlogSamples/DataAccess/FullTextSearch/` + `frontend/search-ui/` + `sql/` |
| 15 | Produtos | [Zero JavaScript: CRUD Completo com Blazor WASM e Radzen](https://zocate.li/posts/2026/blazor-wasm-crud-radzen-tutorial-dotnet10/) | `src/BlogSamples/Produtos/` + `frontend/blazor-wasm/` |

## Estrutura do Projeto

```
BlogSamples.sln
├── src/BlogSamples/                          # Projeto principal (.NET 10 Web API)
│   ├── ApiDesign/                            # CRUD, Parameter Binding, Models
│   ├── AsyncParallel/                        # async/await, CancellationToken, Parallel, PLINQ, Semaphore
│   ├── Authentication/
│   │   ├── Keycloak/                         # JWT + Keycloak (Controller, AdminService)
│   │   ├── EntraId/                          # Microsoft Entra ID (DadosController)
│   │   └── Bff/                              # Backend For Frontend (YARP proxy, CSRF, sessão)
│   ├── DataAccess/
│   │   ├── Pagination/                       # Offset, Keyset, Streaming, Time, HATEOAS
│   │   ├── Migrations/                       # IDesignTimeDbContextFactory
│   │   ├── FluentApi/                        # Entities, EntityConfigurations (1:1, 1:N, N:N, TPH, JSON, owned)
│   │   └── FullTextSearch/                   # SQL Server, PostgreSQL, Oracle — SearchService, Repositories
│   ├── DesignPatterns/
│   │   ├── Creational/                       # Factory Method, Abstract Factory, Builder, Prototype, Singleton
│   │   ├── Structural/                       # Adapter, Decorator, Facade, Proxy
│   │   ├── Behavioral/                       # Strategy, Observer, Command, CQRS
│   │   └── Cloud/                            # Circuit Breaker (Polly), Adapter anti-lock-in
│   ├── Logging/                              # Logging estruturado, dinâmico, App Insights
│   ├── Messaging/                            # RabbitMQ, Azure Service Bus, Batch insert
│   ├── Produtos/                             # API Produtos CRUD (Minimal API, in-memory)
│   │   └── Models/                           # Produto, Categoria, DTOs, Requests
│   ├── Workers/                              # BackgroundService, IHostedService, Graceful Shutdown
│   ├── Endpoints/                            # Minimal API endpoints (logging demo)
│   └── Models/                               # Shared models
├── tests/BlogSamples.Tests/                  # xUnit + NSubstitute (13 testes)
├── frontend/
│   ├── auth-spa/                             # Angular 16+ SPA com MSAL (Entra ID)
│   ├── blazor-wasm/                          # Blazor WASM Standalone + Radzen Blazor (CRUD Produtos)
│   │   ├── Pages/                            # Index (Dashboard), Produtos, ProdutoForm, Categorias
│   │   ├── Services/                         # ProdutoApiService, CategoriaApiService
│   │   └── Models/                           # DTOs, Requests (client-side)
│   ├── bff-spa/                              # Angular 16+ SPA com BFF (sem MSAL)
│   └── search-ui/                            # Angular — busca reativa com debounce
├── python/
│   ├── oauth_flask_example.py                # Flask + MSAL (Authorization Code)
│   └── oauth_daemon_example.py               # M2M Client Credentials
├── sql/
│   ├── fts-sqlserver-setup.sql               # Full-Text Search — SQL Server
│   ├── fts-postgres-setup.sql                # Full-Text Search — PostgreSQL
│   └── fts-oracle-setup.sql                  # Full-Text Search — Oracle Text
└── global.json                               # .NET SDK 10.0.201+ fixado
```

## Adicionando Exemplos de Novos Artigos

Para artigos futuros, basta:

1. **Identificar o domínio técnico** do artigo (ex: `DataAccess`, `Authentication`, `Workers`, etc.)
2. **Criar uma nova pasta** dentro do domínio correspondente em `src/BlogSamples/`
   - Se o domínio já existe: crie uma subpasta (ex: `DataAccess/NovaFeature/`)
   - Se é um domínio novo: crie a pasta na raiz do projeto (ex: `src/BlogSamples/NovoDomnio/`)
3. **Adicionar as classes** com o namespace `BlogSamples.<Domínio>.<Subdomínio>`
4. **Adicionar NuGet packages** necessários ao `BlogSamples.csproj`
5. **Atualizar este README** — tabela de artigos e estrutura
6. **Compilar e testar**: `dotnet build BlogSamples.sln && dotnet test BlogSamples.sln`

### Exemplo: adicionando artigo sobre gRPC

```
src/BlogSamples/
└── Communication/          ← novo domínio
    └── Grpc/               ← subpasta do artigo
        ├── GreeterService.cs
        └── GrpcClientExample.cs
```

```csharp
namespace BlogSamples.Communication.Grpc;

public class GreeterService { /* ... */ }
```

### Convenções

- **Namespace**: `BlogSamples.<Domínio>[.<Subdomínio>]`
- **Cada arquivo** deve ter um comentário header com o artigo de origem e URL
- **Classes são exemplos didáticos** — não precisam de testes (exceto Logging que já tem)
- **Código em português** (nomes de variáveis, classes) conforme os artigos

## Imagens Docker Base

Para criar imagens customizadas a partir das oficiais da Microsoft, para uso como base do dev container ou pipelines CI/CD:

### .NET SDK 8.0.419

```bash
docker pull mcr.microsoft.com/dotnet/sdk:8.0.419-jammy-amd64
docker tag mcr.microsoft.com/dotnet/sdk:8.0.419-jammy-amd64 lzocateli/dotnet-sdk:8.0.419-jammy-amd64
docker push lzocateli/dotnet-sdk:8.0.419-jammy-amd64
```

### ASP.NET Core Runtime 8.0.15

```bash
docker pull mcr.microsoft.com/dotnet/aspnet:8.0.15-jammy-amd64
docker tag mcr.microsoft.com/dotnet/aspnet:8.0.15-jammy-amd64 lzocateli/aspnet:8.0.15-jammy-amd64
docker push lzocateli/aspnet:8.0.15-jammy-amd64
```

> **Nota**: A versão do Runtime ASP.NET que acompanha o SDK 8.0.419 é a **8.0.15**. Verifique em [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0) a correspondência exata de versões.

### .NET SDK 10.0.201 (Noble)

```bash
docker pull mcr.microsoft.com/dotnet/sdk:10.0.201-noble
docker tag mcr.microsoft.com/dotnet/sdk:10.0.201-noble lzocateli/dotnet-sdk:10.0.201-noble
docker push lzocateli/dotnet-sdk:10.0.201-noble
```

### ASP.NET Core Runtime 10.0.1 (Noble)

```bash
docker pull mcr.microsoft.com/dotnet/aspnet:10.0.1-noble
docker tag mcr.microsoft.com/dotnet/aspnet:10.0.1-noble lzocateli/aspnet:10.0.1-noble
docker push lzocateli/aspnet:10.0.1-noble
```

> **Nota**: A versão do Runtime ASP.NET que acompanha o SDK 10.0.201 é a **10.0.1**. Verifique em [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) a correspondência exata de versões. A imagem base mudou de **Jammy** (Ubuntu 22.04) para **Noble** (Ubuntu 24.04 LTS).

## Licença

MIT
