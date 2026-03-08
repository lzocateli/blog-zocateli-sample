// ==========================================================================
// Artigo: Full-Text Search API REST com C#
// URL: /posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/
// Models, Service e Controller para Full-Text Search
// ==========================================================================

using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.DataAccess.FullTextSearch;

// --- Models ---

public record FtsPagedResult<T>(
    IReadOnlyList<T> Itens,
    int TotalRegistros,
    int TotalPaginas,
    int PaginaAtual,
    int TamanhoPagina);

public record ClienteDto(int Id, string Nome, string CpfCnpj, string Email, string Cidade);

public class Cliente
{
    public int    Id       { get; set; }
    public string Nome     { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Email    { get; set; } = "";
    public string Cidade   { get; set; } = "";
}

// --- Interfaces ---

public interface IClienteRepository
{
    Task<(IReadOnlyList<Cliente> itens, int total)> BuscarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct);
}

public interface IClienteService
{
    Task<FtsPagedResult<ClienteDto>> ListarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct);
}

// --- Service ---

public class ClienteService(IClienteRepository repo) : IClienteService
{
    public async Task<FtsPagedResult<ClienteDto>> ListarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct)
    {
        var (itens, total) = await repo.BuscarAsync(q, pagina, tamanho, ct);

        return new FtsPagedResult<ClienteDto>(
            Itens:          itens.Select(c => new ClienteDto(c.Id, c.Nome, c.CpfCnpj, c.Email, c.Cidade)).ToList(),
            TotalRegistros: total,
            TotalPaginas:   (int)Math.Ceiling((double)total / tamanho),
            PaginaAtual:    pagina,
            TamanhoPagina:  tamanho);
    }
}

// --- Controller ---

[ApiController]
[Route("api/clientes")]
public class ClientesController(IClienteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FtsPagedResult<ClienteDto>>> Listar(
        [FromQuery] string? q,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        if (tamanho > 100) tamanho = 100;
        if (pagina < 1)    pagina = 1;

        if (q is { Length: > 100 })
            return BadRequest("Termo de busca muito longo.");

        if (q is { Length: > 0 and < 3 })
            return BadRequest("O termo de busca deve ter pelo menos 3 caracteres.");

        var resultado = await service.ListarAsync(q, pagina, tamanho, ct);
        return Ok(resultado);
    }
}
