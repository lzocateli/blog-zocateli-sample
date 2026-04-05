// ==========================================================================
// Artigo: EF Core 8 Fluent API: Mapeamento e Desacoplamento
// URL: /posts/2026/efcore-fluent-api-mapeamento-desacoplamento/
// Entidades de domínio limpas — sem referência ao EF Core
// ==========================================================================

namespace BlogSamples.DataAccess.FluentApi;

// --- Entidades ---

public class Produto
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }

    public int CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; } = null!;
    public ProdutoDetalhe? Detalhe { get; private set; }
    public ICollection<Tag> Tags { get; private set; } = [];

    // Coleções primitivas em JSON
    public List<string> ImagensUrls { get; private set; } = [];
    public List<string> PalavrasChave { get; private set; } = [];

    // Owned type
    public Dimensoes Dimensoes { get; private set; } = new();
}

public class Categoria
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public bool Ativa { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }
    public ICollection<Produto> Produtos { get; private set; } = [];
}

public class Tag
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public ICollection<Produto> Produtos { get; private set; } = [];
}

public class ProdutoDetalhe
{
    public int Id { get; private set; }
    public string DescricaoCompleta { get; private set; } = string.Empty;
    public string? Especificacoes { get; private set; }
    public double PesoKg { get; private set; }
    public int ProdutoId { get; private set; }
    public Produto Produto { get; private set; } = null!;
}

public class Comentario
{
    public int Id { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public int ProdutoId { get; private set; }
    public Produto Produto { get; private set; } = null!;

    // Auto-referenciamento (resposta a outro comentário)
    public int? ComentarioPaiId { get; private set; }
    public Comentario? ComentarioPai { get; private set; }
    public ICollection<Comentario> Respostas { get; private set; } = [];
}

public class Dimensoes
{
    public double Largura { get; set; }
    public double Altura { get; set; }
    public double Profundidade { get; set; }
    public string Unidade { get; set; } = "cm";
}

// --- Herança: Pagamentos ---

public enum StatusPedidoFluentApi { Pendente, Aprovado, Enviado, Entregue, Cancelado }

public abstract class Pagamento
{
    public int Id { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime PagoEm { get; private set; }
    public int PedidoId { get; private set; }
}

public class PagamentoCartao : Pagamento
{
    public string UltimosDigitos { get; private set; } = string.Empty;
    public string Bandeira { get; private set; } = string.Empty;
    public int Parcelas { get; private set; }
}

public class PagamentoPix : Pagamento
{
    public string ChavePix { get; private set; } = string.Empty;
    public string TransacaoId { get; private set; } = string.Empty;
}

public class PagamentoBoleto : Pagamento
{
    public string CodigoBarras { get; private set; } = string.Empty;
    public DateTime Vencimento { get; private set; }
}
