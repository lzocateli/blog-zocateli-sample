namespace BlogSamples.Logging;

public sealed class LoggingOptions
{
    public const string SectionName = "Logging:Application";

    public string ApplicationName { get; set; } = "BlogSamples";
    public string Version { get; set; } = "1.0.0";
}
