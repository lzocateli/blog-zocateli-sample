// -----------------------------------------------------------------------
// Artigo: CORS não é detalhe: por que times ainda erram esse básico
// URL: https://zocate.li/posts/2026/cors-seguranca-nginx-aspnet-core/
// Políticas CORS explícitas para SPA autenticada, leitura pública e
// validação dinâmica segura por allowlist
// -----------------------------------------------------------------------

namespace BlogSamples.Security.Cors;

public static class CorsPolicyConfiguration
{
    private static readonly HashSet<string> OrigensConfiaveis =
    [
        "https://app.zocate.li",
        "https://admin.zocate.li"
    ];

    public const string PoliticaBlazorWasm = "BlazorWasm";
    public const string PoliticaFrontendSpa = "FrontendSpa";
    public const string PoliticaLeituraPublica = "LeituraPublica";

    public static IServiceCollection AddCorsSeguranca(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PoliticaBlazorWasm, policy =>
            {
                policy.WithOrigins("http://localhost:5200", "https://localhost:7200")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });

            options.AddPolicy(PoliticaFrontendSpa, policy =>
            {
                policy.SetIsOriginAllowed(OriginEstaNaAllowlist)
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .WithHeaders("Authorization", "Content-Type")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            options.AddPolicy(PoliticaLeituraPublica, policy =>
            {
                policy.AllowAnyOrigin()
                      .WithMethods("GET")
                      .WithHeaders("Content-Type");
            });
        });

        return services;
    }

    public static IReadOnlyCollection<string> ObterOrigensConfiaveis() => OrigensConfiaveis;

    private static bool OriginEstaNaAllowlist(string origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
               OrigensConfiaveis.Contains(origin);
    }
}