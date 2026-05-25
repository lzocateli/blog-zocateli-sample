// ==========================================================================
// Artigo: Redis: Por que Chaves Grandes Destroem o Desempenho Compartilhado
// URL: /posts/2026/redis-chaves-grandes-ambientes-compartilhados/
// Builder de chaves com namespacing obrigatório por ambiente e tenant.
// ==========================================================================

namespace BlogSamples.Cache.Redis;

/// <summary>
/// Constrói chaves Redis com namespacing estruturado para facilitar
/// diagnóstico, rastreabilidade e limpeza seletiva em ambientes multi-tenant.
/// </summary>
/// <remarks>
/// Padrão: {ambiente}:{tenant}:{dominio}:{entidade}:{id}
/// Exemplo: prod:acme-corp:relatorio:mensal:2026-05
/// </remarks>
public static class RedisKeyBuilder
{
    /// <summary>
    /// Constrói uma chave Redis padronizada com prefixo de ambiente e tenant.
    /// Todos os segmentos são sanitizados para evitar conflitos com padrões SCAN.
    /// </summary>
    /// <param name="tenant">Identificador do tenant (ex: "acme-corp").</param>
    /// <param name="dominio">Domínio de negócio (ex: "relatorio").</param>
    /// <param name="entidade">Tipo de entidade (ex: "mensal").</param>
    /// <param name="id">Identificador único (ex: "2026-05").</param>
    /// <param name="ambiente">Prefixo de ambiente. Padrão: "prod".</param>
    /// <returns>Chave no formato {ambiente}:{tenant}:{dominio}:{entidade}:{id}</returns>
    public static string Construir(
        string tenant,
        string dominio,
        string entidade,
        string id,
        string ambiente = "prod")
    {
        return $"{Sanitizar(ambiente)}:{Sanitizar(tenant)}:{Sanitizar(dominio)}:{Sanitizar(entidade)}:{id}";
    }

    /// <summary>
    /// Remove caracteres que dificultam padrões SCAN e normaliza para minúsculas.
    /// Espaços são convertidos em hífens; dois-pontos são removidos para não
    /// quebrar o separador de segmentos da chave.
    /// </summary>
    private static string Sanitizar(string segmento) =>
        segmento
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(":", "");
}
