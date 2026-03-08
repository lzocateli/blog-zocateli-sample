using BlogSamples.Logging;
using BlogSamples.Models;

namespace BlogSamples.Endpoints;

public static class OrderEndpoints
{
    private static readonly List<Order> Orders = [];

    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", CreateOrder);
        group.MapGet("/{id:guid}", GetOrderById);
    }

    private static IResult CreateOrder(
        Order order,
        ILogger<DynamicLogLevelService> logger)
    {
        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = order.CustomerId,
            Description = order.Description,
            Total = order.Total,
            Items = order.Items
        };

        LogMessages.OrderProcessingDebug(logger, newOrder.Id, newOrder.Items.Count, newOrder.CustomerId);

        Orders.Add(newOrder);

        LogMessages.OrderCreated(logger, newOrder.Id, newOrder.CustomerId, newOrder.Total);

        return Results.Created($"/api/orders/{newOrder.Id}", newOrder);
    }

    private static IResult GetOrderById(
        Guid id,
        ILogger<DynamicLogLevelService> logger)
    {
        var order = Orders.Find(o => o.Id == id);

        if (order is null)
        {
            LogMessages.OrderNotFound(logger, id);
            return Results.NotFound(new { Error = $"Order {id} not found." });
        }

        LogMessages.OrderRetrieved(logger, order.Id, order.CustomerId);
        return Results.Ok(order);
    }
}
