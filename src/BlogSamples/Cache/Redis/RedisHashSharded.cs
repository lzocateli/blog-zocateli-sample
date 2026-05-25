// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Fragmentação de Hash em múltiplos shards para evitar big keys.
// ==========================================================================

using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Distribui os campos de um hash Redis em múltiplos shards para evitar
/// que um único hash cresça além dos limiares de big key.
/// </summary>
public class RedisHashSharded
{
    private readonly IDatabase _db;

    /// <summary>
    /// Número de shards. Deve ser potência de 2 para distribuição uniforme via módulo.
    /// </summary>
    private const int QuantidadeShards = 16;

    public RedisHashSharded(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    /// <summary>
    /// Calcula a chave do shard correspondente ao campo informado.
    /// A distribuição é determinística: o mesmo campo sempre cai no mesmo shard.
    /// </summary>
    private static string ObterChaveShard(string chaveBase, string campo)
    {
        var indiceShard = Math.Abs(campo.GetHashCode()) % QuantidadeShards;
        return $"{chaveBase}:shard:{indiceShard}";
    }

    /// <summary>
    /// Grava um campo no shard correspondente do hash fragmentado.
    /// </summary>
    public async Task GravarAsync(string chaveBase, string campo, string valor)
    {
        var chaveShard = ObterChaveShard(chaveBase, campo);
        await _db.HashSetAsync(chaveShard, campo, valor);
    }

    /// <summary>
    /// Lê um campo específico do shard correspondente.
    /// </summary>
    public async Task<string?> LerAsync(string chaveBase, string campo)
    {
        var chaveShard = ObterChaveShard(chaveBase, campo);
        var resultado = await _db.HashGetAsync(chaveShard, campo);
        return resultado.HasValue ? resultado.ToString() : null;
    }

    /// <summary>
    /// Lê todos os campos do hash fragmentado paralelizando a leitura de todos os shards.
    /// </summary>
    public async Task<IEnumerable<HashEntry>> LerTodosAsync(string chaveBase)
    {
        // Dispara a leitura de todos os shards em paralelo
        var tarefas = Enumerable
            .Range(0, QuantidadeShards)
            .Select(i => _db.HashGetAllAsync($"{chaveBase}:shard:{i}"));

        var resultados = await Task.WhenAll(tarefas);
        return resultados.SelectMany(r => r);
    }

    /// <summary>
    /// Remove o hash fragmentado deletando todos os shards.
    /// </summary>
    public async Task RemoverAsync(string chaveBase)
    {
        var tarefas = Enumerable
            .Range(0, QuantidadeShards)
            .Select(i => _db.KeyDeleteAsync($"{chaveBase}:shard:{i}"));

        await Task.WhenAll(tarefas);
    }
}
