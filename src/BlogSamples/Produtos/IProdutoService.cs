// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Interface do serviço de Produtos e Categorias
// -----------------------------------------------------------------------

using BlogSamples.Produtos.Models;

namespace BlogSamples.Produtos;

public interface IProdutoService
{
    Task<PagedResult<ProdutoDto>> ListarProdutosAsync(int pagina, int tamanhoPagina, string? filtro);
    Task<ProdutoDto?> ObterProdutoPorIdAsync(int id);
    Task<ProdutoDto> CriarProdutoAsync(CriarProdutoRequest request);
    Task<ProdutoDto?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request);
    Task<bool> RemoverProdutoAsync(int id);

    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync();
    Task<CategoriaDto?> ObterCategoriaPorIdAsync(int id);
    Task<CategoriaDto> CriarCategoriaAsync(CriarCategoriaRequest request);
    Task<CategoriaDto?> AtualizarCategoriaAsync(int id, AtualizarCategoriaRequest request);
    Task<bool> RemoverCategoriaAsync(int id);
}
