namespace SampleApi.Logging;

public sealed class LoggingOptions
{
    public const string SectionName = "Logging:Application";

    public string ApplicationName { get; set; } = "SampleApi";
    public string Version { get; set; } = "1.0.0";
}
