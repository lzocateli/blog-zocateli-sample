// -----------------------------------------------------------------------
// Artigo: BFF Backend For Frontend: Segurança em SPAs
// Configuração completa de BFF com OpenID Connect, cookies HttpOnly,
// proteção CSRF e proxy de APIs
// -----------------------------------------------------------------------

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BlogSamples.Authentication.Bff;

public static class BffConfiguration
{
    /// <summary>
    /// Configura BFF com autenticação OpenID Connect, sessão HttpOnly e proxy via HttpClient.
    /// </summary>
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "MinhaSPA.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // Retornar 401 em vez de redirecionar para login em chamadas AJAX
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Path.StartsWithSegments("/bff"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            var azureAd = configuration.GetSection("AzureAd");

            options.Authority = $"{azureAd["Instance"]}{azureAd["TenantId"]}/v2.0";
            options.ClientId = azureAd["ClientId"];
            options.ClientSecret = azureAd["ClientSecret"];
            options.CallbackPath = azureAd["CallbackPath"];
            options.SignedOutCallbackPath = azureAd["SignedOutCallbackPath"];

            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");
        });

        // CORS para desenvolvimento local
        services.AddCors(options =>
        {
            options.AddPolicy("PermitirAngular", corsBuilder =>
            {
                corsBuilder
                    .WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Antiforgery (proteção CSRF)
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Angular precisa ler este cookie
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        // HttpClient nomeado para proxy das APIs
        var apiBaseUrl = configuration["ApiProxy:BaseUrl"] ?? "https://api.seudominio.com";
        services.AddHttpClient("ApiProxy", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
