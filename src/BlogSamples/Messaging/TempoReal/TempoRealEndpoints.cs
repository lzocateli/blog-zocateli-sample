using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.Messaging.TempoReal;

public static class TempoRealEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapTempoRealEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/tempo-real")
            .WithTags("Tempo real")
            .RequireCors("TempoReal");

        grupo.MapPost("/tarefas", (TempoRealProcessamentoService processamentoService, TempoRealCriacaoRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Descricao))
            {
                return Results.BadRequest(new { erro = "A descrição da tarefa é obrigatória." });
            }

            var resposta = processamentoService.CriarTarefa(request.Descricao.Trim());
            return Results.Created($"/api/tempo-real/tarefas/{resposta.TarefaId}", resposta);
        });

        grupo.MapGet("/tarefas/{tarefaId:guid}", (Guid tarefaId, TempoRealProcessamentoService processamentoService) =>
        {
            var estado = processamentoService.ObterEstado(tarefaId);
            return estado is null ? Results.NotFound() : Results.Ok(estado);
        });

        grupo.MapGet("/tarefas/{tarefaId:guid}/long-poll", async (
            Guid tarefaId,
            [FromQuery(Name = "versao")] int versaoAtual,
            [FromQuery(Name = "timeoutMs")] int timeoutMs,
            TempoRealProcessamentoService processamentoService,
            CancellationToken cancellationToken) =>
        {
            var estado = await processamentoService.AguardarAtualizacaoAsync(
                tarefaId,
                versaoAtual,
                TimeSpan.FromMilliseconds(Math.Clamp(timeoutMs <= 0 ? 15000 : timeoutMs, 1000, 30000)),
                cancellationToken);

            return estado is null ? Results.NotFound() : Results.Ok(estado);
        });

        grupo.MapGet("/tarefas/{tarefaId:guid}/stream", async (
            Guid tarefaId,
            HttpContext context,
            TempoRealProcessamentoService processamentoService,
            CancellationToken cancellationToken) =>
        {
            var estado = processamentoService.ObterEstado(tarefaId);
            if (estado is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            context.Response.ContentType = "text/event-stream";

            await EscreverSseAsync(context.Response, "estado", estado, cancellationToken);

            await foreach (var proximoEstado in processamentoService.ObterFluxoAsync(tarefaId, cancellationToken))
            {
                await EscreverSseAsync(context.Response, "estado", proximoEstado, cancellationToken);
            }
        });

        grupo.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var tarefaIdTexto = context.Request.Query["tarefaId"].ToString();
            if (!Guid.TryParse(tarefaIdTexto, out var tarefaId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Informe o parâmetro tarefaId na query string.");
                return;
            }

            var processamentoService = context.RequestServices.GetRequiredService<TempoRealProcessamentoService>();
            if (processamentoService.ObterEstado(tarefaId) is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();

            await foreach (var estadoAtual in processamentoService.ObterFluxoAsync(tarefaId, context.RequestAborted))
            {
                var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(estadoAtual, JsonOptions));
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, context.RequestAborted);

                if (socket.State is WebSocketState.CloseReceived or WebSocketState.CloseSent)
                {
                    break;
                }
            }

            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fluxo concluído", context.RequestAborted);
            }
        });

        return app;
    }

    private static async Task EscreverSseAsync(
        HttpResponse response,
        string evento,
        TempoRealEstadoResponse estado,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(estado, JsonOptions);
        await response.WriteAsync($"event: {evento}\n", cancellationToken);
        await response.WriteAsync($"data: {payload}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}