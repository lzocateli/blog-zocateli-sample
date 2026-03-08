// -----------------------------------------------------------------------
// Artigo: Paralelismo em C#: Parallel, PLINQ e Tasks na Pratica
// Exemplos de PLINQ: AsParallel, WithDegreeOfParallelism
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel;

public static class PlinqExamples
{
    /// <summary>
    /// PLINQ basico com .AsParallel().
    /// </summary>
    public static void ExemploBasicoPlinq()
    {
        Console.WriteLine("\n=== PLINQ Basico ===\n");

        var numeros = Enumerable.Range(1, 10_000).ToList();

        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        var resultadoSeq = numeros
            .Where(n => n % 2 == 0)
            .Select(n => n * n)
            .ToList();
        sw1.Stop();

        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        var resultadoPar = numeros
            .AsParallel()
            .Where(n => n % 2 == 0)
            .Select(n => n * n)
            .ToList();
        sw2.Stop();

        Console.WriteLine($"LINQ sequencial:  {sw1.ElapsedMilliseconds}ms");
        Console.WriteLine($"PLINQ paralelo:   {sw2.ElapsedMilliseconds}ms");
        Console.WriteLine($"Ambos retornam {resultadoSeq.Count} itens\n");
    }

    /// <summary>
    /// Controlando grau de paralelismo com WithDegreeOfParallelism.
    /// </summary>
    public static void ExemploComGrauDeParalelismo()
    {
        Console.WriteLine("\n=== PLINQ com WithDegreeOfParallelism ===\n");

        var dados = Enumerable.Range(1, 100_000).ToList();

        var resultado = dados
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .Where(n => EhNumeroPrimo(n))
            .OrderBy(n => n)
            .ToList();

        Console.WriteLine($"Numeros primos encontrados: {resultado.Count}\n");
    }

    /// <summary>
    /// ForceParallelism para colecoes pequenas.
    /// </summary>
    public static void ExemploComExecutionMode()
    {
        Console.WriteLine("\n=== PLINQ com ForceParallelism ===\n");

        var pequenhaColecao = Enumerable.Range(1, 100).ToList();

        var resultado = pequenhaColecao
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .Select(n => n * n)
            .ToList();

        Console.WriteLine($"Itens processados em paralelo (colecao pequena): {resultado.Count}\n");
    }

    /// <summary>
    /// Pipeline complexo: Where, Select, OrderBy em paralelo.
    /// </summary>
    public static void ExemploPipelineComplexo()
    {
        Console.WriteLine("\n=== PLINQ Pipeline Complexo ===\n");

        var produtos = GerarProdutos(50_000);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var resultado = produtos
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Where(p => p.Preco > 100)
            .Where(p => p.Categoria == "Eletronicos")
            .Select(p => new { p.Nome, p.Preco, Desconto = p.Preco * 0.1m })
            .OrderByDescending(x => x.Preco)
            .Take(100)
            .ToList();

        sw.Stop();
        Console.WriteLine($"Processados {resultado.Count} itens em {sw.ElapsedMilliseconds}ms\n");
    }

    /// <summary>
    /// AsOrdered: preserva a ordem de entrada (com custo adicional).
    /// </summary>
    public static void ExemploComOrdenacao()
    {
        Console.WriteLine("\n=== PLINQ com AsOrdered ===\n");

        var lista = Enumerable.Range(1, 1000).ToList();

        var semOrdem = lista
            .AsParallel()
            .Where(n => n % 2 == 0)
            .ToList();

        var comOrdem = lista
            .AsParallel()
            .AsOrdered()
            .Where(n => n % 2 == 0)
            .ToList();

        Console.WriteLine($"Sem ordem: {semOrdem.Count} itens");
        Console.WriteLine($"Com ordem: {comOrdem.Count} itens (ordem garantida)\n");
    }

    /// <summary>
    /// GroupBy em paralelo.
    /// </summary>
    public static void ExemploGroupByParalelo()
    {
        Console.WriteLine("\n=== PLINQ GroupBy ===\n");

        var vendas = GerarVendas(10_000);

        var agrupadoPorCategoria = vendas
            .AsParallel()
            .GroupBy(v => v.Categoria)
            .Select(g => new
            {
                Categoria = g.Key,
                Total = g.Sum(v => v.Valor),
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        Console.WriteLine("Vendas por Categoria:");
        foreach (var cat in agrupadoPorCategoria.Take(5))
            Console.WriteLine($"  {cat.Categoria}: {cat.Quantidade} itens - R$ {cat.Total:F2}");
        Console.WriteLine();
    }

    /// <summary>
    /// Agregacao paralela: Sum em paralelo.
    /// </summary>
    public static void ExemploAgregacao()
    {
        Console.WriteLine("\n=== PLINQ Agregacao ===\n");

        var numeros = Enumerable.Range(1, 1_000_000).ToList();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var soma = numeros
            .AsParallel()
            .Where(n => n % 2 == 0)
            .Sum(n => (long)n);
        sw.Stop();

        Console.WriteLine($"Soma paralela de {numeros.Count:N0} numeros: {soma:N0}");
        Console.WriteLine($"Tempo: {sw.ElapsedMilliseconds}ms\n");
    }

    private static List<Produto> GerarProdutos(int quantidade)
        => Enumerable.Range(1, quantidade)
            .Select(i => new Produto
            {
                Id = i,
                Nome = $"Produto {i}",
                Preco = (decimal)Random.Shared.Next(50, 1000),
                Categoria = new[] { "Eletronicos", "Livros", "Alimentos" }[Random.Shared.Next(3)]
            }).ToList();

    private static List<Venda> GerarVendas(int quantidade)
        => Enumerable.Range(1, quantidade)
            .Select(i => new Venda
            {
                Categoria = new[] { "TI", "Saude", "Energia" }[Random.Shared.Next(3)],
                Valor = Random.Shared.Next(1000, 50000)
            }).ToList();

    private static bool EhNumeroPrimo(int numero)
    {
        if (numero < 2) return false;
        if (numero == 2) return true;
        if (numero % 2 == 0) return false;
        for (int i = 3; i * i <= numero; i += 2)
            if (numero % i == 0) return false;
        return true;
    }

    private class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }

    private class Venda
    {
        public string Categoria { get; set; } = string.Empty;
        public int Valor { get; set; }
    }
}
