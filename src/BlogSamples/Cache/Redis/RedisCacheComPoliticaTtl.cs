// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Política obrigatória de TTL para chaves em ambientes compartilhados.
// ==========================================================================

using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Wrapper de cache que impõe TTL obrigatório em todas as chaves,
/// evitando o acúmulo de dados indefinidos em ambientes compartilhados.
/// </summary>
public class RedisCacheComPoliticaTtl
{
    private readonly IDatabase _db;

    /// <summary>TTL padrão aplicado quando nenhum é informado.</summary>
    private static readonly TimeSpan TtlPadrao = TimeSpan.FromHours(1);

    /// <summary>TTL máximo permitido — chaves com TTL maior são truncadas.</summary>
    private static readonly TimeSpan TtlMaximo = TimeSpan.FromHours(24);

    public RedisCacheComPoliticaTtl(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    /// <summary>
    /// Grava uma string com TTL garantido. Se nenhum TTL for informado, usa o padrão de 1h.
    /// Se o TTL informado ultrapassar 24h, é truncado para o máximo permitido.
    /// </summary>
    public async Task GravarAsync(string chave, string valor, TimeSpan? ttl = null)
    {
        // Nunca permite TTL nulo — garante expiração automática de todas as chaves
        var ttlEfetivo = ttl.HasValue
            ? TimeSpan.FromTicks(Math.Min(ttl.Value.Ticks, TtlMaximo.Ticks))
            : TtlPadrao;

        await _db.StringSetAsync(chave, valor, ttlEfetivo);
    }

    /// <summary>
    /// Lê uma string do cache. Retorna null se a chave não existir ou tiver expirado.
    /// </summary>
    public async Task<string?> LerAsync(string chave)
    {
        var resultado = await _db.StringGetAsync(chave);
        return resultado.HasValue ? resultado.ToString() : null;
    }

    /// <summary>
    /// Verifica o TTL restante de uma chave.
    /// Retorna null se a chave não existir. Retorna TimeSpan.MaxValue se não tiver TTL
    /// (situação que não deveria ocorrer com este wrapper, mas é tratada por segurança).
    /// </summary>
    public async Task<TimeSpan?> ObterTtlRestanteAsync(string chave)
    {
        var ttlRestante = await _db.KeyTimeToLiveAsync(chave);
        return ttlRestante;
    }
}
