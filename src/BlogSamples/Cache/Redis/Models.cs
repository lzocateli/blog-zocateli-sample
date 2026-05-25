// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Modelos de domínio usados nos exemplos de cache Redis.
// ==========================================================================

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Representa uma transação financeira usada nos exemplos de cache paginado.
/// </summary>
public record Transacao(
    int Id,
    int ClienteId,
    decimal Valor,
    string Descricao,
    DateTimeOffset Data);
