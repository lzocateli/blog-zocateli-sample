// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Varredura programática de big keys usando SCAN + MEMORY USAGE.
// ==========================================================================

using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Varre o keyspace do Redis usando SCAN (não bloqueante) e MEMORY USAGE
/// para identificar chaves com tamanho acima do limiar configurado.
/// </summary>
public class RedisBigKeyScanner
{
    private readonly IDatabase _db;
    private readonly IServer _server;
    private readonly ILogger<RedisBigKeyScanner> _logger;

    /// <summary>Limiar em bytes para considerar uma chave "grande" (padrão: 10 MB).</summary>
    private const long LimiarBigKeyBytes = 10 * 1024 * 1024;

    public RedisBigKeyScanner(
        IConnectionMultiplexer redis,
        ILogger<RedisBigKeyScanner> logger)
    {
        _db = redis.GetDatabase();
        // Obtém o server para usar o comando SCAN via KeysAsync
        _server = redis.GetServer(redis.GetEndPoints().First());
        _logger = logger;
    }

    /// <summary>
    /// Varre o keyspace em busca de big keys usando SCAN iterativo (não bloqueante).
    /// </summary>
    /// <param name="padrao">Padrão de chave (ex: "relatorio:*"). Padrão: "*".</param>
    /// <param name="tamanhoPagina">Quantidade de chaves por iteração SCAN.</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task VarrerBigKeysAsync(
        string padrao = "*",
        int tamanhoPagina = 100,
        CancellationToken ct = default)
    {
        var bigKeys = new List<(string Chave, long TamanhoBytes, string Tipo)>();

        // KeysAsync usa SCAN internamente — itera em lotes (tamanhoPagina por vez)
        // sem bloquear o Redis entre as iterações.
        await foreach (var chave in _server
            .KeysAsync(pattern: padrao, pageSize: tamanhoPagina)
            .WithCancellation(ct))
        {
            // MEMORY USAGE retorna o tamanho em bytes incluindo overhead interno
            var resultado = await _db.ExecuteAsync("MEMORY", "USAGE", chave, "SAMPLES", "5");

            if (resultado.IsNull) continue;

            var tamanho = (long)resultado;
            if (tamanho >= LimiarBigKeyBytes)
            {
                var tipo = await _db.KeyTypeAsync(chave);
                bigKeys.Add((chave.ToString(), tamanho, tipo.ToString()));

                _logger.LogWarning(
                    "Big key detectada: {Chave} | Tipo: {Tipo} | Tamanho: {TamanhoMB:F2} MB",
                    chave, tipo, tamanho / (1024.0 * 1024.0));
            }
        }

        _logger.LogInformation(
            "Varredura concluída. {Quantidade} big keys encontradas.", bigKeys.Count);

        foreach (var (nomeChave, bytes, tipo) in bigKeys.OrderByDescending(k => k.TamanhoBytes))
        {
            _logger.LogWarning(
                "  → {Chave} ({Tipo}): {TamanhoMB:F2} MB",
                nomeChave, tipo, bytes / (1024.0 * 1024.0));
        }
    }
}
