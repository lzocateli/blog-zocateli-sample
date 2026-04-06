// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Entidade principal do domínio Produtos
// -----------------------------------------------------------------------

namespace BlogSamples.Produtos.Models;

public sealed class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public int QuantidadeEstoque { get; set; }
    public bool Ativo { get; set; } = true;
    public int CategoriaId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
}
