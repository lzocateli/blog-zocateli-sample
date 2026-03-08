// -----------------------------------------------------------------------
// Artigo: Keycloak: Autenticação Grátis com Container e C#
// Controller com endpoints protegidos por roles do Keycloak
// -----------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.Authentication.Keycloak;

[ApiController]
[Route("api/[controller]")]
public class ProtegidoController : ControllerBase
{
    [HttpGet("publico")]
    [AllowAnonymous]
    public IActionResult RotaPublica()
        => Ok(new { mensagem = "Esta rota é pública e não requer autenticação." });

    [HttpGet("autenticado")]
    [Authorize]
    public IActionResult RotaAutenticada()
    {
        var username = User.FindFirst("preferred_username")?.Value;
        var email = User.FindFirst("email")?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        return Ok(new
        {
            mensagem = "Você está autenticado via Keycloak!",
            usuario = username,
            email,
            roles
        });
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult RotaAdmin()
        => Ok(new
        {
            mensagem = "Acesso administrativo concedido.",
            usuario = User.FindFirst("preferred_username")?.Value
        });

    [HttpGet("manager")]
    [Authorize(Policy = "ManagerOnly")]
    public IActionResult RotaManager()
        => Ok(new
        {
            mensagem = "Acesso de manager concedido.",
            usuario = User.FindFirst("preferred_username")?.Value
        });

    [HttpGet("claims")]
    [Authorize]
    public IActionResult VerClaims()
    {
        var claims = User.Claims.Select(c => new { tipo = c.Type, valor = c.Value });
        return Ok(claims);
    }
}
