using Microsoft.Extensions.Logging;

namespace BlogSamples.Logging;

public static partial class LogMessages
{
    // --- Dynamic Log Level ---

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Warning,
        Message = "Log level changed: Category={Category}, Level={Level}, Duration={DurationMinutes} minutes")]
    public static partial void LogLevelChanged(ILogger logger, string category, string level, int durationMinutes);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Log level reverted to default for category: {Category}")]
    public static partial void LogLevelReverted(ILogger logger, string category);

    // --- Order Processing ---

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Order {OrderId} created for customer {CustomerId}, total: {Total}")]
    public static partial void OrderCreated(ILogger logger, Guid orderId, string customerId, decimal total);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Order {OrderId} retrieved for customer {CustomerId}")]
    public static partial void OrderRetrieved(ILogger logger, Guid orderId, string customerId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Order {OrderId} not found")]
    public static partial void OrderNotFound(ILogger logger, Guid orderId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Processing order {OrderId}: validating {ItemCount} items for customer {CustomerId}")]
    public static partial void OrderProcessingDebug(ILogger logger, Guid orderId, int itemCount, string customerId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Failed to process order {OrderId} for customer {CustomerId}")]
    public static partial void OrderProcessingFailed(ILogger logger, Exception exception, Guid orderId, string customerId);

    // --- Application Lifecycle ---

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Application started: {ApplicationName} v{Version} on {HostName}")]
    public static partial void ApplicationStarted(ILogger logger, string applicationName, string version, string hostName);
}
