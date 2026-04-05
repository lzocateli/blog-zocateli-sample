using Microsoft.ApplicationInsights.Extensibility;
using BlogSamples.Endpoints;
using BlogSamples.Logging;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration: Dynamic Log Level (registered LAST to override appsettings) ---
((IConfigurationBuilder)builder.Configuration).Add(new DynamicLogLevelConfigurationSource());

// --- Logging Options ---
builder.Services.Configure<LoggingOptions>(
    builder.Configuration.GetSection(LoggingOptions.SectionName));

// --- Application Insights ---
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, ApplicationTelemetryInitializer>();

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BlogSamples API", Version = "v1" });
});

// --- Custom Services ---
builder.Services.AddSingleton<DynamicLogLevelService>();
builder.Services.AddSingleton<LogEnrichmentMiddleware>();

var app = builder.Build();

// --- Swagger UI em /docs ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlogSamples API v1");
    c.RoutePrefix = "docs";
});

// Redirecionar raiz para /docs
app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

// --- Middleware Pipeline ---
app.UseMiddleware<LogEnrichmentMiddleware>();

// --- Endpoints ---
app.MapLogLevelEndpoints();
app.MapOrderEndpoints();

// --- Startup Log ---
var options = builder.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
LogMessages.ApplicationStarted(startupLogger, options.ApplicationName, options.Version, Environment.MachineName);

app.Run();

public partial class Program;

