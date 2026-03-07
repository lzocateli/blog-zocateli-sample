using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SampleApi.Logging;

public sealed class LogEnrichmentMiddleware : IMiddleware
{
    private readonly IOptions<LoggingOptions> _options;
    private readonly IHostEnvironment _environment;

    public LogEnrichmentMiddleware(IOptions<LoggingOptions> options, IHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<LogEnrichmentMiddleware>>();

        var enrichmentData = new Dictionary<string, object>
        {
            ["ApplicationName"] = _options.Value.ApplicationName,
            ["ApplicationVersion"] = _options.Value.Version,
            ["HostName"] = Environment.MachineName,
            ["Environment"] = _environment.EnvironmentName,
            ["CorrelationId"] = context.TraceIdentifier
        };

        using (logger.BeginScope(enrichmentData))
        {
            await next(context);
        }
    }
}
