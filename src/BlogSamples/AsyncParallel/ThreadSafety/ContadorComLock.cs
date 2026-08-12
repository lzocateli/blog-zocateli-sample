// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Uso do tipo dedicado System.Threading.Lock (.NET 9+) — recomendado no
// .NET 10 no lugar do padrão antigo "lock (new object())".
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.ThreadSafety;

/// <summary>
/// Contador thread-safe usando <see cref="System.Threading.Lock"/> como
/// primitiva dedicada. O compilador reconhece o tipo e emite código
/// mais eficiente do que o padrão baseado em <c>object</c>.
/// </summary>
public sealed class ContadorComLock : IContadorService
{
    private readonly Lock _sincronizacao = new();
    private long _valor;

    public long Valor
    {
        get
        {
            lock (_sincronizacao)
            {
                return _valor;
            }
        }
    }

    public bool EhThreadSafe => true;

    public void Incrementar()
    {
        lock (_sincronizacao)
        {
            _valor++;
        }
    }

    public void Reiniciar()
    {
        lock (_sincronizacao)
        {
            _valor = 0;
        }
    }
}
