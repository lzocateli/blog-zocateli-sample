// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// ❌ PADRÃO PROBLEMÁTICO: cache de transações sem paginação e sem TTL.
//    Este arquivo é didático — demonstra o que NÃO fazer.
// ==========================================================================

using System.Text.Json;
using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// ❌ ANTI-PADRÃO: cache que grava toda a lista de transações em uma única chave
/// sem TTL. Pode gerar chaves de 80+ MB que bloqueiam o event loop do Redis.
/// Ver <see cref="TransacaoCache"/> para a implementação correta.
/// </summary>
public class TransacaoCacheProblematico
{
    private readonly IDatabase _db;

    public TransacaoCacheProblematico(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    /// <summary>
    /// ❌ Carrega e cacheia 300.000 transações em uma única chave sem TTL.
    /// Problemas: chave de ~80 MB, bloqueio do event loop por centenas de ms,
    /// acúmulo indefinido de dados em memória compartilhada.
    /// </summary>
    public async Task<List<Transacao>> ObterTransacoesMensaisAsync(
        int clienteId, int mes, int ano)
    {
        var chaveCache = $"transacoes:{clienteId}:{mes}/{ano}";

        var dadosCache = await _db.StringGetAsync(chaveCache);
        if (dadosCache.HasValue)
        {
            // Desserializa ~300.000 objetos de uma vez — alto consumo de CPU/memória
            return JsonSerializer.Deserialize<List<Transacao>>((string)dadosCache!)!;
        }

        // Simula busca de 300.000 transações do banco
        var transacoes = GerarTransacoesSimuladas(clienteId, mes, ano);

        // ❌ Serializa ~300.000 objetos para JSON (~80 MB) e grava SEM TTL
        await _db.StringSetAsync(chaveCache, JsonSerializer.Serialize(transacoes));

        return transacoes;
    }

    private static List<Transacao> GerarTransacoesSimuladas(int clienteId, int mes, int ano)
    {
        var inicio = new DateTimeOffset(ano, mes, 1, 0, 0, 0, TimeSpan.FromHours(-3));
        return Enumerable
            .Range(1, 300_000)
            .Select(i => new Transacao(i, clienteId, i * 0.01m, $"Transação {i}", inicio.AddSeconds(i)))
            .ToList();
    }
}
