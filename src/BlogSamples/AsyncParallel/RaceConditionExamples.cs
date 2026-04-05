// -----------------------------------------------------------------------
// Artigo: Programação Assíncrona em C#: async/await do Fundamento à Produção
// Race conditions: exemplos de código thread-unsafe vs thread-safe
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace BlogSamples.AsyncParallel;

public static class RaceConditionExamples
{
    /// <summary>
    /// ❌ PROBLEMA: Race condition — múltiplas continuações acessam a mesma variável.
    /// Resultado: valor final pode ser menor que 1000.
    /// </summary>
    public static async Task ExemploSemProtecaoAsync()
    {
        var contador = 0;

        async Task IncrementarSemProtecaoAsync()
        {
            await Task.Yield();
            contador++; // ❌ Não é uma operação atômica
        }

        var tasks = Enumerable.Range(0, 1000)
            .Select(_ => IncrementarSemProtecaoAsync());

        await Task.WhenAll(tasks);
        Console.WriteLine($"Sem proteção — Esperado: 1000, Obtido: {contador}");
    }

    /// <summary>
    /// ✅ CORRETO: Acesso protegido com lock.
    /// Apenas uma thread por vez executa o bloco.
    /// </summary>
    public static async Task ExemploComLockAsync()
    {
        var contador = 0;
        var lockObj = new object();

        async Task IncrementarComLockAsync()
        {
            await Task.Yield();
            lock (lockObj)
            {
                contador++;
            }
        }

        var tasks = Enumerable.Range(0, 1000)
            .Select(_ => IncrementarComLockAsync());

        await Task.WhenAll(tasks);
        Console.WriteLine($"Com lock — Esperado: 1000, Obtido: {contador}");
    }

    /// <summary>
    /// ✅ CORRETO e mais performático: Interlocked para operações atômicas.
    /// </summary>
    public static async Task ExemploComInterlockedAsync()
    {
        var contador = 0;

        async Task IncrementarAtomicoAsync()
        {
            await Task.Yield();
            Interlocked.Increment(ref contador);
        }

        var tasks = Enumerable.Range(0, 1000)
            .Select(_ => IncrementarAtomicoAsync());

        await Task.WhenAll(tasks);
        Console.WriteLine($"Interlocked — Esperado: 1000, Obtido: {contador}");
    }

    /// <summary>
    /// ConcurrentDictionary com AddOrUpdate para operações atômicas.
    /// (Do artigo de Paralelismo em C#)
    /// </summary>
    public static void ExemploConcurrentDictionary()
    {
        var contadores = new ConcurrentDictionary<string, int>();

        Parallel.For(0, 1000, _ =>
        {
            contadores.AddOrUpdate("total", 1, (_, atual) => atual + 1);
        });

        Console.WriteLine($"ConcurrentDictionary — Esperado: 1000, Obtido: {contadores["total"]}");
    }
}
