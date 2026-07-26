using Microsoft.AspNetCore.Http.HttpResults;

namespace BlogSamples.Messaging.EventDriven;

public static class EventDrivenEndpoints
{
    private static readonly Dictionary<Guid, PedidoViewModel> Pedidos = [];

    public static void MapEventDrivenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/event-driven")
            .WithTags("EventDriven");

        group.MapPost("/pedidos", CreatePedido);
        group.MapGet("/pedidos/{id:guid}", GetPedido);
    }

    private static Created<PedidoViewModel> CreatePedido(CriarPedidoRequest request)
    {
        var pedidoId = Guid.NewGuid();
        var state = new PedidoSagaState
        {
            PedidoId = pedidoId,
            Status = SagaStatus.Inicializado,
            Etapa = SagaEtapa.AguardandoReserva
        };

        var result = PedidoSagaOrquestrador.HandlePedidoCriado(state);
        var response = new PedidoViewModel
        {
            Id = pedidoId,
            ClienteId = request.ClienteId,
            Valor = request.Valor,
            Status = result.Status,
            Etapa = result.Etapa,
            CriadoEm = DateTimeOffset.UtcNow
        };

        Pedidos[pedidoId] = response;
        return TypedResults.Created($"/event-driven/pedidos/{pedidoId}", response);
    }

    private static IResult GetPedido(Guid id)
    {
        return Pedidos.TryGetValue(id, out var pedido)
            ? Results.Ok(pedido)
            : Results.NotFound(new { error = "Pedido não encontrado" });
    }
}
