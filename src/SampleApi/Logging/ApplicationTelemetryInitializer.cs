using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace SampleApi.Logging;

public sealed class ApplicationTelemetryInitializer : ITelemetryInitializer
{
    private readonly string _applicationName;
    private readonly string _version;
    private readonly string _hostName;
    private readonly string _environment;

    public ApplicationTelemetryInitializer(IOptions<LoggingOptions> options, IHostEnvironment env)
    {
        _applicationName = options.Value.ApplicationName;
        _version = options.Value.Version;
        _hostName = Environment.MachineName;
        _environment = env.EnvironmentName;
    }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties.TryAdd("ApplicationName", _applicationName);
        telemetry.Context.GlobalProperties.TryAdd("ApplicationVersion", _version);
        telemetry.Context.GlobalProperties.TryAdd("HostName", _hostName);
        telemetry.Context.GlobalProperties.TryAdd("Environment", _environment);
        telemetry.Context.Component.Version = _version;
    }
}
