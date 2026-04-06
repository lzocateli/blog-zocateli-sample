namespace BlogSamples.BlazorWasm.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Itens { get; set; } = [];
    public int PaginaAtual { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}
