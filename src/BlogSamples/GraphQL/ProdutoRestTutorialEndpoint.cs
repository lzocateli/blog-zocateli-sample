// -----------------------------------------------------------------------
// Artigo: GraphQL para Iniciantes: Guia Completo com .NET
// Endpoint REST de leitura para comparação com a query GraphQL
// -----------------------------------------------------------------------

using BlogSamples.Produtos;

namespace BlogSamples.GraphQL;

public sealed record ProdutoRestTutorialResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    bool Active,
    int CategoryId,
    string CategoryName);

public static class ProdutoRestTutorialEndpoint
{
    public static void MapProdutoRestTutorialEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/{id:int}", async (int id, IProdutoService service) =>
        {
            var product = await service.ObterProdutoPorIdAsync(id);

            return product is null
                ? Results.NotFound()
                : Results.Ok(new ProdutoRestTutorialResponse(
                    product.Id,
                    product.Nome,
                    product.Descricao,
                    product.Preco,
                    product.QuantidadeEstoque,
                    product.Ativo,
                    product.CategoriaId,
                    product.CategoriaNome));
        })
        .WithName("ObterProdutoRestTutorial")
        .Produces<ProdutoRestTutorialResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}