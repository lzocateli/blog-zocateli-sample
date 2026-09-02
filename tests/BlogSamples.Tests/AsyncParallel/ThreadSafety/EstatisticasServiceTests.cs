// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Valida que EstatisticasService mantém invariantes agregadas sob
// concorrência realista (cenário típico de Singleton em ASP.NET Core).
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using BlogSamples.AsyncParallel.ThreadSafety;

namespace BlogSamples.Tests.AsyncParallel.ThreadSafety;

public sealed class EstatisticasServiceTests
{
    [Fact]
    public async Task Registrar_Concorrente_MantemContagemCorretaPorChave()
    {
        const int Threads = 32;
        const int Iteracoes = 25_000;
        var chaves = new[] { "leitura", "escrita", "erro", "auth" };

        var servico = new EstatisticasService();
        var barreira = new Barrier(Threads);
        var tarefas = new Task[Threads];

        for (var i = 0; i < Threads; i++)
        {
            var indiceThread = i;
            tarefas[i] = Task.Factory.StartNew(
                () =>
                {
                    barreira.SignalAndWait();
                    for (var j = 0; j < Iteracoes; j++)
                    {
                        servico.Registrar(chaves[(indiceThread + j) % chaves.Length]);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        await Task.WhenAll(tarefas);

        var total = (long)Threads * Iteracoes;
        Assert.Equal(total, servico.Total());
        Assert.Equal(total, servico.Snapshot().Values.Sum());
    }

    [Fact]
    public void Snapshot_DevolveCopiaImutavel_NaoAfetaEstadoInterno()
    {
        var servico = new EstatisticasService();
        servico.Registrar("a", 3);
        servico.Registrar("b", 5);

        var snapshot = servico.Snapshot();
        Assert.Equal(3, snapshot["a"]);
        Assert.Equal(5, snapshot["b"]);

        // Mutar via reflexão da referência devolvida não deve afetar o interno.
        Assert.IsNotType<ConcurrentDictionary<string, long>>(snapshot);

        servico.Registrar("a", 1);
        Assert.Equal(3, snapshot["a"]); // snapshot congelado
        Assert.Equal(4, servico.Obter("a"));
    }

    [Fact]
    public void Registrar_ChaveInvalida_LancaExcecao()
    {
        var servico = new EstatisticasService();
        Assert.Throws<ArgumentNullException>(() => servico.Registrar(null!));
        Assert.Throws<ArgumentException>(() => servico.Registrar(string.Empty));
        Assert.Throws<ArgumentException>(() => servico.Registrar("   "));
    }
}
