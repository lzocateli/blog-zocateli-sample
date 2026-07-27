using Microsoft.AspNetCore.Http.HttpResults;

namespace BlogSamples.Messaging.EventDriven;

public static class EventDrivenEndpoints
{
    public static void MapEventDrivenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/event-driven")
            .WithTags("EventDriven");

        group.MapPost("/pedidos", CreatePedido);
        group.MapGet("/pedidos/{id:guid}", GetPedido);
        group.MapGet("/outbox", GetOutbox);
    }

    private static async Task<Created<PedidoViewModel>> CreatePedido(
        CriarPedidoRequest request,
        EventDrivenOutboxStore outboxStore,
        CancellationToken ct)
    {
        var response = await outboxStore.CreatePedidoWithOutboxAsync(request, ct);
        return TypedResults.Created($"/event-driven/pedidos/{response.Id}", response);
    }

    private static async Task<IResult> GetPedido(
        Guid id,
        EventDrivenOutboxStore outboxStore,
        CancellationToken ct)
    {
        var pedido = await outboxStore.GetPedidoAsync(id, ct);
        return pedido is null
            ? Results.NotFound(new { error = "Pedido não encontrado" })
            : Results.Ok(pedido);
    }

    private static async Task<Ok<OutboxPageView>> GetOutbox(
        EventDrivenOutboxStore outboxStore,
        int limit = 50,
        string? status = null,
        string order = "desc",
        DateTimeOffset? cursorCreatedAt = null,
        Guid? cursorId = null,
        CancellationToken ct = default)
    {
        var normalizedOrder = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : "desc";

        var outbox = await outboxStore.GetOutboxMessagesAsync(
            limit,
            status,
            normalizedOrder,
            cursorCreatedAt,
            cursorId,
            ct);
        return TypedResults.Ok(outbox);
    }
}
