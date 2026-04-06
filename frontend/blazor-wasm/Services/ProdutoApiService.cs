// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Service HTTP tipado para consumir a API de Produtos
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using BlogSamples.BlazorWasm.Models;

namespace BlogSamples.BlazorWasm.Services;

public class ProdutoApiService(HttpClient http)
{
    public async Task<PagedResult<ProdutoDto>> ListarAsync(
        int pagina = 1, int tamanhoPagina = 20, string? filtro = null)
    {
        var url = $"api/produtos?pagina={pagina}&tamanhoPagina={tamanhoPagina}";
        if (!string.IsNullOrWhiteSpace(filtro))
            url += $"&filtro={Uri.EscapeDataString(filtro)}";

        return await http.GetFromJsonAsync<PagedResult<ProdutoDto>>(url)
            ?? new PagedResult<ProdutoDto>();
    }

    public async Task<ProdutoDto?> ObterPorIdAsync(int id)
        => await http.GetFromJsonAsync<ProdutoDto>($"api/produtos/{id}");

    public async Task<ProdutoDto?> CriarAsync(CriarProdutoRequest request)
    {
        var response = await http.PostAsJsonAsync("api/produtos", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProdutoDto>();
    }

    public async Task<ProdutoDto?> AtualizarAsync(int id, AtualizarProdutoRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/produtos/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProdutoDto>();
    }

    public async Task RemoverAsync(int id)
    {
        var response = await http.DeleteAsync($"api/produtos/{id}");
        response.EnsureSuccessStatusCode();
    }
}
