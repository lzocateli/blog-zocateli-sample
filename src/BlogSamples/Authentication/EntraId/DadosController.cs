// -----------------------------------------------------------------------
// Artigo: Autenticação e Autorização: JWT, OAuth2 e OpenID Connect
// API REST protegida com Azure Entra ID (Microsoft.Identity.Web)
// -----------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

namespace BlogSamples.Authentication.EntraId;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DadosController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido — requer access token válido com escopo User.Read.
    /// </summary>
    [HttpGet]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public IActionResult ObterDados()
    {
        var usuario = User.GetDisplayName()
                     ?? User.FindFirst("preferred_username")?.Value
                     ?? "Desconhecido";

        var objectId = User.GetObjectId();
        var tenantId = User.GetTenantId();
        var scopes = User.FindFirst("scp")?.Value;

        return Ok(new
        {
            Mensagem = "Acesso autorizado!",
            Usuario = usuario,
            ObjectId = objectId,
            TenantId = tenantId,
            Scopes = scopes,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint admin — requer role 'Admin' atribuída no Azure Entra ID.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult ObterDadosAdmin()
    {
        var roles = User.Claims
            .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            Mensagem = "Bem-vindo, administrador!",
            Roles = roles,
            Usuario = User.GetDisplayName()
        });
    }

    /// <summary>
    /// Diferencia chamadas delegadas (usuário) de chamadas M2M (daemon).
    /// </summary>
    [HttpGet("info")]
    public IActionResult InformacoesToken()
    {
        var scopeClaim = User.FindFirst("scp");
        var rolesClaim = User.FindFirst("roles");

        var tipoFluxo = scopeClaim != null
            ? "Delegado (usuário via SPA/Web App)"
            : "Aplicação (M2M via Client Credentials)";

        return Ok(new
        {
            TipoFluxo = tipoFluxo,
            Issuer = User.FindFirst("iss")?.Value,
            Audience = User.FindFirst("aud")?.Value,
            Expiracao = User.FindFirst("exp")?.Value,
            Scopes = scopeClaim?.Value,
            Roles = rolesClaim?.Value
        });
    }
}
