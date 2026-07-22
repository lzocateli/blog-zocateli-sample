// -----------------------------------------------------------------------
// Artigo: Apache Airflow com .NET 10: dispare e monitore DAGs
// URL: https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/
// Configuração do cliente da API pública do Apache Airflow 3.
// -----------------------------------------------------------------------

namespace BlogSamples.Orchestration.Airflow;

public sealed class AirflowOptions
{
    public const string SectionName = "Airflow";

    public required Uri BaseUrl { get; init; }

    public required string Usuario { get; init; }

    public required string Senha { get; init; }

    public int IntervaloMonitoramentoMs { get; init; } = 5_000;

    public int TempoLimiteSegundos { get; init; } = 600;
}