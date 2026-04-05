// -----------------------------------------------------------------------
// Artigo: BFF Backend For Frontend: Segurança em SPAs
// Endpoints do BFF: verificação de autenticação e proxy de APIs
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using System.Net.Http.Headers;

namespace BlogSamples.Authentication.Bff;

public static class BffProxyEndpoints
{
    public static void MapBffEndpoints(this WebApplication app)
    {
        // Verificar estado de autenticação
        app.MapGet("/bff/user", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var claims = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Results.Ok(new
            {
                IsAuthenticated = true,
                Claims = claims,
                Name = ctx.User.Identity.Name
            });
        });

        // Login — redireciona para o IdP
        app.MapGet("/bff/login", (HttpContext ctx, string? returnUrl) =>
        {
            var validReturnUrl = returnUrl?.StartsWith("/") == true ? returnUrl : "/";
            return Results.Challenge(new AuthenticationProperties { RedirectUri = validReturnUrl });
        });

        // Logout
        app.MapPost("/bff/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync();
            return Results.Ok(new { mensagem = "Logout realizado." });
        }).RequireAuthorization();

        // Antiforgery token endpoint
        app.MapGet("/bff/antiforgery", (IAntiforgery antiforgery, HttpContext ctx) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        // Proxy genérico para APIs backend
        app.MapMethods("/api/{**path}", new[] { "GET", "POST", "PUT", "PATCH", "DELETE" },
            async (HttpContext ctx, IHttpClientFactory clientFactory, string path) =>
            {
                if (ctx.User.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                var accessToken = await ctx.GetTokenAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                    return Results.Unauthorized();

                var client = clientFactory.CreateClient("ApiProxy");
                var request = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (ctx.Request.ContentLength > 0)
                    request.Content = new StreamContent(ctx.Request.Body);

                var response = await client.SendAsync(request, ctx.RequestAborted);
                var content = await response.Content.ReadAsStringAsync(ctx.RequestAborted);

                return Results.Content(content, response.Content.Headers.ContentType?.ToString(),
                    statusCode: (int)response.StatusCode);
            }).RequireAuthorization();
    }
}
