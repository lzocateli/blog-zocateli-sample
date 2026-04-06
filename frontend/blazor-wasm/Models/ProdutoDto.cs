namespace BlogSamples.BlazorWasm.Models;

public class ProdutoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public int QuantidadeEstoque { get; set; }
    public bool Ativo { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = default!;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}
