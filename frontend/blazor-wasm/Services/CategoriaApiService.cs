// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Service HTTP tipado para consumir a API de Categorias
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using BlogSamples.BlazorWasm.Models;

namespace BlogSamples.BlazorWasm.Services;

public class CategoriaApiService(HttpClient http)
{
    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<CategoriaDto>>("api/categorias")
            ?? [];
    }

    public async Task<CategoriaDto?> ObterPorIdAsync(int id)
        => await http.GetFromJsonAsync<CategoriaDto>($"api/categorias/{id}");

    public async Task<CategoriaDto?> CriarAsync(CriarCategoriaRequest request)
    {
        var response = await http.PostAsJsonAsync("api/categorias", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoriaDto>();
    }

    public async Task<CategoriaDto?> AtualizarAsync(int id, AtualizarCategoriaRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/categorias/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoriaDto>();
    }

    public async Task RemoverAsync(int id)
    {
        var response = await http.DeleteAsync($"api/categorias/{id}");
        response.EnsureSuccessStatusCode();
    }
}
