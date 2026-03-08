// -----------------------------------------------------------------------
// Artigo: Programação Assíncrona em C#: async/await do Fundamento à Produção
// Exemplos de CancellationToken em diferentes camadas
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace BlogSamples.AsyncParallel;

public static class CancellationExamples
{
    /// <summary>
    /// CancellationToken básico com CancelAfter.
    /// Cancela automaticamente após 3 segundos.
    /// </summary>
    public static async Task ExemploCancelamentoComTimeoutAsync()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            Console.WriteLine("Iniciando operação demorada...");
            await OperacaoDemoradaAsync(cts.Token);
            Console.WriteLine("Operação concluída com sucesso!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⛔ Operação cancelada! Recursos liberados.");
        }
    }

    private static async Task OperacaoDemoradaAsync(CancellationToken cancellationToken)
    {
        for (int i = 1; i <= 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"  Processando etapa {i}/10...");
            await Task.Delay(1000, cancellationToken);
        }
    }
}

// --- Serviço com CancellationToken em todas as camadas ---

public interface IProdutoService
{
    Task<List<Produto>> ObterTodosAsync(CancellationToken cancellationToken = default);
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
}

public class ProdutoService : IProdutoService
{
    private readonly HttpClient _httpClient;

    public ProdutoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Produto>> ObterTodosAsync(
        CancellationToken cancellationToken = default)
    {
        // Simula consulta com suporte a cancelamento
        await Task.Delay(100, cancellationToken);
        return [];
    }

    public async Task<Produto?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/produtos/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<Produto>(cancellationToken: cancellationToken);
    }
}

public record Produto(int Id, string Nome, decimal Preco);

// --- Controller ASP.NET Core com CancellationToken injetado automaticamente ---

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutosController(IProdutoService produtoService)
        => _produtoService = produtoService;

    // ✅ CancellationToken injetado automaticamente pelo ASP.NET Core
    [HttpGet]
    public async Task<ActionResult<List<Produto>>> ObterTodos(
        CancellationToken cancellationToken)
    {
        var produtos = await _produtoService.ObterTodosAsync(cancellationToken);
        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Produto>> ObterPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var produto = await _produtoService.ObterPorIdAsync(id, cancellationToken);
        return produto is null ? NotFound() : Ok(produto);
    }
}
