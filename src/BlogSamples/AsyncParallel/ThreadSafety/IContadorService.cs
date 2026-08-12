// -----------------------------------------------------------------------
// Artigo: Thread-Safety em .NET 10 vs Python: Concorrência, GIL e Race Conditions
// URL: https://zocate.li/posts/2026/thread-safety-dotnet-10-vs-python-gil-race-conditions/
// Contrato mínimo de um serviço de contador. As implementações neste
// diretório demonstram versões thread-unsafe e thread-safe do mesmo
// contrato, usadas nos testes de race condition.
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.ThreadSafety;

/// <summary>
/// Contrato de um serviço registrado tipicamente como Singleton em ASP.NET Core.
/// A propriedade <see cref="EhThreadSafe"/> serve apenas como sinalização
/// didática para os testes: em código real esse tipo de metadado não existe.
/// </summary>
public interface IContadorService
{
    /// <summary>Incrementa o contador em uma unidade.</summary>
    void Incrementar();

    /// <summary>Valor atual do contador.</summary>
    long Valor { get; }

    /// <summary>Reinicia o contador para zero.</summary>
    void Reiniciar();

    /// <summary>Sinaliza se a implementação foi projetada para acesso concorrente.</summary>
    bool EhThreadSafe { get; }
}
