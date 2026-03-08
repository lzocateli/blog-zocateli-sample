namespace BlogSamples.Models;

public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CustomerId { get; init; } = default!;
    public string Description { get; init; } = default!;
    public decimal Total { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; init; } = [];
}

public sealed class OrderItem
{
    public string ProductName { get; init; } = default!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
