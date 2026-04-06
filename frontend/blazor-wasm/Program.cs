using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlogSamples.BlazorWasm;
using BlogSamples.BlazorWasm.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configurar HttpClient com URL base da API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5101";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Services HTTP tipados
builder.Services.AddScoped<ProdutoApiService>();
builder.Services.AddScoped<CategoriaApiService>();

// Radzen Components (DialogService, NotificationService, etc.)
builder.Services.AddRadzenComponents();

await builder.Build().RunAsync();
