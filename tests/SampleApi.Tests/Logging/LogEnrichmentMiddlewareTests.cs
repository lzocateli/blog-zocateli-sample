using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SampleApi.Logging;

namespace SampleApi.Tests.Logging;

public class LogEnrichmentMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsExpectedScopeProperties()
    {
        // Arrange
        var options = Options.Create(new LoggingOptions
        {
            ApplicationName = "TestApp",
            Version = "2.0.0"
        });

        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Testing");

        var middleware = new LogEnrichmentMiddleware(options, environment);

        Dictionary<string, object> capturedScope = null;
        var logger = Substitute.For<ILogger<LogEnrichmentMiddleware>>();
        logger.BeginScope(Arg.Do<Dictionary<string, object>>(s => capturedScope = s))
            .Returns(Substitute.For<IDisposable>());

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILogger<LogEnrichmentMiddleware>)).Returns(logger);

        var context = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = "test-trace-123"
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.NotNull(capturedScope);
        Assert.Equal("TestApp", capturedScope["ApplicationName"]);
        Assert.Equal("2.0.0", capturedScope["ApplicationVersion"]);
        Assert.Equal(Environment.MachineName, capturedScope["HostName"]);
        Assert.Equal("Testing", capturedScope["Environment"]);
        Assert.Equal("test-trace-123", capturedScope["CorrelationId"]);
    }
}
