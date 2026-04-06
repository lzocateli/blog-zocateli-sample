// -----------------------------------------------------------------------
// Artigo: Zero JavaScript: CRUD Completo com Blazor WASM e Radzen
// Implementação in-memory do serviço de Produtos com ConcurrentDictionary
// Seed estático com 8 categorias e 54 produtos distribuídos
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using BlogSamples.Produtos.Models;

namespace BlogSamples.Produtos;

public sealed class ProdutoService : IProdutoService
{
    private readonly ConcurrentDictionary<int, Produto> _produtos = new();
    private readonly ConcurrentDictionary<int, Categoria> _categorias = new();
    private int _proximoProdutoId;
    private int _proximaCategoriaId;

    public ProdutoService()
    {
        SeedCategorias();
        SeedProdutos();
    }

    // ======================== PRODUTOS ========================

    public Task<PagedResult<ProdutoDto>> ListarProdutosAsync(
        int pagina, int tamanhoPagina, string? filtro)
    {
        var query = _produtos.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro))
            query = query.Where(p =>
                p.Nome.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                (p.Descricao?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false));

        var totalRegistros = query.Count();
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanhoPagina);

        var itens = query
            .OrderBy(p => p.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(new PagedResult<ProdutoDto>(
            itens, pagina, tamanhoPagina, totalRegistros, totalPaginas));
    }

    public Task<ProdutoDto?> ObterProdutoPorIdAsync(int id)
    {
        var dto = _produtos.TryGetValue(id, out var produto) ? MapToDto(produto) : null;
        return Task.FromResult(dto);
    }

    public Task<ProdutoDto> CriarProdutoAsync(CriarProdutoRequest request)
    {
        if (!_categorias.ContainsKey(request.CategoriaId))
            throw new ArgumentException($"Categoria {request.CategoriaId} não encontrada.");

        var produto = new Produto
        {
            Id = Interlocked.Increment(ref _proximoProdutoId),
            Nome = request.Nome,
            Descricao = request.Descricao,
            Preco = request.Preco,
            QuantidadeEstoque = request.QuantidadeEstoque,
            Ativo = request.Ativo,
            CategoriaId = request.CategoriaId,
            DataCriacao = DateTime.UtcNow
        };

        _produtos[produto.Id] = produto;
        return Task.FromResult(MapToDto(produto));
    }

    public Task<ProdutoDto?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request)
    {
        if (!_produtos.TryGetValue(id, out var produto))
            return Task.FromResult<ProdutoDto?>(null);

        if (!_categorias.ContainsKey(request.CategoriaId))
            throw new ArgumentException($"Categoria {request.CategoriaId} não encontrada.");

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Preco = request.Preco;
        produto.QuantidadeEstoque = request.QuantidadeEstoque;
        produto.Ativo = request.Ativo;
        produto.CategoriaId = request.CategoriaId;
        produto.DataAtualizacao = DateTime.UtcNow;

        _produtos[id] = produto;
        return Task.FromResult<ProdutoDto?>(MapToDto(produto));
    }

    public Task<bool> RemoverProdutoAsync(int id)
    {
        return Task.FromResult(_produtos.TryRemove(id, out _));
    }

    // ======================== CATEGORIAS ========================

    public Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync()
    {
        var categorias = _categorias.Values
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome, c.Descricao, c.Ativo))
            .ToList();

        return Task.FromResult<IReadOnlyList<CategoriaDto>>(categorias);
    }

    public Task<CategoriaDto?> ObterCategoriaPorIdAsync(int id)
    {
        var dto = _categorias.TryGetValue(id, out var cat)
            ? new CategoriaDto(cat.Id, cat.Nome, cat.Descricao, cat.Ativo)
            : null;
        return Task.FromResult(dto);
    }

    public Task<CategoriaDto> CriarCategoriaAsync(CriarCategoriaRequest request)
    {
        var categoria = new Categoria
        {
            Id = Interlocked.Increment(ref _proximaCategoriaId),
            Nome = request.Nome,
            Descricao = request.Descricao,
            Ativo = request.Ativo
        };

        _categorias[categoria.Id] = categoria;
        return Task.FromResult(new CategoriaDto(categoria.Id, categoria.Nome, categoria.Descricao, categoria.Ativo));
    }

    public Task<CategoriaDto?> AtualizarCategoriaAsync(int id, AtualizarCategoriaRequest request)
    {
        if (!_categorias.TryGetValue(id, out var categoria))
            return Task.FromResult<CategoriaDto?>(null);

        categoria.Nome = request.Nome;
        categoria.Descricao = request.Descricao;
        categoria.Ativo = request.Ativo;

        _categorias[id] = categoria;
        return Task.FromResult<CategoriaDto?>(
            new CategoriaDto(categoria.Id, categoria.Nome, categoria.Descricao, categoria.Ativo));
    }

    public Task<bool> RemoverCategoriaAsync(int id)
    {
        // Não permitir remover categoria com produtos associados
        if (_produtos.Values.Any(p => p.CategoriaId == id))
            return Task.FromResult(false);

        return Task.FromResult(_categorias.TryRemove(id, out _));
    }

    // ======================== MAPPING ========================

    private ProdutoDto MapToDto(Produto p)
    {
        var categoriaNome = _categorias.TryGetValue(p.CategoriaId, out var cat) ? cat.Nome : "N/A";
        return new ProdutoDto(
            p.Id, p.Nome, p.Descricao, p.Preco, p.QuantidadeEstoque,
            p.Ativo, p.CategoriaId, categoriaNome, p.DataCriacao, p.DataAtualizacao);
    }

    // ======================== SEED ========================

    private void SeedCategorias()
    {
        var categorias = new[]
        {
            new Categoria { Id = 1, Nome = "Eletrônicos", Descricao = "Dispositivos eletrônicos e gadgets" },
            new Categoria { Id = 2, Nome = "Livros", Descricao = "Livros físicos e digitais" },
            new Categoria { Id = 3, Nome = "Roupas", Descricao = "Vestuário masculino e feminino" },
            new Categoria { Id = 4, Nome = "Alimentos", Descricao = "Alimentos e bebidas" },
            new Categoria { Id = 5, Nome = "Ferramentas", Descricao = "Ferramentas manuais e elétricas" },
            new Categoria { Id = 6, Nome = "Esportes", Descricao = "Equipamentos e acessórios esportivos" },
            new Categoria { Id = 7, Nome = "Casa e Jardim", Descricao = "Produtos para casa, decoração e jardim" },
            new Categoria { Id = 8, Nome = "Informática", Descricao = "Hardware, periféricos e acessórios" }
        };

        foreach (var c in categorias)
            _categorias[c.Id] = c;

        _proximaCategoriaId = categorias.Length;
    }

    private void SeedProdutos()
    {
        var produtos = new[]
        {
            // Eletrônicos (1)
            new Produto { Nome = "Smartphone Galaxy S25", Descricao = "Tela AMOLED 6.2\", 256GB, 5G", Preco = 4299.00m, QuantidadeEstoque = 45, CategoriaId = 1 },
            new Produto { Nome = "Fone Bluetooth ANC Pro", Descricao = "Cancelamento de ruído ativo, 30h bateria", Preco = 599.90m, QuantidadeEstoque = 120, CategoriaId = 1 },
            new Produto { Nome = "Smartwatch Fitness Plus", Descricao = "Monitor cardíaco, GPS, resistente à água", Preco = 899.00m, QuantidadeEstoque = 80, CategoriaId = 1 },
            new Produto { Nome = "Tablet Ultra 11\"", Descricao = "Tela 11\" 120Hz, 128GB, caneta inclusa", Preco = 2799.00m, QuantidadeEstoque = 30, CategoriaId = 1 },
            new Produto { Nome = "Caixa de Som Portátil", Descricao = "Bluetooth 5.3, à prova d'água, 20h bateria", Preco = 349.90m, QuantidadeEstoque = 200, CategoriaId = 1 },
            new Produto { Nome = "Carregador Wireless 15W", Descricao = "Qi2, compatível com iPhone e Android", Preco = 129.90m, QuantidadeEstoque = 500, CategoriaId = 1 },
            new Produto { Nome = "Câmera de Segurança Wi-Fi", Descricao = "Full HD, visão noturna, áudio bidirecional", Preco = 249.90m, QuantidadeEstoque = 150, CategoriaId = 1 },

            // Livros (2)
            new Produto { Nome = "Clean Architecture", Descricao = "Robert C. Martin — Arquitetura limpa", Preco = 89.90m, QuantidadeEstoque = 60, CategoriaId = 2 },
            new Produto { Nome = "Domain-Driven Design", Descricao = "Eric Evans — Atacando as complexidades", Preco = 119.90m, QuantidadeEstoque = 40, CategoriaId = 2 },
            new Produto { Nome = "C# in Depth, 5th Edition", Descricao = "Jon Skeet — Guia avançado de C#", Preco = 149.90m, QuantidadeEstoque = 35, CategoriaId = 2 },
            new Produto { Nome = "Designing Data-Intensive Applications", Descricao = "Martin Kleppmann — Sistemas distribuídos", Preco = 179.90m, QuantidadeEstoque = 25, CategoriaId = 2 },
            new Produto { Nome = "The Pragmatic Programmer", Descricao = "Hunt & Thomas — edição do 20º aniversário", Preco = 99.90m, QuantidadeEstoque = 50, CategoriaId = 2 },
            new Produto { Nome = "Refactoring", Descricao = "Martin Fowler — Melhorando o design de código", Preco = 109.90m, QuantidadeEstoque = 45, CategoriaId = 2 },

            // Roupas (3)
            new Produto { Nome = "Camiseta Dev C#", Descricao = "Algodão premium, estampa .NET, preta", Preco = 79.90m, QuantidadeEstoque = 300, CategoriaId = 3 },
            new Produto { Nome = "Moletom Kubernetes", Descricao = "Com capuz, logo K8s bordado, cinza", Preco = 189.90m, QuantidadeEstoque = 100, CategoriaId = 3 },
            new Produto { Nome = "Calça Cargo Tech", Descricao = "Elastano, bolsos laterais, preta", Preco = 159.90m, QuantidadeEstoque = 80, CategoriaId = 3 },
            new Produto { Nome = "Boné Docker", Descricao = "Trucker, logo Docker bordado, azul", Preco = 59.90m, QuantidadeEstoque = 250, CategoriaId = 3 },
            new Produto { Nome = "Meias Programador (Pack 5)", Descricao = "Estampas: Git, Python, JS, C#, Go", Preco = 49.90m, QuantidadeEstoque = 400, CategoriaId = 3 },
            new Produto { Nome = "Jaqueta Softshell DevOps", Descricao = "Impermeável, leve, bolso para laptop 14\"", Preco = 299.90m, QuantidadeEstoque = 40, Ativo = false, CategoriaId = 3 },

            // Alimentos (4)
            new Produto { Nome = "Café Especial Arábica 1kg", Descricao = "Torra média, notas de chocolate e caramelo", Preco = 54.90m, QuantidadeEstoque = 200, CategoriaId = 4 },
            new Produto { Nome = "Chá Verde Orgânico (50 sachês)", Descricao = "Importado do Japão, sem agrotóxicos", Preco = 39.90m, QuantidadeEstoque = 150, CategoriaId = 4 },
            new Produto { Nome = "Barra de Proteína (12 un)", Descricao = "30g proteína, chocolate, sem glúten", Preco = 89.90m, QuantidadeEstoque = 300, CategoriaId = 4 },
            new Produto { Nome = "Castanha do Pará 500g", Descricao = "Selecionada, sem casca, safra 2026", Preco = 34.90m, QuantidadeEstoque = 180, CategoriaId = 4 },
            new Produto { Nome = "Azeite Extra Virgem 500ml", Descricao = "Português, prensado a frio, acidez < 0.3%", Preco = 62.90m, QuantidadeEstoque = 90, CategoriaId = 4 },
            new Produto { Nome = "Chocolate 70% Cacau 200g", Descricao = "Bean-to-bar brasileiro, amêndoas de Ilhéus", Preco = 28.90m, QuantidadeEstoque = 250, CategoriaId = 4 },

            // Ferramentas (5)
            new Produto { Nome = "Kit Chaves de Precisão (64 bits)", Descricao = "Para eletrônicos, notebook, celular", Preco = 89.90m, QuantidadeEstoque = 120, CategoriaId = 5 },
            new Produto { Nome = "Multímetro Digital", Descricao = "True RMS, faixa automática, CAT III", Preco = 199.90m, QuantidadeEstoque = 60, CategoriaId = 5 },
            new Produto { Nome = "Ferro de Solda Estação 60W", Descricao = "Display digital, temperatura ajustável", Preco = 249.90m, QuantidadeEstoque = 40, CategoriaId = 5 },
            new Produto { Nome = "Jogo de Alicates (5 peças)", Descricao = "Cromo-vanádio, cabos emborrachados", Preco = 119.90m, QuantidadeEstoque = 75, CategoriaId = 5 },
            new Produto { Nome = "Trena a Laser 50m", Descricao = "Medição contínua, memória 20 medidas", Preco = 169.90m, QuantidadeEstoque = 55, CategoriaId = 5 },
            new Produto { Nome = "Lanterna Tática Recarregável", Descricao = "2000 lumens, USB-C, resistente à água", Preco = 79.90m, QuantidadeEstoque = 200, Ativo = false, CategoriaId = 5 },

            // Esportes (6)
            new Produto { Nome = "Tênis de Corrida Gel", Descricao = "Amortecimento gel, mesh respirável", Preco = 399.90m, QuantidadeEstoque = 70, CategoriaId = 6 },
            new Produto { Nome = "Garrafa Térmica 1L", Descricao = "Aço inox, mantém temperatura 24h", Preco = 89.90m, QuantidadeEstoque = 300, CategoriaId = 6 },
            new Produto { Nome = "Tapete de Yoga Premium", Descricao = "TPE 6mm, antiderrapante, com alça", Preco = 129.90m, QuantidadeEstoque = 100, CategoriaId = 6 },
            new Produto { Nome = "Elástico de Resistência (Kit 5)", Descricao = "5 níveis de resistência, com bolsa", Preco = 59.90m, QuantidadeEstoque = 250, CategoriaId = 6 },
            new Produto { Nome = "Mochila Trilha 40L", Descricao = "Impermeável, suporte lombar, hidratação", Preco = 349.90m, QuantidadeEstoque = 30, CategoriaId = 6 },
            new Produto { Nome = "Corda de Pular Speed", Descricao = "Cabo de aço revestido, rolamentos duplos", Preco = 39.90m, QuantidadeEstoque = 400, CategoriaId = 6 },
            new Produto { Nome = "Luvas de Musculação", Descricao = "Couro sintético, pulso ajustável", Preco = 49.90m, QuantidadeEstoque = 180, Ativo = false, CategoriaId = 6 },

            // Casa e Jardim (7)
            new Produto { Nome = "Luminária LED Desk", Descricao = "5 intensidades, USB, braço articulado", Preco = 149.90m, QuantidadeEstoque = 90, CategoriaId = 7 },
            new Produto { Nome = "Organizador de Cabos (Kit 10)", Descricao = "Velcro reutilizável, cores sortidas", Preco = 24.90m, QuantidadeEstoque = 500, CategoriaId = 7 },
            new Produto { Nome = "Suporte Monitor Ergonômico", Descricao = "Alumínio, altura regulável, até 32\"", Preco = 219.90m, QuantidadeEstoque = 50, CategoriaId = 7 },
            new Produto { Nome = "Vaso Autoirrigável (3 un)", Descricao = "Reservatório 500ml, indicador de nível", Preco = 79.90m, QuantidadeEstoque = 120, CategoriaId = 7 },
            new Produto { Nome = "Purificador de Ar HEPA", Descricao = "Filtro H13, área até 30m², silencioso", Preco = 599.90m, QuantidadeEstoque = 25, CategoriaId = 7 },
            new Produto { Nome = "Kit Jardinagem (12 peças)", Descricao = "Aço inox, cabos de madeira, com bolsa", Preco = 99.90m, QuantidadeEstoque = 80, CategoriaId = 7 },

            // Informática (8)
            new Produto { Nome = "Monitor 27\" 4K IPS", Descricao = "HDR400, USB-C PD 65W, 99% sRGB", Preco = 2499.00m, QuantidadeEstoque = 20, CategoriaId = 8 },
            new Produto { Nome = "Teclado Mecânico 75%", Descricao = "Switches brown, hot-swap, RGB, wireless", Preco = 449.90m, QuantidadeEstoque = 85, CategoriaId = 8 },
            new Produto { Nome = "Mouse Ergonômico Vertical", Descricao = "6 botões, 4000 DPI, USB-C recarregável", Preco = 199.90m, QuantidadeEstoque = 110, CategoriaId = 8 },
            new Produto { Nome = "SSD NVMe 2TB Gen4", Descricao = "Leitura 7000 MB/s, escrita 6500 MB/s", Preco = 699.90m, QuantidadeEstoque = 60, CategoriaId = 8 },
            new Produto { Nome = "Webcam 4K com Microfone", Descricao = "Campo de visão 90°, foco automático", Preco = 349.90m, QuantidadeEstoque = 75, CategoriaId = 8 },
            new Produto { Nome = "Hub USB-C 11 em 1", Descricao = "HDMI 4K, Ethernet, SD, PD 100W", Preco = 279.90m, QuantidadeEstoque = 130, CategoriaId = 8 },
            new Produto { Nome = "Mousepad Desk 90x40cm", Descricao = "Microfibra, base antiderrapante, costura", Preco = 69.90m, QuantidadeEstoque = 350, CategoriaId = 8 },
            new Produto { Nome = "Memória RAM DDR5 32GB (2x16)", Descricao = "5600MHz, CL36, heatsink alumínio", Preco = 549.90m, QuantidadeEstoque = 40, CategoriaId = 8 },
        };

        for (var i = 0; i < produtos.Length; i++)
        {
            var p = produtos[i];
            p.Id = i + 1;
            p.DataCriacao = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90));
            _produtos[p.Id] = p;
        }

        _proximoProdutoId = produtos.Length;
    }
}
