// -----------------------------------------------------------------------
// Artigo: GraphQL para Iniciantes: Guia Completo com .NET
// Schema GraphQL.NET, types, resolvers, query e mutation sobre produtos
// -----------------------------------------------------------------------

using BlogSamples.Produtos;
using BlogSamples.Produtos.Models;
using GraphQL;
using GraphQL.Types;

namespace BlogSamples.GraphQL;

public sealed class ProdutoGraphType : ObjectGraphType<ProdutoDto>
{
    public ProdutoGraphType()
    {
        Name = "Product";

        Field<NonNullGraphType<IntGraphType>>("id")
            .Resolve(context => context.Source.Id);
        Field<NonNullGraphType<StringGraphType>>("name")
            .Resolve(context => context.Source.Nome);
        Field<NonNullGraphType<DecimalGraphType>>("price")
            .Resolve(context => context.Source.Preco);
        Field<CategoriaGraphType>("category")
            .ResolveAsync(async context =>
            {
                var requestServices = context.RequestServices ?? throw new InvalidOperationException("O request não possui serviços disponíveis.");
                var service = requestServices.GetRequiredService<IProdutoService>();
                return await service.ObterCategoriaPorIdAsync(context.Source.CategoriaId);
            });
    }
}

public sealed class CategoriaGraphType : ObjectGraphType<CategoriaDto>
{
    public CategoriaGraphType()
    {
        Name = "Category";

        Field<NonNullGraphType<IntGraphType>>("id")
            .Resolve(context => context.Source.Id);
        Field<NonNullGraphType<StringGraphType>>("name")
            .Resolve(context => context.Source.Nome);
    }
}

public sealed class CriarProdutoInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
}

public sealed class CriarProdutoInputGraphType : InputObjectGraphType<CriarProdutoInput>
{
    public CriarProdutoInputGraphType()
    {
        Name = "CreateProductInput";
        Field(input => input.Name);
        Field(input => input.Description, nullable: true);
        Field(input => input.Price);
        Field(input => input.Stock);
        Field(input => input.CategoryId);
    }
}

public sealed class ProdutoQuery : ObjectGraphType
{
    public ProdutoQuery()
    {
        Name = "Query";

        Field<ProdutoGraphType>("product")
            .Argument<NonNullGraphType<IntGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var requestServices = context.RequestServices ?? throw new InvalidOperationException("O request não possui serviços disponíveis.");
                var service = requestServices.GetRequiredService<IProdutoService>();
                return await service.ObterProdutoPorIdAsync(context.GetArgument<int>("id"));
            });
    }
}

public sealed class ProdutoMutation : ObjectGraphType
{
    public ProdutoMutation()
    {
        Name = "Mutation";

        Field<ProdutoGraphType>("createProduct")
            .Argument<NonNullGraphType<CriarProdutoInputGraphType>>("input")
            .ResolveAsync(async context =>
            {
                var input = context.GetArgument<CriarProdutoInput>("input");
                var request = new CriarProdutoRequest(
                    input.Name,
                    input.Description,
                    input.Price,
                    input.Stock,
                    input.CategoryId);
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length is < 3 or > 200)
                    validationErrors.Add("Nome deve ter entre 3 e 200 caracteres.");
                if (request.Preco is < 0.01m or > 999999.99m)
                    validationErrors.Add("Preço deve ser entre R$ 0,01 e R$ 999.999,99.");
                if (request.QuantidadeEstoque is < 0 or > 99999)
                    validationErrors.Add("Estoque deve ser entre 0 e 99.999 unidades.");
                if (request.CategoriaId < 1)
                    validationErrors.Add("Categoria deve ser informada.");

                if (validationErrors.Count > 0)
                    throw new ExecutionError(string.Join(" ", validationErrors));

                var requestServices = context.RequestServices ?? throw new InvalidOperationException("O request não possui serviços disponíveis.");
                var service = requestServices.GetRequiredService<IProdutoService>();

                try
                {
                    return await service.CriarProdutoAsync(request);
                }
                catch (ArgumentException exception)
                {
                    throw new ExecutionError(exception.Message);
                }
            });
    }
}

public sealed class ProdutoSchema : Schema
{
    public ProdutoSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new ProdutoQuery();
        Mutation = new ProdutoMutation();
    }
}