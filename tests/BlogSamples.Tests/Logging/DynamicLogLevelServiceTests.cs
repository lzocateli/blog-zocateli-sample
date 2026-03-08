using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using BlogSamples.Logging;

namespace BlogSamples.Tests.Logging;

public class DynamicLogLevelServiceTests : IDisposable
{
    private readonly DynamicLogLevelService _service;
    private readonly DynamicLogLevelConfigurationProvider _provider;

    public DynamicLogLevelServiceTests()
    {
        _provider = new DynamicLogLevelConfigurationProvider();
        _provider.Load();

        var source = Substitute.For<IConfigurationSource>();
        source.Build(Arg.Any<IConfigurationBuilder>()).Returns(_provider);

        var config = new ConfigurationBuilder()
            .Add(source)
            .Build();

        var logger = Substitute.For<ILogger<DynamicLogLevelService>>();
        _service = new DynamicLogLevelService(config, logger);
    }

    [Fact]
    public void SetLogLevel_SetsOverrideAndUpdatesProvider()
    {
        // Act
        _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.FromMinutes(15));

        // Assert
        Assert.NotNull(_service.CurrentOverride);
        Assert.Equal(LogLevel.Debug, _service.CurrentOverride.Level);
        Assert.Equal("Default", _service.CurrentOverride.Category);
        Assert.True(_provider.TryGet("Logging:LogLevel:Default", out var value));
        Assert.Equal("Debug", value);
    }

    [Fact]
    public void SetLogLevel_WithCategory_SetsCorrectKey()
    {
        // Act
        _service.SetLogLevel("Microsoft.AspNetCore", LogLevel.Trace, TimeSpan.FromMinutes(5));

        // Assert
        Assert.NotNull(_service.CurrentOverride);
        Assert.Equal("Microsoft.AspNetCore", _service.CurrentOverride.Category);
        Assert.True(_provider.TryGet("Logging:LogLevel:Microsoft.AspNetCore", out var value));
        Assert.Equal("Trace", value);
    }

    [Fact]
    public void SetLogLevel_ThrowsForZeroDuration()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.Zero));
    }

    [Fact]
    public void SetLogLevel_ThrowsForNegativeDuration()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void RevertToDefault_ClearsOverrideAndProvider()
    {
        // Arrange
        _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.FromMinutes(15));

        // Act
        _service.RevertToDefault();

        // Assert
        Assert.Null(_service.CurrentOverride);
        Assert.False(_provider.TryGet("Logging:LogLevel:Default", out _));
    }

    [Fact]
    public void SetLogLevel_SecondCall_ReplacesFirst()
    {
        // Arrange
        _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.FromMinutes(15));

        // Act
        _service.SetLogLevel(null, LogLevel.Trace, TimeSpan.FromMinutes(30));

        // Assert
        Assert.NotNull(_service.CurrentOverride);
        Assert.Equal(LogLevel.Trace, _service.CurrentOverride.Level);
        Assert.True(_provider.TryGet("Logging:LogLevel:Default", out var value));
        Assert.Equal("Trace", value);
    }

    [Fact]
    public async Task SetLogLevel_TimerRevertsAutomatically()
    {
        // Arrange
        _service.SetLogLevel(null, LogLevel.Debug, TimeSpan.FromMilliseconds(100));

        // Act — wait for timer to fire
        await Task.Delay(500);

        // Assert
        Assert.Null(_service.CurrentOverride);
        Assert.False(_provider.TryGet("Logging:LogLevel:Default", out _));
    }

    [Fact]
    public void CurrentOverride_IsNullByDefault()
    {
        // Assert
        Assert.Null(_service.CurrentOverride);
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}
