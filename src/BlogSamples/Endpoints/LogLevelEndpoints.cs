using Microsoft.Extensions.Logging;
using BlogSamples.Logging;
using BlogSamples.Models;

namespace BlogSamples.Endpoints;

public static class LogLevelEndpoints
{
    public static void MapLogLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/log-level")
            .WithTags("Admin");

        group.MapGet("/", GetCurrentLogLevel);
        group.MapPost("/", SetLogLevel);
        group.MapDelete("/", RevertLogLevel);
    }

    private static IResult GetCurrentLogLevel(DynamicLogLevelService service)
    {
        var current = service.CurrentOverride;

        if (current is null)
        {
            return Results.Ok(new
            {
                Status = "default",
                Message = "No log level override active. Using appsettings configuration."
            });
        }

        return Results.Ok(new
        {
            Status = "overridden",
            current.Category,
            Level = current.Level.ToString(),
            current.ExpiresAt,
            RemainingMinutes = Math.Max(0, (int)(current.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes)
        });
    }

    private static IResult SetLogLevel(
        SetLogLevelRequest request,
        DynamicLogLevelService service,
        ILogger<DynamicLogLevelService> logger)
    {
        if (!Enum.TryParse<LogLevel>(request.Level, ignoreCase: true, out var logLevel))
        {
            return Results.BadRequest(new
            {
                Error = $"Invalid log level: '{request.Level}'.",
                ValidLevels = Enum.GetNames<LogLevel>()
            });
        }

        if (logLevel == LogLevel.None)
        {
            return Results.BadRequest(new
            {
                Error = "Cannot set log level to 'None'. Use DELETE to revert to default."
            });
        }

        var duration = TimeSpan.FromMinutes(request.DurationMinutes);
        service.SetLogLevel(request.Category, logLevel, duration);

        return Results.Ok(new
        {
            Message = $"Log level set to '{logLevel}' for {request.DurationMinutes} minutes.",
            Category = request.Category ?? "Default",
            Level = logLevel.ToString(),
            DurationMinutes = request.DurationMinutes,
            ExpiresAt = DateTimeOffset.UtcNow.Add(duration)
        });
    }

    private static IResult RevertLogLevel(DynamicLogLevelService service)
    {
        service.RevertToDefault();

        return Results.Ok(new
        {
            Message = "Log level reverted to default configuration."
        });
    }
}
