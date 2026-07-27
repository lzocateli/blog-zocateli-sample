using Microsoft.ApplicationInsights.Extensibility;
using BlogSamples.Endpoints;
using BlogSamples.Logging;
using BlogSamples.Messaging.EventDriven;
using BlogSamples.Messaging.TempoReal;
using BlogSamples.Orchestration.Airflow;
using BlogSamples.Produtos;
using BlogSamples.Security.Cors;
using Microsoft.Extensions.Options;
using Npgsql;
using Azure.Messaging.ServiceBus;

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

// --- Produtos (Artigo 15 — Blazor WASM + Radzen) ---
builder.Services.AddSingleton<IProdutoService, ProdutoService>();

// --- CORS (policies explícitas por cenário) ---
builder.Services.AddCorsSeguranca();

builder.Services.AddCors(options =>
{
    options.AddPolicy("TempoReal", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://localhost:4201", "http://localhost:4202")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<TempoRealProcessamentoService>();
builder.Services.AddSignalR();

// --- EventDriven com Outbox transacional (PostgreSQL + publisher separado) ---
builder.Services.Configure<EventDrivenOptions>(
    builder.Configuration.GetSection(EventDrivenOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<EventDrivenOptions>>().Value;
    return NpgsqlDataSource.Create(options.PostgresConnectionString);
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<EventDrivenOptions>>().Value;
    return new ServiceBusClient(options.ServiceBusConnectionString);
});
builder.Services.AddSingleton<EventDrivenOutboxStore>();
builder.Services.AddSingleton<IEventDrivenBrokerPublisher, ServiceBusEventDrivenPublisher>();
builder.Services.AddHostedService<EventDrivenSchemaInitializer>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

// --- Apache Airflow 3 API v2 ---
builder.Services.AddAirflowClient(builder.Configuration);

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

// --- CORS ---
app.UseCors();
app.UseWebSockets();

// --- Middleware Pipeline ---
app.UseMiddleware<LogEnrichmentMiddleware>();

// --- Endpoints ---
app.MapLogLevelEndpoints();
app.MapOrderEndpoints();
app.MapCorsEndpoints();
app.MapProdutoEndpoints();
app.MapTempoRealEndpoints();
app.MapAirflowEndpoints();
app.MapEventDrivenEndpoints();
app.MapHub<TempoRealHub>("/hubs/tempo-real").RequireCors("TempoReal");

// --- Startup Log ---
var options = builder.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
LogMessages.ApplicationStarted(startupLogger, options.ApplicationName, options.Version, Environment.MachineName);

app.Run();

public partial class Program;

