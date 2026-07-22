// -----------------------------------------------------------------------
// Artigo: Apache Airflow com .NET 10: dispare e monitore DAGs
// URL: https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/
// Registro do cliente tipado e dos endpoints de demonstração.
// -----------------------------------------------------------------------

namespace BlogSamples.Orchestration.Airflow;

public static class AirflowSetup
{
    public static IServiceCollection AddAirflowClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AirflowOptions>()
            .Bind(configuration.GetSection(AirflowOptions.SectionName))
            .Validate(options => options.BaseUrl.IsAbsoluteUri, "Airflow:BaseUrl deve ser uma URL absoluta.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Usuario), "Airflow:Usuario é obrigatório.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Senha), "Airflow:Senha é obrigatória.")
            .Validate(options => options.IntervaloMonitoramentoMs > 0, "O intervalo deve ser positivo.")
            .Validate(options => options.TempoLimiteSegundos > 0, "O tempo limite deve ser positivo.")
            .ValidateOnStart();

        services.AddHttpClient<IAirflowClient, AirflowClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AirflowOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static void MapAirflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/airflow/dags")
            .WithTags("Apache Airflow");

        group.MapPost("/{dagId}/runs", async (
            string dagId,
            DispararAirflowApiRequest request,
            IAirflowClient client,
            CancellationToken cancellationToken) =>
        {
            var execucao = request.AguardarConclusao
                ? await client.DispararEMonitorarAsync(dagId, request.Configuracao, cancellationToken)
                : await client.DispararAsync(dagId, request.Configuracao, cancellationToken);

            return Results.Ok(execucao);
        });

        group.MapGet("/{dagId}/runs/{dagRunId}", (
            string dagId,
            string dagRunId,
            IAirflowClient client,
            CancellationToken cancellationToken) =>
            client.ObterExecucaoAsync(dagId, dagRunId, cancellationToken));
    }
}

public sealed record DispararAirflowApiRequest(
    IReadOnlyDictionary<string, object?> Configuracao,
    bool AguardarConclusao = true);