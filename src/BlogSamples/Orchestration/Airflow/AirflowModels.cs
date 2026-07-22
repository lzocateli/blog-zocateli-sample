// -----------------------------------------------------------------------
// Artigo: Apache Airflow com .NET 10: dispare e monitore DAGs
// URL: https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/
// Contratos JSON usados para autenticar, disparar e consultar Dag Runs.
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace BlogSamples.Orchestration.Airflow;

public sealed record DispararDagRequest(
    [property: JsonPropertyName("dag_run_id")] string DagRunId,
    [property: JsonPropertyName("conf")] IReadOnlyDictionary<string, object?> Configuracao,
    [property: JsonPropertyName("logical_date")] DateTimeOffset? DataLogica = null);

public sealed record DagRunResponse(
    [property: JsonPropertyName("dag_run_id")] string DagRunId,
    [property: JsonPropertyName("dag_id")] string DagId,
    [property: JsonPropertyName("state")] string Estado,
    [property: JsonPropertyName("start_date")] DateTimeOffset? Inicio,
    [property: JsonPropertyName("end_date")] DateTimeOffset? Fim);

internal sealed record AirflowTokenRequest(
    [property: JsonPropertyName("username")] string Usuario,
    [property: JsonPropertyName("password")] string Senha);

internal sealed record AirflowTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken);