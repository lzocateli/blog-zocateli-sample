using System.Net;
using System.Text;
using BlogSamples.Orchestration.Airflow;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlogSamples.Tests.Orchestration.Airflow;

public sealed class AirflowClientTests
{
    [Fact]
    public async Task DispararEMonitorarAsync_DeveRetornarQuandoDagFinalizarComSucesso()
    {
        var respostas = new Queue<HttpResponseMessage>(
        [
            Json(HttpStatusCode.OK, "{\"access_token\":\"token-sem-expiracao\"}"),
            Json(HttpStatusCode.OK, "{\"dag_run_id\":\"run-123\",\"dag_id\":\"etl_vendas\",\"state\":\"queued\"}"),
            Json(HttpStatusCode.OK, "{\"dag_run_id\":\"run-123\",\"dag_id\":\"etl_vendas\",\"state\":\"running\"}"),
            Json(HttpStatusCode.OK, "{\"dag_run_id\":\"run-123\",\"dag_id\":\"etl_vendas\",\"state\":\"success\"}")
        ]);
        using var handler = new FilaHttpMessageHandler(respostas);
        using var client = CriarClient(handler);

        var resultado = await client.DispararEMonitorarAsync(
            "etl_vendas",
            new Dictionary<string, object> { ["data_referencia"] = "2026-07-21" });

        Assert.Equal("success", resultado.Estado);
        Assert.Equal(4, handler.Requisicoes.Count);
        Assert.Equal("/auth/token", handler.Requisicoes[0].RequestUri!.AbsolutePath);
        Assert.Equal("/api/v2/dags/etl_vendas/dagRuns", handler.Requisicoes[1].RequestUri!.AbsolutePath);
        Assert.Contains("\"data_referencia\":\"2026-07-21\"", handler.Corpos[1], StringComparison.Ordinal);
        Assert.Contains("\"logical_date\":null", handler.Corpos[1], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ObterExecucaoAsync_DeveRenovarTokenAposTokenInvalido(HttpStatusCode statusCode)
    {
        var respostas = new Queue<HttpResponseMessage>(
        [
            Json(HttpStatusCode.OK, "{\"access_token\":\"token-antigo\"}"),
            Json(statusCode, "{\"detail\":\"token inválido\"}"),
            Json(HttpStatusCode.OK, "{\"access_token\":\"token-novo\"}"),
            Json(HttpStatusCode.OK, "{\"dag_run_id\":\"run-456\",\"dag_id\":\"etl_vendas\",\"state\":\"running\"}")
        ]);
        using var handler = new FilaHttpMessageHandler(respostas);
        using var client = CriarClient(handler);

        var resultado = await client.ObterExecucaoAsync("etl_vendas", "run-456");

        Assert.Equal("running", resultado.Estado);
        Assert.Equal("Bearer", handler.Requisicoes[3].Headers.Authorization!.Scheme);
        Assert.Equal("token-novo", handler.Requisicoes[3].Headers.Authorization!.Parameter);
    }

    private static AirflowClient CriarClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://airflow.local/")
        };
        var options = Options.Create(new AirflowOptions
        {
            BaseUrl = httpClient.BaseAddress,
            Usuario = "dotnet-service",
            Senha = "segredo",
            IntervaloMonitoramentoMs = 1,
            TempoLimiteSegundos = 5
        });

        return new AirflowClient(httpClient, options, NullLogger<AirflowClient>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class FilaHttpMessageHandler(Queue<HttpResponseMessage> respostas) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requisicoes { get; } = [];

        public List<string> Corpos { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var copia = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                copia.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Requisicoes.Add(copia);
            Corpos.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return respostas.Dequeue();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var resposta in respostas)
                {
                    resposta.Dispose();
                }

                foreach (var requisicao in Requisicoes)
                {
                    requisicao.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}