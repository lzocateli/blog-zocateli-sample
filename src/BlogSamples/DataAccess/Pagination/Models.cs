// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Models reutilizáveis para paginação
// ==========================================================================

using System.Text.Json;

namespace BlogSamples.DataAccess.Pagination;

// --- Models ---

public record PaginacaoRequest(
    int Pagina        = 1,
    int TamanhoPagina = 20)
{
    public int TamanhoSeguro => Math.Clamp(TamanhoPagina, 1, 100);
    public int OffsetSeguro  => (Math.Max(Pagina, 1) - 1) * TamanhoSeguro;
}

public record PaginaResultado<T>(
    IReadOnlyList<T> Dados,
    int              PaginaAtual,
    int              TamanhoPagina,
    long             TotalRegistros,
    int              TotalPaginas,
    bool             TemProxima,
    bool             TemAnterior);

/// <summary>
/// Token de cursor — serializado em Base64 para o cliente (opaco).
/// </summary>
public record KeysetCursor(Guid UltimoId, DateTime UltimaData)
{
    public string Encode() =>
        Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(this));

    public static KeysetCursor? Decode(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try
        {
            return JsonSerializer.Deserialize<KeysetCursor>(
                Convert.FromBase64String(token));
        }
        catch { return null; }
    }
}

public record KeysetResultado<T>(
    IReadOnlyList<T> Dados,
    bool             TemProximaPagina,
    string?          ProximoToken);

public record TimePaginacaoRequest(
    DateTime  DataInicio,
    DateTime  DataFim,
    DateTime? UltimaDataVista = null,
    Guid?     UltimoIdVisto   = null,
    int       Limite          = 50);

// --- HATEOAS ---

public record PaginacaoHateoasResultado<T>(
    IReadOnlyList<T> Dados,
    PaginacaoLinks   Links,
    PaginacaoMeta    Meta);

public record PaginacaoLinks(
    string? Primeiro,
    string? Anterior,
    string? Proximo,
    string? Ultimo);

public record PaginacaoMeta(
    int  Limite,
    long TotalRegistros,
    bool TemProxima);

// --- DTO ---

public record PedidoDto(Guid Id, string ClienteId, decimal Valor, DateTime DataCriacao, string Status);
