// -----------------------------------------------------------------------
// Artigo: Keycloak: Autenticação Grátis com Container e C#
// Configuração JWT Bearer + mapeamento de roles do Keycloak
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BlogSamples.Authentication.Keycloak;

public static class KeycloakJwtSetup
{
    /// <summary>
    /// Configura autenticação JWT Bearer com Keycloak.
    /// Mapeia realm_access.roles do token JWT para ClaimTypes.Role.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakConfig = configuration.GetSection("Keycloak");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakConfig["Authority"];
            options.Audience = keycloakConfig["Audience"];
            options.RequireHttpsMetadata = bool.Parse(
                keycloakConfig["RequireHttpsMetadata"] ?? "true");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = keycloakConfig["Authority"],
                ValidateAudience = true,
                ValidAudience = keycloakConfig["Audience"],
                ValidateLifetime = true,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = "preferred_username"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    MapKeycloakRolesToClaims(context);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin"));
            options.AddPolicy("UserOrAdmin", policy =>
                policy.RequireRole("user", "admin"));
            options.AddPolicy("ManagerOnly", policy =>
                policy.RequireRole("manager"));
        });

        return services;
    }

    /// <summary>
    /// Extrai realm_access.roles do token JWT do Keycloak
    /// e adiciona como claims de Role no ClaimsPrincipal.
    /// </summary>
    private static void MapKeycloakRolesToClaims(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return;

        var realmAccess = context.Principal.FindFirst("realm_access");
        if (realmAccess is null)
            return;

        using var doc = JsonDocument.Parse(realmAccess.Value);
        if (!doc.RootElement.TryGetProperty("roles", out var roles))
            return;

        foreach (var role in roles.EnumerateArray())
        {
            var roleName = role.GetString();
            if (!string.IsNullOrEmpty(roleName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }
    }
}
