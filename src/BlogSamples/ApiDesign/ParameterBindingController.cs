// -----------------------------------------------------------------------
// Artigo: Design de APIs REST: Verbos HTTP e Parameter Binding no ASP.NET Core
// Exemplos de Parameter Binding: FromRoute, FromQuery, FromBody, FromForm, FromHeader
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.ApiDesign;

[ApiController]
[Route("api/[controller]")]
public class ParameterBindingController : ControllerBase
{
    // --- [FromRoute] ---
    // GET /api/parameterbinding/{id}/itens/{itemId}
    [HttpGet("{id:int}/itens/{itemId:int}")]
    public IActionResult ObterItem(
        [FromRoute] int id,
        [FromRoute] int itemId)
        => Ok(new { id, itemId });

    // --- [FromQuery] ---
    // GET /api/parameterbinding/pesquisar?categoria=eletronicos&precoMin=100&precoMax=500
    [HttpGet("pesquisar")]
    public IActionResult Pesquisar([FromQuery] FiltrosProdutoQuery filtros)
        => Ok(filtros);

    // GET /api/parameterbinding/batch?ids=1&ids=2&ids=3
    [HttpGet("batch")]
    public IActionResult ObterEmLote([FromQuery] int[] ids)
        => Ok(new { ids });

    // GET /api/parameterbinding/relatorio?data_inicio=2026-01-01&data_fim=2026-03-01
    [HttpGet("relatorio")]
    public IActionResult Relatorio(
        [FromQuery(Name = "data_inicio")] DateOnly dataInicio,
        [FromQuery(Name = "data_fim")] DateOnly dataFim)
        => Ok(new { dataInicio, dataFim });

    // --- [FromBody] ---
    // POST /api/parameterbinding/pedido
    [HttpPost("pedido")]
    public IActionResult CriarPedido([FromBody] CriarPedidoBodyRequest request)
        => Ok(request);

    // --- [FromForm] ---
    // POST /api/parameterbinding/{id}/imagem
    [HttpPost("{id:int}/imagem")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB máximo
    public async Task<IActionResult> UploadImagem(
        [FromRoute] int id,
        [FromForm] IFormFile imagem,
        [FromForm] string? altText,
        [FromForm] bool isPrincipal = false)
    {
        if (imagem.Length == 0)
            return BadRequest("Arquivo vazio.");

        var extensoes = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(imagem.FileName).ToLowerInvariant();
        if (!extensoes.Contains(ext))
            return BadRequest($"Extensão '{ext}' não permitida.");

        await Task.CompletedTask; // Placeholder para upload real
        return Ok(new { id, altText, isPrincipal, fileName = imagem.FileName });
    }

    // --- [FromHeader] ---
    // GET /api/parameterbinding/com-headers
    [HttpGet("com-headers")]
    public IActionResult ComHeaders(
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantId,
        [FromQuery] int pagina = 1)
        => Ok(new { correlationId, tenantId, pagina });

    // POST /api/parameterbinding/pagamentos — Idempotency-Key header
    [HttpPost("pagamentos")]
    public IActionResult ProcessarPagamento(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] PagamentoRequest request)
        => Ok(new { idempotencyKey, request });
}

// --- DTOs ---

public record FiltrosProdutoQuery(
    string? Categoria,
    decimal? PrecoMin,
    decimal? PrecoMax,
    bool? EmEstoque,
    string? Termo);

public record CriarPedidoBodyRequest(
    [Required] int ClienteId,
    [Required, MinLength(1)] List<CriarItemRequest> Itens,
    string? Observacoes);

public record CriarItemRequest(
    [Required] int ProdutoId,
    [Required, Range(1, 9999)] int Quantidade,
    decimal? DescontoUnitario);

public record PagamentoRequest(decimal Valor, string Moeda);
