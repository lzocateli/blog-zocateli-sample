// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Entidade de categorias de produtos
// -----------------------------------------------------------------------

namespace BlogSamples.Produtos.Models;

public sealed class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}
