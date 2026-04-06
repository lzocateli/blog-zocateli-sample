// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// DTOs de request com DataAnnotations para validação server-side
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace BlogSamples.Produtos.Models;

public record CriarProdutoRequest(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    string Nome,

    string? Descricao,

    [Required(ErrorMessage = "Preço é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Preço deve ser entre R$ 0,01 e R$ 999.999,99")]
    decimal Preco,

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0, 99999, ErrorMessage = "Estoque deve ser entre 0 e 99.999")]
    int QuantidadeEstoque,

    [Required(ErrorMessage = "Categoria é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
    int CategoriaId,

    bool Ativo = true);

public record AtualizarProdutoRequest(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    string Nome,

    string? Descricao,

    [Required(ErrorMessage = "Preço é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Preço deve ser entre R$ 0,01 e R$ 999.999,99")]
    decimal Preco,

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0, 99999, ErrorMessage = "Estoque deve ser entre 0 e 99.999")]
    int QuantidadeEstoque,

    [Required(ErrorMessage = "Categoria é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
    int CategoriaId,

    bool Ativo);

public record CriarCategoriaRequest(
    [Required(ErrorMessage = "Nome da categoria é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    string Nome,

    string? Descricao,

    bool Ativo = true);

public record AtualizarCategoriaRequest(
    [Required(ErrorMessage = "Nome da categoria é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    string Nome,

    string? Descricao,

    bool Ativo);
