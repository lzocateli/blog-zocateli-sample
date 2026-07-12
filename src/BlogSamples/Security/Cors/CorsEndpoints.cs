// -----------------------------------------------------------------------
// Artigo: CORS não é detalhe: por que times ainda erram esse básico
// URL: https://zocate.li/posts/2026/cors-seguranca-nginx-aspnet-core/
// Endpoints mínimos para demonstrar allowlist, credentials e preflight
// -----------------------------------------------------------------------

namespace BlogSamples.Security.Cors;

public static class CorsEndpoints
{
    public static void MapCorsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/cors")
            .WithTags("Security")
            .RequireCors(CorsPolicyConfiguration.PoliticaFrontendSpa);

        app.MapGet("/api/security/cors/publico", () =>
            Results.Ok(new
            {
                mensagem = "Leitura pública liberada apenas para GET.",
                politica = CorsPolicyConfiguration.PoliticaLeituraPublica
            }))
            .WithTags("Security")
            .RequireCors(CorsPolicyConfiguration.PoliticaLeituraPublica)
            .WithName("CorsPublico");

        group.MapGet("/diagnostico", (HttpContext httpContext) =>
        {
            var origin = httpContext.Request.Headers.Origin.ToString();

            return Results.Ok(new DiagnosticoCorsResponse(
                origin,
                CorsPolicyConfiguration.PoliticaFrontendSpa,
                CorsPolicyConfiguration.ObterOrigensConfiaveis(),
                true));
        })
        .WithName("DiagnosticoCors");

        group.MapGet("/privado", (HttpContext httpContext) =>
        {
            if (!httpContext.Request.Headers.TryGetValue("X-Demo-Token", out var token) ||
                token != "token-seguro")
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                mensagem = "CORS aprovou a leitura, mas a autorização continua no backend.",
                usuario = "lincoln.zocateli"
            });
        })
        .WithName("CorsPrivado");

        group.MapPost("/pedidos", (PedidoCorsRequest pedido, HttpContext httpContext) =>
        {
            var origin = httpContext.Request.Headers.Origin.ToString();

            return Results.Ok(new PedidoCorsResponse(
                Guid.NewGuid(),
                pedido.Produto,
                pedido.Quantidade,
                origin,
                "Preflight tratado pela policy FrontendSpa"));
        })
        .WithName("CriarPedidoCors");
    }

    public sealed record PedidoCorsRequest(string Produto, int Quantidade);

    private sealed record DiagnosticoCorsResponse(
        string OriginRecebida,
        string PoliticaAplicada,
        IReadOnlyCollection<string> OrigensPermitidas,
        bool CredentialsHabilitadas);

    private sealed record PedidoCorsResponse(
        Guid Id,
        string Produto,
        int Quantidade,
        string OriginRecebida,
        string Observacao);
}