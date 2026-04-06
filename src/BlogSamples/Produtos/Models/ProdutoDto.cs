// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// DTOs de resposta para Produtos e Categorias + PagedResult genérico
// -----------------------------------------------------------------------

namespace BlogSamples.Produtos.Models;

public record ProdutoDto(
    int Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEstoque,
    bool Ativo,
    int CategoriaId,
    string CategoriaNome,
    DateTime DataCriacao,
    DateTime? DataAtualizacao);

public record CategoriaDto(int Id, string Nome, string? Descricao, bool Ativo);

public record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int PaginaAtual,
    int TamanhoPagina,
    int TotalRegistros,
    int TotalPaginas);
