using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogSamples.Logging;

public sealed class DynamicLogLevelService : IDisposable
{
    private readonly DynamicLogLevelConfigurationProvider _provider;
    private readonly ILogger<DynamicLogLevelService> _logger;
    private readonly object _lock = new();
    private Timer? _revertTimer;
    private LogLevelOverride? _currentOverride;

    public DynamicLogLevelService(IConfiguration configuration, ILogger<DynamicLogLevelService> logger)
    {
        _logger = logger;

        var root = configuration as IConfigurationRoot
            ?? throw new InvalidOperationException(
                "IConfiguration must be an IConfigurationRoot. Ensure DynamicLogLevelConfigurationSource is registered.");

        _provider = root.Providers
            .OfType<DynamicLogLevelConfigurationProvider>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "DynamicLogLevelConfigurationProvider not found. Ensure DynamicLogLevelConfigurationSource is added to the configuration builder.");
    }

    public LogLevelOverride? CurrentOverride
    {
        get
        {
            lock (_lock)
            {
                return _currentOverride;
            }
        }
    }

    public void SetLogLevel(string? category, LogLevel level, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero, nameof(duration));

        var key = $"Logging:LogLevel:{category ?? "Default"}";

        lock (_lock)
        {
            _revertTimer?.Dispose();

            _provider.Update(key, level.ToString());

            _currentOverride = new LogLevelOverride(
                Category: category ?? "Default",
                Level: level,
                ExpiresAt: DateTimeOffset.UtcNow.Add(duration));

            _revertTimer = new Timer(
                callback: _ => RevertToDefault(),
                state: null,
                dueTime: duration,
                period: Timeout.InfiniteTimeSpan);

            LogMessages.LogLevelChanged(_logger, category ?? "Default", level.ToString(), (int)duration.TotalMinutes);
        }
    }

    public void RevertToDefault()
    {
        lock (_lock)
        {
            _revertTimer?.Dispose();
            _revertTimer = null;

            _provider.Clear();

            var previous = _currentOverride;
            _currentOverride = null;

            if (previous is not null)
            {
                LogMessages.LogLevelReverted(_logger, previous.Category ?? "Default");
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _revertTimer?.Dispose();
            _revertTimer = null;
        }
    }
}

public sealed record LogLevelOverride(
    string Category,
    LogLevel Level,
    DateTimeOffset ExpiresAt);
