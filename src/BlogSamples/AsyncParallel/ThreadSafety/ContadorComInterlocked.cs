// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Interlocked é a opção mais performática para contadores simples,
// pois usa instruções atômicas do processador sem bloquear outras threads.
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.ThreadSafety;

/// <summary>
/// Contador thread-safe usando <see cref="Interlocked"/> — operação atômica
/// no nível do hardware, sem bloqueio de threads. Prefira este padrão sobre
/// <c>lock</c> quando precisar proteger uma única variável primitiva.
/// </summary>
public sealed class ContadorComInterlocked : IContadorService
{
    private long _valor;

    // Leitura de long é atômica em 64 bits, mas Interlocked.Read garante
    // atomicidade também em plataformas 32 bits sem depender do JIT.
    public long Valor => Interlocked.Read(ref _valor);

    public bool EhThreadSafe => true;

    public void Incrementar() => Interlocked.Increment(ref _valor);

    public void Reiniciar() => Interlocked.Exchange(ref _valor, 0);
}
