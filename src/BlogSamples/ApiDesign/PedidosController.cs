// -----------------------------------------------------------------------
// Artigo: Design de APIs REST: Verbos HTTP e Parameter Binding no ASP.NET Core
// Exemplos de CRUD completo com GET, POST, PUT, PATCH, DELETE
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using BlogSamples.ApiDesign.Models;

namespace BlogSamples.ApiDesign;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _service;
    public PedidosController(IPedidoService service) => _service = service;

    /// GET /api/pedidos?pagina=1&tamanhoPagina=20&status=aberto
    [HttpGet]
    [ProducesResponseType<PagedResult<PedidoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var resultado = await _service.ListarAsync(pagina, tamanhoPagina, status, ct);
        return Ok(resultado);
    }

    /// GET /api/pedidos/42
    [HttpGet("{id:int}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var pedido = await _service.ObterPorIdAsync(id, ct);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    /// GET /api/pedidos/42/itens
    [HttpGet("{id:int}/itens")]
    [ProducesResponseType<IEnumerable<ItemPedidoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarItens(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var itens = await _service.ListarItensAsync(id, ct);
        return Ok(itens);
    }

    /// POST /api/pedidos
    [HttpPost]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPedidoRequest request,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var pedido = await _service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id }, pedido);
    }

    /// PUT /api/pedidos/42  — substituição completa
    [HttpPut("{id:int}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Substituir(
        [FromRoute] int id,
        [FromBody] SubstituirPedidoRequest request,
        CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("O id da URL não corresponde ao id do payload.");

        var atualizado = await _service.SubstituirAsync(id, request, ct);
        return atualizado is null ? NotFound() : Ok(atualizado);
    }

    /// PATCH /api/pedidos/42  — Merge Patch (atualização parcial)
    [HttpPatch("{id:int}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarParcial(
        [FromRoute] int id,
        [FromBody] AtualizarPedidoRequest request,
        CancellationToken ct = default)
    {
        var atualizado = await _service.AtualizarParcialAsync(id, request, ct);
        return atualizado is null ? NotFound() : Ok(atualizado);
    }

    /// DELETE /api/pedidos/42
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var resultado = await _service.RemoverAsync(id, ct);

        return resultado switch
        {
            DeletarResultado.NaoEncontrado => NotFound(),
            DeletarResultado.ConflitoDependencias =>
                Conflict(new { error = "Pedido possui itens vinculados." }),
            _ => NoContent()
        };
    }

    /// DELETE /api/pedidos/42/itens/7
    [HttpDelete("{id:int}/itens/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverItem(
        [FromRoute] int id,
        [FromRoute] int itemId,
        CancellationToken ct = default)
    {
        var removido = await _service.RemoverItemAsync(id, itemId, ct);
        return removido ? NoContent() : NotFound();
    }
}
