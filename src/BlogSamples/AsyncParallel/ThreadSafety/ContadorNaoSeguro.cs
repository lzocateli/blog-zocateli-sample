// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Implementação DELIBERADAMENTE insegura para demonstrar race condition.
// NUNCA use este padrão em produção.
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.ThreadSafety;

/// <summary>
/// Implementação NÃO thread-safe. Existe apenas para servir como controle
/// nos testes de race condition — o teste deve conseguir provar que este
/// tipo perde incrementos quando acessado por múltiplas threads.
/// </summary>
public sealed class ContadorNaoSeguro : IContadorService
{
    private long _valor;

    public long Valor => _valor;

    public bool EhThreadSafe => false;

    public void Incrementar()
    {
        // _valor++ expande para: read _valor -> add 1 -> write _valor.
        // Duas threads podem ler o mesmo valor antes de qualquer uma gravar,
        // resultando em incrementos perdidos.
        _valor++;
    }

    public void Reiniciar() => _valor = 0;
}
