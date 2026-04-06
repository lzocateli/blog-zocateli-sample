using System.ComponentModel.DataAnnotations;

namespace BlogSamples.BlazorWasm.Models;

public class CriarProdutoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    public string Nome { get; set; } = default!;

    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Preço é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Preço deve ser entre R$ 0,01 e R$ 999.999,99")]
    public decimal Preco { get; set; }

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0, 99999, ErrorMessage = "Estoque deve ser entre 0 e 99.999")]
    public int QuantidadeEstoque { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
    public int CategoriaId { get; set; }

    public bool Ativo { get; set; } = true;
}

public class AtualizarProdutoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    public string Nome { get; set; } = default!;

    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Preço é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Preço deve ser entre R$ 0,01 e R$ 999.999,99")]
    public decimal Preco { get; set; }

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0, 99999, ErrorMessage = "Estoque deve ser entre 0 e 99.999")]
    public int QuantidadeEstoque { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
    public int CategoriaId { get; set; }

    public bool Ativo { get; set; }
}

public class CriarCategoriaRequest
{
    [Required(ErrorMessage = "Nome da categoria é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = default!;

    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public class AtualizarCategoriaRequest
{
    [Required(ErrorMessage = "Nome da categoria é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = default!;

    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}
