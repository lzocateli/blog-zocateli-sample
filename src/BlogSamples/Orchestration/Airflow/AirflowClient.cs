// -----------------------------------------------------------------------
// Artigo: Apache Airflow com .NET 10: dispare e monitore DAGs
// URL: https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/
// Cliente resiliente para a API pública v2 do Apache Airflow 3.
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BlogSamples.Orchestration.Airflow;

public interface IAirflowClient
{
    Task<DagRunResponse> DispararAsync(
        string dagId,
        IReadOnlyDictionary<string, object?>? configuracao = null,
        CancellationToken cancellationToken = default);

    Task<DagRunResponse> ObterExecucaoAsync(
        string dagId,
        string dagRunId,
        CancellationToken cancellationToken = default);

    Task<DagRunResponse> DispararEMonitorarAsync(
        string dagId,
        IReadOnlyDictionary<string, object?>? configuracao = null,
        CancellationToken cancellationToken = default);
}

public sealed class AirflowClient(
    HttpClient httpClient,
    IOptions<AirflowOptions> options,
    ILogger<AirflowClient> logger) : IAirflowClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> EstadosFinais = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "failed",
        "canceled"
    };

    private readonly AirflowOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpiraEm;

    public async Task<DagRunResponse> DispararAsync(
        string dagId,
        IReadOnlyDictionary<string, object?>? configuracao = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dagId);

        var dagRunId = $"dotnet__{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfff}__{Guid.NewGuid():N}";
        var payload = new DispararDagRequest(
            dagRunId,
            configuracao ?? new Dictionary<string, object?>());

        var caminho = $"api/v2/dags/{Uri.EscapeDataString(dagId)}/dagRuns";
        var execucao = await EnviarAutenticadoAsync<DagRunResponse>(
            () => new HttpRequestMessage(HttpMethod.Post, caminho)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            },
            cancellationToken);

        logger.LogInformation(
            "Dag {DagId} disparada com Dag Run {DagRunId} no estado {Estado}",
            dagId,
            execucao.DagRunId,
            execucao.Estado);

        return execucao;
    }

    public Task<DagRunResponse> ObterExecucaoAsync(
        string dagId,
        string dagRunId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dagId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dagRunId);

        var caminho = $"api/v2/dags/{Uri.EscapeDataString(dagId)}/dagRuns/{Uri.EscapeDataString(dagRunId)}";
        return EnviarAutenticadoAsync<DagRunResponse>(
            () => new HttpRequestMessage(HttpMethod.Get, caminho),
            cancellationToken);
    }

    public async Task<DagRunResponse> DispararEMonitorarAsync(
        string dagId,
        IReadOnlyDictionary<string, object?>? configuracao = null,
        CancellationToken cancellationToken = default)
    {
        var execucao = await DispararAsync(dagId, configuracao, cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TempoLimiteSegundos));

        while (!EstadosFinais.Contains(execucao.Estado))
        {
            await Task.Delay(_options.IntervaloMonitoramentoMs, timeout.Token);
            execucao = await ObterExecucaoAsync(dagId, execucao.DagRunId, timeout.Token);

            logger.LogInformation(
                "Dag Run {DagRunId} consultada no estado {Estado}",
                execucao.DagRunId,
                execucao.Estado);
        }

        return execucao;
    }

    private async Task<T> EnviarAutenticadoAsync<T>(
        Func<HttpRequestMessage> criarRequisicao,
        CancellationToken cancellationToken)
    {
        for (var tentativa = 0; tentativa < 2; tentativa++)
        {
            var token = await ObterTokenAsync(cancellationToken);
            using var requisicao = criarRequisicao();
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var resposta = await httpClient.SendAsync(requisicao, cancellationToken);

            if (tentativa == 0 && resposta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                InvalidarToken();
                continue;
            }

            return await DesserializarRespostaAsync<T>(resposta, cancellationToken);
        }

        throw new InvalidOperationException("Não foi possível autenticar na API do Airflow.");
    }

    private async Task<string> ObterTokenAsync(CancellationToken cancellationToken)
    {
        if (TokenValido())
        {
            return _token!;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (TokenValido())
            {
                return _token!;
            }

            using var resposta = await httpClient.PostAsJsonAsync(
                "auth/token",
                new AirflowTokenRequest(_options.Usuario, _options.Senha),
                JsonOptions,
                cancellationToken);
            var token = await DesserializarRespostaAsync<AirflowTokenResponse>(resposta, cancellationToken);

            _token = token.AccessToken;
            _tokenExpiraEm = LerExpiracaoJwt(token.AccessToken) ?? DateTimeOffset.UtcNow.AddMinutes(5);
            return _token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool TokenValido() =>
        _token is not null && _tokenExpiraEm > DateTimeOffset.UtcNow.AddMinutes(1);

    private void InvalidarToken()
    {
        _token = null;
        _tokenExpiraEm = DateTimeOffset.MinValue;
    }

    private static async Task<T> DesserializarRespostaAsync<T>(
        HttpResponseMessage resposta,
        CancellationToken cancellationToken)
    {
        if (!resposta.IsSuccessStatusCode)
        {
            var detalhe = await resposta.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Airflow retornou {(int)resposta.StatusCode} ({resposta.ReasonPhrase}): {detalhe}",
                null,
                resposta.StatusCode);
        }

        return await resposta.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new JsonException("A API do Airflow retornou uma resposta vazia.");
    }

    private static DateTimeOffset? LerExpiracaoJwt(string token)
    {
        var partes = token.Split('.');
        if (partes.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            using var documento = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            return documento.RootElement.TryGetProperty("exp", out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64())
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _tokenLock.Dispose();
}