# Structured Logging Sample — .NET 8 + Azure Application Insights

Aplicação de exemplo que demonstra uma estratégia completa de **logging estruturado, dinâmico e contextual** no .NET 8, integrada ao Azure Application Insights.

> **📖 Artigo completo:** [Log Sem Contexto é Ruído: Logging Dinâmico e Estruturado no .NET 8](https://zocate.li/posts/2026/logging-estruturado-dinamico-dotnet8-azure-appinsights/)

## O que esta aplicação demonstra

| Feature | Descrição |
|---------|-----------|
| **Logging Estruturado** | Propriedades tipadas com `[LoggerMessage]` source generator — sem interpolação, sem boxing |
| **Enriquecimento Automático** | Todo log inclui `ApplicationName`, `Version`, `HostName`, `Environment`, `CorrelationId` |
| **Nível de Log Dinâmico** | Endpoint admin para alterar o nível de log em runtime com timer de reversão automática |
| **Application Insights** | Integração completa com `TelemetryInitializer` — contexto da aplicação em toda telemetria |
| **SonarQube Compliant** | Aderente às regras S6664, S2629 e S6667 |
| **Testável** | Testes unitários com xUnit e NSubstitute |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (versão 8.0.418 fixada via `global.json`)

## Como Rodar

```bash
# Clonar o repositório
git clone https://github.com/lzocateli/dotnet-structured-logging-sample.git
cd dotnet-structured-logging-sample

# Rodar a aplicação
dotnet run --project src/SampleApi
```

A API estará disponível em `http://localhost:5000`.

## Endpoints

### Admin — Controle de Nível de Log

```bash
# Consultar nível atual
curl http://localhost:5000/api/admin/log-level

# Alterar para Debug por 15 minutos (reverte automaticamente)
curl -X POST http://localhost:5000/api/admin/log-level \
  -H "Content-Type: application/json" \
  -d '{"level": "Debug", "durationMinutes": 15}'

# Reverter manualmente para o padrão
curl -X DELETE http://localhost:5000/api/admin/log-level
```

### Orders — Demonstração de Logging Estruturado

```bash
# Criar um pedido (gera logs Information + Debug)
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "CUST-001", "description": "Pedido de teste", "total": 199.90, "items": [{"productName": "Widget", "quantity": 2, "unitPrice": 99.95}]}'

# Buscar um pedido pelo ID
curl http://localhost:5000/api/orders/{id}
```

## Testes

```bash
dotnet test
```

## Estrutura do Projeto

```
├── src/SampleApi/
│   ├── Program.cs                            # Composição e startup
│   ├── appsettings.json                      # Configuração de logging e App Insights
│   ├── Logging/
│   │   ├── DynamicLogLevelConfigurationSource.cs    # IConfigurationSource customizado
│   │   ├── DynamicLogLevelConfigurationProvider.cs  # ConfigurationProvider com OnReload()
│   │   ├── DynamicLogLevelService.cs                # Singleton: SetLogLevel + Timer
│   │   ├── LogEnrichmentMiddleware.cs               # BeginScope com contexto da app
│   │   ├── ApplicationTelemetryInitializer.cs       # ITelemetryInitializer para App Insights
│   │   ├── LoggingOptions.cs                        # IOptions<LoggingOptions>
│   │   └── LogMessages.cs                           # [LoggerMessage] source generator
│   ├── Endpoints/
│   │   ├── LogLevelEndpoints.cs                     # POST/GET/DELETE /api/admin/log-level
│   │   └── OrderEndpoints.cs                        # Exemplo com logging rico
│   └── Models/
│       ├── SetLogLevelRequest.cs
│       └── Order.cs
├── tests/SampleApi.Tests/
│   └── Logging/
│       ├── DynamicLogLevelConfigurationProviderTests.cs
│       ├── DynamicLogLevelServiceTests.cs
│       └── LogEnrichmentMiddlewareTests.cs
├── global.json                               # .NET SDK 8.0.418 fixado
├── nuget.config
└── SampleApi.sln
```

## Configuração do Application Insights

Para conectar ao Application Insights, configure a connection string no `appsettings.json`:

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

## Licença

MIT
