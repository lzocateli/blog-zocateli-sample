// ==========================================================================
// Artigo: EFCore.BulkExtensions: Operações em Massa Profissionais no .NET
// URL: /posts/2026/efcore-bulkextensions-operacoes-massa-dotnet/
// Entidades dedicadas aos exemplos de operações bulk
// ==========================================================================

namespace BlogSamples.DataAccess.BulkOperations;

/// <summary>
/// Registro de aplicação no inventário (cenário enterprise).
/// </summary>
public class RegistroApp
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NomeProjeto { get; set; } = string.Empty;
    public string Repositorio { get; set; } = string.Empty;
    public string Linguagem { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string TipoAplicacao { get; set; } = string.Empty;
    public string VersaoFramework { get; set; } = string.Empty;
    public bool Desativado { get; set; }
    public bool Legado { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}

/// <summary>
/// Credencial de aplicação vinculada a um registro.
/// </summary>
public class ClienteApp
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NomeExibicao { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string NomeVault { get; set; } = string.Empty;
    public DateTimeOffset DataExpiracao { get; set; }
    public string TipoCliente { get; set; } = string.Empty;
    public string AmbienteId { get; set; } = string.Empty;
    public string RegistroAppId { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}

/// <summary>
/// Secret do Azure Key Vault vinculado a um registro.
/// </summary>
public class ConfiguracaoSegura
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string NomeVault { get; set; } = string.Empty;
    public string AmbienteId { get; set; } = string.Empty;
    public string RegistroAppId { get; set; } = string.Empty;
    public DateTimeOffset? ExpiraEm { get; set; }
    public bool Habilitado { get; set; } = true;
    public string? Versao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}

/// <summary>
/// Mensagem de evento para processamento via Azure Service Bus.
/// </summary>
public record RegistroAppMessage(
    string NomeProjeto,
    string Repositorio,
    string Linguagem,
    string Plataforma,
    string TipoAplicacao,
    string VersaoFramework,
    bool Legado);
