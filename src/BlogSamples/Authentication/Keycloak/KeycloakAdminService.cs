// -----------------------------------------------------------------------
// Artigo: Keycloak: Autenticação Grátis com Container e C#
// Serviço Admin REST API — gerenciamento de usuários programático
// -----------------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlogSamples.Authentication.Keycloak;

public sealed class KeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeycloakAdminService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Keycloak:Authority"]!
            .Replace($"/realms/{configuration["Keycloak:Realm"]}", "");
        _realm = configuration["Keycloak:Realm"] ?? "meu-app";
        _clientId = configuration["Keycloak:ClientId"]!;
        _clientSecret = configuration["Keycloak:ClientSecret"]!;
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/realms/{_realm}/protocol/openid-connect/token", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task<JsonElement> ListarUsuariosAsync(
        int first = 0, int max = 100, CancellationToken ct = default)
    {
        var token = await GetAdminTokenAsync(ct);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users?first={first}&max={max}", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<bool> CriarUsuarioAsync(
        string username, string email, string firstName, string lastName,
        string senha, CancellationToken ct = default)
    {
        var token = await GetAdminTokenAsync(ct);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var novoUsuario = new
        {
            username, email, firstName, lastName,
            enabled = true, emailVerified = true,
            credentials = new[] { new { type = "password", value = senha, temporary = false } }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(novoUsuario), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users", content, ct);
        return response.IsSuccessStatusCode;
    }
}
