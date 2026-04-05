// -----------------------------------------------------------------------
// Artigo: Paralelismo em C#: Parallel, PLINQ e Tasks na Prática
// Exemplos de PLINQ: AsParallel, WithDegreeOfParallelism, AsOrdered
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel;

public static class PlinqExamples
{
    /// <summary>
    /// PLINQ básico com AsParallel e WithDegreeOfParallelism.
    /// </summary>
    public static void ExemploPlinqBasico()
    {
        var numeros = Enumerable.Range(1, 100_000).ToList();

        // PLINQ: paraleliza a operação automaticamente
        var pares = numeros
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Where(n => n % 2 == 0)
            .Select(n => n * n)
            .ToList();

        Console.WriteLine($"PLINQ básico — {pares.Count} resultados");
    }

    /// <summary>
    /// PLINQ com AsOrdered para manter a ordem original.
    /// </summary>
    public static void ExemploPlinqOrdenado()
    {
        var dados = Enumerable.Range(1, 1000).ToList();

        var resultados = dados
            .AsParallel()
            .AsOrdered() // Mantém a ordem dos elementos de entrada
            .Where(d => d % 3 == 0)
            .Select(d => $"Item {d}")
            .ToList();

        Console.WriteLine($"PLINQ ordenado — {resultados.Count} resultados, primeiro: {resultados.FirstOrDefault()}");
    }
}
