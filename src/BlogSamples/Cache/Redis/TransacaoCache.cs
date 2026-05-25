// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// ✅ SOLUÇÃO: cache paginado de transações com TTL obrigatório.
// ==========================================================================

using System.Text.Json;
using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// ✅ Cache paginado de transações: distribui os dados em múltiplas chaves
/// de ~100 registros cada (~30 KB por chave) com TTL obrigatório de 30 minutos.
/// Elimina a big key de 80+ MB do <see cref="TransacaoCacheProblematico"/>.
/// </summary>
public class TransacaoCache
{
    private readonly IDatabase _redis;

    private const int TamanhoPagina = 100;
    private const string PrefixoChave = "tx";
    private static readonly TimeSpan TtlPagina = TimeSpan.FromMinutes(30);

    public TransacaoCache(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    /// <summary>
    /// Grava uma página de transações como chave individual com TTL.
    /// Cada chave contém no máximo <see cref="TamanhoPagina"/> registros (~30 KB).
    /// </summary>
    /// <param name="clienteId">ID do cliente.</param>
    /// <param name="mes">Mês de referência.</param>
    /// <param name="ano">Ano de referência.</param>
    /// <param name="pagina">Número da página (base 0).</param>
    /// <param name="transacoes">Lista de transações da página.</param>
    public async Task GravarPaginaAsync(
        int clienteId,
        int mes,
        int ano,
        int pagina,
        IEnumerable<Transacao> transacoes)
    {
        var chave = ConstruirChavePagina(clienteId, mes, ano, pagina);
        var json = JsonSerializer.Serialize(transacoes.Take(TamanhoPagina));

        // TTL de 30 minutos — dado de relatório não precisa ficar eternamente no cache
        await _redis.StringSetAsync(chave, json, TtlPagina);
    }

    /// <summary>
    /// Recupera uma página específica do cache. Retorna null em caso de cache miss.
    /// </summary>
    public async Task<List<Transacao>?> ObterPaginaAsync(
        int clienteId,
        int mes,
        int ano,
        int pagina)
    {
        var chave = ConstruirChavePagina(clienteId, mes, ano, pagina);
        var dadosCache = await _redis.StringGetAsync(chave);

        return dadosCache.HasValue
            ? JsonSerializer.Deserialize<List<Transacao>>((string)dadosCache!)
            : null;
    }

    /// <summary>
    /// Invalida todas as páginas de um cliente/mês/ano deletando as chaves.
    /// Útil quando os dados são atualizados e o cache precisa ser limpo.
    /// </summary>
    public async Task InvalidarAsync(int clienteId, int mes, int ano, int totalPaginas)
    {
        var tarefas = Enumerable
            .Range(0, totalPaginas)
            .Select(pagina => _redis.KeyDeleteAsync(ConstruirChavePagina(clienteId, mes, ano, pagina)));

        await Task.WhenAll(tarefas);
    }

    /// <summary>
    /// Constrói a chave de uma página seguindo o padrão de namespacing do artigo.
    /// Formato: tx:{clienteId}:{mes}-{ano}:p{pagina}
    /// </summary>
    private static string ConstruirChavePagina(int clienteId, int mes, int ano, int pagina) =>
        $"{PrefixoChave}:{clienteId}:{mes}-{ano}:p{pagina}";
}
