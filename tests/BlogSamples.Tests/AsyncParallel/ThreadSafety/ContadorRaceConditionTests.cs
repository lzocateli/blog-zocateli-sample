// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Teste padrão N x K para provar (ou refutar) thread-safety de um serviço.
// -----------------------------------------------------------------------

using BlogSamples.AsyncParallel.ThreadSafety;

namespace BlogSamples.Tests.AsyncParallel.ThreadSafety;

public sealed class ContadorRaceConditionTests
{
    private const int Threads = 32;
    private const int IncrementosPorThread = 100_000;
    private const long TotalEsperado = (long)Threads * IncrementosPorThread;

    [Fact]
    public void ContadorComLock_MantemInvariante_SobConcorrencia()
    {
        var contador = new ContadorComLock();

        ExecutarIncrementosEmParalelo(contador);

        Assert.True(contador.EhThreadSafe);
        Assert.Equal(TotalEsperado, contador.Valor);
    }

    [Fact]
    public void ContadorComInterlocked_MantemInvariante_SobConcorrencia()
    {
        var contador = new ContadorComInterlocked();

        ExecutarIncrementosEmParalelo(contador);

        Assert.True(contador.EhThreadSafe);
        Assert.Equal(TotalEsperado, contador.Valor);
    }

    /// <summary>
    /// Demonstra que a versão não protegida perde incrementos sob concorrência.
    /// Como race conditions são probabilísticas, o teste executa várias rodadas
    /// e considera sucesso se ao menos uma delas produzir valor diferente do
    /// esperado. Com 32 threads e 100k incrementos, a probabilidade de todas
    /// as rodadas coincidirem com o valor esperado é desprezível — se isso
    /// acontecer, o cenário de estresse precisa ser reforçado.
    /// </summary>
    [Fact]
    public void ContadorNaoSeguro_DemonstraRaceCondition()
    {
        const int Rodadas = 5;
        var perdasObservadas = 0;

        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var contador = new ContadorNaoSeguro();
            ExecutarIncrementosEmParalelo(contador);

            if (contador.Valor < TotalEsperado)
            {
                perdasObservadas++;
            }
        }

        Assert.False(new ContadorNaoSeguro().EhThreadSafe);
        Assert.True(
            perdasObservadas > 0,
            $"Esperava detectar race condition em ao menos uma das {Rodadas} rodadas.");
    }

    private static void ExecutarIncrementosEmParalelo(IContadorService contador)
    {
        contador.Reiniciar();

        var barreira = new Barrier(Threads);
        var tarefas = new Task[Threads];

        for (var i = 0; i < Threads; i++)
        {
            tarefas[i] = Task.Run(() =>
            {
                // Sincroniza a largada para maximizar a sobreposição das threads.
                barreira.SignalAndWait();

                for (var j = 0; j < IncrementosPorThread; j++)
                {
                    contador.Incrementar();
                }
            });
        }

        Task.WaitAll(tarefas);
    }
}
