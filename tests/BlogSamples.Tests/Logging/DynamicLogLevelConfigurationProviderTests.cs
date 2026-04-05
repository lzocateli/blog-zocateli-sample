using BlogSamples.Logging;

namespace BlogSamples.Tests.Logging;

public class DynamicLogLevelConfigurationProviderTests
{
    [Fact]
    public void Update_SetsValueAndTriggersReload()
    {
        // Arrange
        var provider = new DynamicLogLevelConfigurationProvider();
        provider.Load();
        var reloadTriggered = false;

        var token = provider.GetReloadToken();
        token.RegisterChangeCallback(_ => reloadTriggered = true, null);

        // Act
        provider.Update("Logging:LogLevel:Default", "Debug");

        // Assert
        Assert.True(provider.TryGet("Logging:LogLevel:Default", out var value));
        Assert.Equal("Debug", value);
        Assert.True(reloadTriggered);
    }

    [Fact]
    public void Update_WithNullValue_RemovesKey()
    {
        // Arrange
        var provider = new DynamicLogLevelConfigurationProvider();
        provider.Load();
        provider.Update("Logging:LogLevel:Default", "Debug");

        // Act
        provider.Update("Logging:LogLevel:Default", null);

        // Assert
        Assert.False(provider.TryGet("Logging:LogLevel:Default", out _));
    }

    [Fact]
    public void Clear_RemovesAllKeysAndTriggersReload()
    {
        // Arrange
        var provider = new DynamicLogLevelConfigurationProvider();
        provider.Load();
        provider.Update("Logging:LogLevel:Default", "Debug");
        provider.Update("Logging:LogLevel:Microsoft", "Trace");

        var reloadTriggered = false;
        var token = provider.GetReloadToken();
        token.RegisterChangeCallback(_ => reloadTriggered = true, null);

        // Act
        provider.Clear();

        // Assert
        Assert.False(provider.TryGet("Logging:LogLevel:Default", out _));
        Assert.False(provider.TryGet("Logging:LogLevel:Microsoft", out _));
        Assert.True(reloadTriggered);
    }

    [Fact]
    public void Load_StartsEmpty()
    {
        // Arrange & Act
        var provider = new DynamicLogLevelConfigurationProvider();
        provider.Load();

        // Assert
        Assert.False(provider.TryGet("Logging:LogLevel:Default", out _));
    }
}
