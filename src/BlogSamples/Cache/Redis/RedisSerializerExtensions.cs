// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Extension methods para serialização comprimida com GZip no Redis.
// ==========================================================================

using System.IO.Compression;
using System.Text.Json;
using StackExchange.Redis;

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Extension methods para <see cref="IDatabase"/> que serializam e comprimem
/// objetos com GZip antes de gravar no Redis, reduzindo o tamanho das chaves.
/// </summary>
public static class RedisSerializerExtensions
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializa o objeto para JSON, comprime com GZip e grava no Redis.
    /// Redução típica: 60–80% em relação ao JSON sem compressão.
    /// </summary>
    public static async Task GravarComprimidoAsync<T>(
        this IDatabase db,
        string chave,
        T valor,
        TimeSpan? expiracao = null)
    {
        using var fluxoSaida = new MemoryStream();
        await using (var gzip = new GZipStream(fluxoSaida, CompressionLevel.Optimal))
        {
            await JsonSerializer.SerializeAsync(gzip, valor, _opcoesJson);
        }

        var bytesComprimidos = fluxoSaida.ToArray();
        await db.StringSetAsync(chave, bytesComprimidos, expiracao);
    }

    /// <summary>
    /// Lê os bytes do Redis, descomprime com GZip e desserializa para o tipo T.
    /// </summary>
    public static async Task<T?> LerComprimidoAsync<T>(
        this IDatabase db,
        string chave)
    {
        var valor = await db.StringGetAsync(chave);
        if (!valor.HasValue) return default;

        var bytesComprimidos = (byte[])valor!;
        using var fluxoEntrada = new MemoryStream(bytesComprimidos);
        await using var gzip = new GZipStream(fluxoEntrada, CompressionMode.Decompress);

        return await JsonSerializer.DeserializeAsync<T>(gzip, _opcoesJson);
    }
}
