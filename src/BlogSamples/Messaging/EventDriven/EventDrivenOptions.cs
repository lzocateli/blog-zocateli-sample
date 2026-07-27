namespace BlogSamples.Messaging.EventDriven;

public sealed class EventDrivenOptions
{
    public const string SectionName = "EventDriven";

    public bool Enabled { get; set; } = true;
    public string TopicName { get; set; } = "saga-pedidos";
    public string SubscriptionName { get; set; } = "orquestrador";

    public string PostgresConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=blogsamples;Username=blogsamples;Password=blogsamples";

    public string ServiceBusConnectionString { get; set; } =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public int PublisherBatchSize { get; set; } = 20;
    public int PublisherIntervalMs { get; set; } = 1500;
    public int LockTimeoutSeconds { get; set; } = 60;
    public int MaxPublishAttempts { get; set; } = 10;
}