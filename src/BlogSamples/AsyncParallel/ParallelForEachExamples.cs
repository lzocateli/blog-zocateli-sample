// -----------------------------------------------------------------------
// Artigo: Paralelismo em C#: Parallel, PLINQ e Tasks na Prática
// Exemplos de Parallel.ForEach e Parallel.ForEachAsync
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace BlogSamples.AsyncParallel;

public static class ParallelForEachExamples
{
    /// <summary>
    /// Parallel.ForEach com ConcurrentBag para acumular resultados.
    /// </summary>
    public static void ExemploParallelForEach()
    {
        var itens = Enumerable.Range(1, 100).ToList();
        var resultados = new ConcurrentBag<string>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        Parallel.ForEach(itens, options, item =>
        {
            // Simula processamento CPU-bound
            var resultado = $"Item {item} processado na thread {Environment.CurrentManagedThreadId}";
            resultados.Add(resultado);
        });

        Console.WriteLine($"Parallel.ForEach — {resultados.Count} itens processados");
    }

    /// <summary>
    /// Parallel.ForEachAsync — combina async I/O com paralelismo.
    /// </summary>
    public static async Task ExemploParallelForEachAsyncAsync()
    {
        var ids = Enumerable.Range(1, 50).ToList();
        var resultados = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(ids, new ParallelOptions
        {
            MaxDegreeOfParallelism = 4
        },
        async (id, ct) =>
        {
            // Simula chamada async (I/O-bound)
            await Task.Delay(100, ct);
            resultados.Add($"ID {id} processado");
        });

        Console.WriteLine($"Parallel.ForEachAsync — {resultados.Count} itens processados");
    }

    /// <summary>
    /// Dictionary indexing para O(1) lookups (em vez de O(n) com List).
    /// </summary>
    public static void ExemploDictionaryIndexing()
    {
        var produtos = Enumerable.Range(1, 10000)
            .Select(i => new { Id = i, Nome = $"Produto {i}", Preco = i * 1.5m })
            .ToList();

        // ❌ O(n) — busca linear na lista
        // var resultado = produtos.FirstOrDefault(p => p.Id == 5000);

        // ✅ O(1) — index com Dictionary
        var indice = produtos.ToDictionary(p => p.Id);
        var resultado = indice[5000];

        Console.WriteLine($"Dictionary lookup — {resultado.Nome}: R${resultado.Preco}");
    }
}
