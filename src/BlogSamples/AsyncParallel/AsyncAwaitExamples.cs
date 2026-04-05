// -----------------------------------------------------------------------
// Artigo: Programação Assíncrona em C#: async/await do Fundamento à Produção
// Exemplos de async/await, Task.WhenAll, síncrono vs assíncrono
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace BlogSamples.AsyncParallel;

public static class AsyncAwaitExamples
{
    /// <summary>
    /// Versão SÍNCRONA — bloqueia a thread durante cada chamada.
    /// Tempo esperado: ~6000ms (2s + 2s + 2s — sequencial)
    /// </summary>
    public static void ExemploSincrono()
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("Iniciando operações SÍNCRONAS...\n");

        var resultado1 = BuscarDadosSincrono("Serviço A", 2000);
        var resultado2 = BuscarDadosSincrono("Serviço B", 2000);
        var resultado3 = BuscarDadosSincrono("Serviço C", 2000);

        Console.WriteLine($"\n{resultado1}");
        Console.WriteLine(resultado2);
        Console.WriteLine(resultado3);

        sw.Stop();
        Console.WriteLine($"\n⏱️ Tempo total: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Versão ASSÍNCRONA — executa todas as chamadas ao mesmo tempo.
    /// Tempo esperado: ~2000ms (todas executam "ao mesmo tempo")
    /// </summary>
    public static async Task ExemploAssincronoAsync()
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("Iniciando operações ASSÍNCRONAS...\n");

        var task1 = BuscarDadosAsync("Serviço A", 2000);
        var task2 = BuscarDadosAsync("Serviço B", 2000);
        var task3 = BuscarDadosAsync("Serviço C", 2000);

        var resultados = await Task.WhenAll(task1, task2, task3);

        Console.WriteLine($"\n{resultados[0]}");
        Console.WriteLine(resultados[1]);
        Console.WriteLine(resultados[2]);

        sw.Stop();
        Console.WriteLine($"\n⏱️ Tempo total: {sw.ElapsedMilliseconds}ms");
    }

    private static string BuscarDadosSincrono(string servico, int delayMs)
    {
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Buscando {servico}...");
        Thread.Sleep(delayMs); // ❌ Bloqueia a thread
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] {servico} concluído.");
        return $"Dados do {servico}";
    }

    private static async Task<string> BuscarDadosAsync(string servico, int delayMs)
    {
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Buscando {servico}...");
        await Task.Delay(delayMs); // ✅ Libera a thread durante a espera
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] {servico} concluído.");
        return $"Dados do {servico}";
    }
}
