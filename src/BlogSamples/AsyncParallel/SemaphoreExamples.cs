// -----------------------------------------------------------------------
// Artigo: Paralelismo em C#: Parallel, PLINQ e Tasks na Prática
// SemaphoreSlim para controle de concorrência a recursos limitados
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel;

public static class SemaphoreExamples
{
    /// <summary>
    /// SemaphoreSlim controla acesso concorrente a um recurso limitado.
    /// Exemplo: máximo 3 chamadas simultâneas a uma API externa.
    /// </summary>
    public static async Task ExemploSemaphoreSlimAsync()
    {
        const int maxConcorrencia = 3;
        using var semaphore = new SemaphoreSlim(maxConcorrencia);

        var ids = Enumerable.Range(1, 20).ToList();

        var tasks = ids.Select(async id =>
        {
            await semaphore.WaitAsync();
            try
            {
                Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Processando {id} (thread {Environment.CurrentManagedThreadId})");
                await Task.Delay(500); // Simula chamada a recurso limitado
                return $"Resultado {id}";
            }
            finally
            {
                semaphore.Release();
            }
        });

        var resultados = await Task.WhenAll(tasks);
        Console.WriteLine($"SemaphoreSlim — {resultados.Length} itens processados com max {maxConcorrencia} concorrentes");
    }
}
