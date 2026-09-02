// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Minimal API comum aos perfis JIT FDD, JIT SCD, R2R e Native AOT.
// -----------------------------------------------------------------------

using BlogSamples.NativeAot.DecisionLab.Api;
using BlogSamples.NativeAot.DecisionLab.Workload;
using Microsoft.AspNetCore.Http.Json;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, DecisionLabJsonContext.Default));

WebApplication app = builder.Build();

app.MapGet("/ready", () => TypedResults.Ok(new ReadyResponse("ready")));
app.MapPost("/work", (WorkloadRequest request) => TypedResults.Ok(WorkloadProcessor.Process(request)));

app.Start();
Console.WriteLine("Application started.");
app.WaitForShutdown();

namespace BlogSamples.NativeAot.DecisionLab.Api
{
    public sealed record ReadyResponse(string Status);
}