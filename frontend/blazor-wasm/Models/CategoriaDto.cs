namespace BlogSamples.BlazorWasm.Models;

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}
