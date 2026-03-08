// -----------------------------------------------------------------------
// Artigo: Keycloak: Autenticação Grátis com Container e C#
// Logout via back-channel (refresh token) e front-channel (redirect URL)
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.Authentication.Keycloak;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Logout via back-channel: invalida a sessão no Keycloak usando refresh_token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken ct = default)
    {
        var authority = _configuration["Keycloak:Authority"];
        var clientId = _configuration["Keycloak:ClientId"];
        var clientSecret = _configuration["Keycloak:ClientSecret"];

        var httpClient = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId!,
            ["client_secret"] = clientSecret!,
            ["refresh_token"] = request.RefreshToken
        });

        var response = await httpClient.PostAsync(
            $"{authority}/protocol/openid-connect/logout", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return BadRequest(new { mensagem = "Falha ao realizar logout no Keycloak.", detalhes = error });
        }

        return Ok(new { mensagem = "Logout realizado com sucesso." });
    }

    /// <summary>
    /// Gera a URL de logout do Keycloak para redirecionamento (Front-Channel).
    /// </summary>
    [HttpGet("logout-url")]
    [Authorize]
    public IActionResult GetLogoutUrl([FromQuery] string? redirectUri = null)
    {
        var authority = _configuration["Keycloak:Authority"];
        var clientId = _configuration["Keycloak:ClientId"];

        var logoutUrl = $"{authority}/protocol/openid-connect/logout?client_id={clientId}";

        if (!string.IsNullOrEmpty(redirectUri))
            logoutUrl += $"&post_logout_redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Ok(new { logoutUrl });
    }
}

public record LogoutRequest(string RefreshToken);
