# Apache Airflow 3 com .NET 10

Ambiente local do artigo [Apache Airflow com .NET 10: dispare e monitore DAGs](https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/).

## Persistência

Todos os dados ficam em bind mounts relativos a esta pasta:

| Host | Container | Conteúdo |
| ---- | --------- | -------- |
| `dags/` | `/opt/airflow/dags` | DAGs versionados |
| `logs/` | `/opt/airflow/logs` | Logs de scheduler e tarefas |
| `config/` | `/opt/airflow/config` | Configuração gerada |
| `plugins/` | `/opt/airflow/plugins` | Plugins locais |
| `data/postgres/` | `/var/lib/postgresql/data` | Metadados, usuários e histórico |

`docker compose down` remove os containers e a rede, mas preserva esses diretórios.

## Execução

```powershell
Set-Location src/BlogSamples/Orchestration/Airflow

# Opcional: personalize portas e credenciais fora do Git.
Copy-Item .env.example .env

# Migra o banco e cria o usuário local.
docker compose up airflow-init

# Inicia e aguarda todos os healthchecks.
docker compose up -d --wait
```

A interface e a API pública ficam em <http://localhost:8081>. As credenciais padrão de desenvolvimento são `dotnet-service` e `airflow`.

Execute a aplicação .NET em outro terminal:

```powershell
$env:Airflow__Senha = "airflow"
dotnet run --project src/BlogSamples
```

Dispare o DAG de demonstração:

```powershell
$body = @{
    configuracao = @{
        data_referencia = "2026-07-21"
        correlation_id = "teste-local-001"
    }
    aguardarConclusao = $true
} | ConvertTo-Json -Depth 3

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:5101/api/airflow/dags/etl_vendas/runs" `
    -ContentType "application/json" `
    -Body $body
```

## Limpeza

```powershell
# Recria containers sem perder DAGs, logs, configuração ou banco.
docker compose down

# Remove também todo o estado persistido do exemplo.
docker compose down
Remove-Item -Recurse -Force data/postgres, logs/*, config/*
```

Esta stack usa `LocalExecutor` e credenciais de desenvolvimento. Ela não é uma topologia de produção.
