using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlogSamples.Tests.Security;

public sealed class CorsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Com_Origin_Permitida_Deve_Retornar_Header_Cors()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/cors/diagnostico");
        request.Headers.Add("Origin", "https://app.zocate.li");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://app.zocate.li", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains("Origin", response.Headers.Vary);
    }

    [Fact]
    public async Task Get_Com_Origin_Nao_Permitida_Nao_Deve_Retornar_Header_Cors()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/cors/diagnostico");
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
        Assert.False(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out _));
    }

    [Fact]
    public async Task Options_Preflight_Com_Origin_Permitida_Deve_Retornar_Headers_Esperados()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/security/cors/pedidos");
        request.Headers.Add("Origin", "https://admin.zocate.li");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization, Content-Type");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://admin.zocate.li", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("Authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }
}