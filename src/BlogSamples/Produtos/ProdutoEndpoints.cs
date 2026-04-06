// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Minimal API endpoints para Produtos e Categorias
// Pattern consistente com OrderEndpoints existente
// -----------------------------------------------------------------------

using BlogSamples.Produtos.Models;

namespace BlogSamples.Produtos;

public static class ProdutoEndpoints
{
    public static void MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        // ======================== PRODUTOS ========================
        var produtos = app.MapGroup("/api/produtos")
            .WithTags("Produtos");

        produtos.MapGet("/", async (
            IProdutoService service,
            int pagina = 1,
            int tamanhoPagina = 20,
            string? filtro = null) =>
        {
            var resultado = await service.ListarProdutosAsync(pagina, tamanhoPagina, filtro);
            return Results.Ok(resultado);
        })
        .WithName("ListarProdutos")
        .Produces<PagedResult<ProdutoDto>>();

        produtos.MapGet("/{id:int}", async (int id, IProdutoService service) =>
        {
            var produto = await service.ObterProdutoPorIdAsync(id);
            return produto is not null ? Results.Ok(produto) : Results.NotFound();
        })
        .WithName("ObterProduto")
        .Produces<ProdutoDto>()
        .ProducesProblem(404);

        produtos.MapPost("/", async (CriarProdutoRequest request, IProdutoService service) =>
        {
            var produto = await service.CriarProdutoAsync(request);
            return Results.CreatedAtRoute("ObterProduto", new { id = produto.Id }, produto);
        })
        .WithName("CriarProduto")
        .Produces<ProdutoDto>(201)
        .ProducesValidationProblem();

        produtos.MapPut("/{id:int}", async (int id, AtualizarProdutoRequest request, IProdutoService service) =>
        {
            var produto = await service.AtualizarProdutoAsync(id, request);
            return produto is not null ? Results.Ok(produto) : Results.NotFound();
        })
        .WithName("AtualizarProduto")
        .Produces<ProdutoDto>()
        .ProducesProblem(404);

        produtos.MapDelete("/{id:int}", async (int id, IProdutoService service) =>
        {
            var removido = await service.RemoverProdutoAsync(id);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverProduto")
        .Produces(204)
        .ProducesProblem(404);

        // ======================== CATEGORIAS ========================
        var categorias = app.MapGroup("/api/categorias")
            .WithTags("Categorias");

        categorias.MapGet("/", async (IProdutoService service) =>
        {
            var lista = await service.ListarCategoriasAsync();
            return Results.Ok(lista);
        })
        .WithName("ListarCategorias")
        .Produces<IReadOnlyList<CategoriaDto>>();

        categorias.MapGet("/{id:int}", async (int id, IProdutoService service) =>
        {
            var categoria = await service.ObterCategoriaPorIdAsync(id);
            return categoria is not null ? Results.Ok(categoria) : Results.NotFound();
        })
        .WithName("ObterCategoria")
        .Produces<CategoriaDto>()
        .ProducesProblem(404);

        categorias.MapPost("/", async (CriarCategoriaRequest request, IProdutoService service) =>
        {
            var categoria = await service.CriarCategoriaAsync(request);
            return Results.CreatedAtRoute("ObterCategoria", new { id = categoria.Id }, categoria);
        })
        .WithName("CriarCategoria")
        .Produces<CategoriaDto>(201)
        .ProducesValidationProblem();

        categorias.MapPut("/{id:int}", async (int id, AtualizarCategoriaRequest request, IProdutoService service) =>
        {
            var categoria = await service.AtualizarCategoriaAsync(id, request);
            return categoria is not null ? Results.Ok(categoria) : Results.NotFound();
        })
        .WithName("AtualizarCategoria")
        .Produces<CategoriaDto>()
        .ProducesProblem(404);

        categorias.MapDelete("/{id:int}", async (int id, IProdutoService service) =>
        {
            var removido = await service.RemoverCategoriaAsync(id);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverCategoria")
        .Produces(204)
        .ProducesProblem(404);
    }
}
