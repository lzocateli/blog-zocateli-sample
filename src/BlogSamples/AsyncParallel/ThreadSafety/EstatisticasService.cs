// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Cenário realista: serviço registrado como Singleton que agrega estatísticas
// por chave. Usar Dictionary<TKey,TValue> aqui seria bug latente — a solução
// correta é ConcurrentDictionary com operações atômicas de agregação.
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace BlogSamples.AsyncParallel.ThreadSafety;

/// <summary>
/// Serviço de agregação por chave que pode ser registrado com segurança como
/// <c>AddSingleton</c> em ASP.NET Core. Usa <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// com <see cref="ConcurrentDictionary{TKey, TValue}.AddOrUpdate(TKey, System.Func{TKey, TValue}, System.Func{TKey, TValue, TValue})"/>
/// para garantir invariantes sob concorrência sem <c>lock</c> manual.
/// </summary>
public sealed class EstatisticasService
{
    private readonly ConcurrentDictionary<string, long> _contagemPorChave = new();

    /// <summary>
    /// Incrementa a contagem da chave informada de forma atômica.
    /// </summary>
    /// <param name="chave">Identificador do bucket de agregação.</param>
    /// <param name="incremento">Valor a somar (padrão 1).</param>
    public void Registrar(string chave, long incremento = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);

        _contagemPorChave.AddOrUpdate(
            chave,
            addValueFactory: _ => incremento,
            updateValueFactory: (_, valorAtual) => valorAtual + incremento);
    }

    /// <summary>Total agregado para uma chave específica. Retorna zero se ausente.</summary>
    public long Obter(string chave)
        => _contagemPorChave.TryGetValue(chave, out var valor) ? valor : 0;

    /// <summary>
    /// Devolve um snapshot imutável do estado. A cópia é essencial: expor o
    /// dicionário interno permitiria ao chamador enumerar durante uma
    /// escrita concorrente, o que gera exceções intermitentes.
    /// </summary>
    public IReadOnlyDictionary<string, long> Snapshot()
        => _contagemPorChave.ToArray().ToDictionary(kv => kv.Key, kv => kv.Value);

    /// <summary>Total somado entre todas as chaves.</summary>
    public long Total()
    {
        long soma = 0;
        foreach (var valor in _contagemPorChave.Values)
        {
            soma += valor;
        }

        return soma;
    }

    public void Limpar() => _contagemPorChave.Clear();
}
