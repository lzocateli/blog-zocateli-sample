// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Extensions e helpers para paginação (HATEOAS, Link headers)
// ==========================================================================

using Microsoft.AspNetCore.Http;

namespace BlogSamples.DataAccess.Pagination;

public static class PaginacaoExtensions
{
    /// <summary>
    /// Adiciona Link header HTTP padrão RFC 5988.
    /// </summary>
    public static void AdicionarLinkHeader<T>(
        this HttpContext ctx,
        KeysetResultado<T> resultado,
        string baseUrl)
    {
        if (resultado.ProximoToken != null)
            ctx.Response.Headers.Append(
                "Link",
                $"<{baseUrl}?cursor={resultado.ProximoToken}>; rel=\"next\"");
    }
}
