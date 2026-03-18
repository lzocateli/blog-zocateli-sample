// ==========================================================================
// Artigo: EFCore.BulkExtensions: Operações em Massa Profissionais no .NET
// URL: /posts/2026/efcore-bulkextensions-operacoes-massa-dotnet/
// Repositório com TODOS os métodos de EFCore.BulkExtensions demonstrados
// ==========================================================================

using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;

namespace BlogSamples.DataAccess.BulkOperations;

/// <summary>
/// Repositório que demonstra todos os métodos de EFCore.BulkExtensions
/// em cenários reais de operações em massa.
/// </summary>
public class RegistroAppRepository(
    BulkDbContext dbContext,
    ILogger<RegistroAppRepository> logger)
{
    // =====================================================================
    // 1. BulkInsertAsync — INSERT em massa
    // =====================================================================

    /// <summary>
    /// Insere uma lista de registros de aplicações em massa.
    /// Usa BulkCopy nativo do SQL Server (ou equivalente do provider)
    /// para inserir milhares de registros em uma única operação.
    /// </summary>
    public async Task<int> InserirEmMassaAsync(
        List<RegistroApp> registros,
        CancellationToken ct = default)
    {
        if (registros.Count == 0)
            return 0;

        var bulkConfig = new BulkConfig
        {
            BatchSize = 1000,
            SetOutputIdentity = true,
            PreserveInsertOrder = true
        };

        await dbContext.BulkInsertAsync(registros, bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkInsert concluído: {Count} registros inseridos", registros.Count);
        return registros.Count;
    }

    // =====================================================================
    // 2. BulkUpdateAsync — UPDATE em massa
    // =====================================================================

    /// <summary>
    /// Atualiza uma lista de registros existentes em massa.
    /// Ideal para atualizações de status, flags ou campos calculados
    /// em grandes volumes de dados.
    /// </summary>
    public async Task<int> AtualizarEmMassaAsync(
        List<RegistroApp> registros,
        CancellationToken ct = default)
    {
        if (registros.Count == 0)
            return 0;

        var bulkConfig = new BulkConfig
        {
            BatchSize = 1000,
            // Atualiza apenas os campos específicos, preservando os demais
            PropertiesToIncludeOnUpdate =
            [
                nameof(RegistroApp.Linguagem),
                nameof(RegistroApp.VersaoFramework),
                nameof(RegistroApp.Legado),
                nameof(RegistroApp.Desativado),
                nameof(RegistroApp.AtualizadoEm)
            ],
            TrackingEntities = false
        };

        await dbContext.BulkUpdateAsync(registros, bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkUpdate concluído: {Count} registros atualizados", registros.Count);
        return registros.Count;
    }

    // =====================================================================
    // 3. BulkDeleteAsync — DELETE em massa
    // =====================================================================

    /// <summary>
    /// Remove uma lista de registros em massa.
    /// Executa DELETE por chave primária — muito mais eficiente
    /// do que remover um a um via ChangeTracker.
    /// </summary>
    public async Task<int> RemoverEmMassaAsync(
        List<RegistroApp> registros,
        CancellationToken ct = default)
    {
        if (registros.Count == 0)
            return 0;

        await dbContext.BulkDeleteAsync(registros, cancellationToken: ct);

        logger.LogInformation("BulkDelete concluído: {Count} registros removidos", registros.Count);
        return registros.Count;
    }

    // =====================================================================
    // 4. BulkInsertOrUpdateAsync — UPSERT (MERGE)
    // =====================================================================

    /// <summary>
    /// Executa UPSERT (INSERT ou UPDATE) em massa usando SQL Server MERGE.
    /// Identifica registros existentes pela chave composta (NomeProjeto, Repositorio)
    /// e decide automaticamente entre inserir ou atualizar.
    /// </summary>
    /// <remarks>
    /// <para><b>BulkConfig detalhado:</b></para>
    /// <list type="bullet">
    /// <item><term>SetOutputIdentity</term>
    /// <description>Retorna os IDs gerados para registros inseridos.</description></item>
    /// <item><term>PreserveInsertOrder</term>
    /// <description>Mantém a ordem original da lista durante o INSERT.</description></item>
    /// <item><term>UpdateByProperties</term>
    /// <description>Chave composta usada para identificar duplicatas no MERGE.</description></item>
    /// <item><term>PropertiesToExcludeOnUpdate</term>
    /// <description>Protege o Id e CriadoEm de serem sobrescritos no UPDATE.</description></item>
    /// <item><term>TrackingEntities = false</term>
    /// <description>Desabilita ChangeTracker para performance em massa.</description></item>
    /// <item><term>BatchSize = 1000</term>
    /// <description>Processa até 1000 registros por roundtrip ao banco.</description></item>
    /// </list>
    /// </remarks>
    public async Task<int> InserirOuAtualizarAsync(
        List<RegistroApp> registros,
        IReadOnlyCollection<string> propriedadesExcluirNoUpdate,
        CancellationToken ct = default)
    {
        if (registros.Count == 0)
            return 0;

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            PreserveInsertOrder = true,
            UpdateByProperties =
            [
                nameof(RegistroApp.NomeProjeto),
                nameof(RegistroApp.Repositorio)
            ],
            PropertiesToExcludeOnUpdate = [.. propriedadesExcluirNoUpdate],
            SqlBulkCopyOptions = EFCore.BulkExtensions.SqlBulkCopyOptions.Default,
            TrackingEntities = false,
            BatchSize = 1000
        };

        await dbContext.BulkInsertOrUpdateAsync(registros, bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkInsertOrUpdate concluído: {Count} registros processados", registros.Count);
        return registros.Count;
    }

    // =====================================================================
    // 5. BulkInsertOrUpdateOrDeleteAsync — Sincronização completa (delta)
    // =====================================================================

    /// <summary>
    /// Sincroniza a tabela com a lista fornecida: insere novos, atualiza existentes
    /// e remove os que não estão mais na lista.
    /// Ideal para sincronização total com fonte externa (CSV, API, etc.).
    /// </summary>
    public async Task<int> SincronizarAsync(
        List<RegistroApp> registrosAtuais,
        CancellationToken ct = default)
    {
        if (registrosAtuais.Count == 0)
            return 0;

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            UpdateByProperties =
            [
                nameof(RegistroApp.NomeProjeto),
                nameof(RegistroApp.Repositorio)
            ],
            PropertiesToExcludeOnUpdate =
            [
                nameof(RegistroApp.Id),
                nameof(RegistroApp.CriadoEm)
            ],
            TrackingEntities = false,
            BatchSize = 1000
        };

        await dbContext.BulkInsertOrUpdateOrDeleteAsync(
            registrosAtuais, bulkConfig, cancellationToken: ct);

        logger.LogInformation(
            "BulkInsertOrUpdateOrDelete concluído: {Count} registros sincronizados",
            registrosAtuais.Count);

        return registrosAtuais.Count;
    }

    // =====================================================================
    // 6. BulkSaveChangesAsync — Substituto otimizado do SaveChangesAsync
    // =====================================================================

    /// <summary>
    /// Substitui o SaveChangesAsync padrão por uma versão otimizada
    /// que usa operações bulk internamente.
    /// </summary>
    public async Task<int> SalvarAlteracoesBulkAsync(CancellationToken ct = default)
    {
        // BulkSaveChanges analisa o ChangeTracker e aplica operações bulk
        // para cada tipo de operação (Add, Update, Delete) automaticamente
        var bulkConfig = new BulkConfig
        {
            BatchSize = 500,
            TrackingEntities = false
        };

        await dbContext.BulkSaveChangesAsync(bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkSaveChanges concluído");
        return 0;
    }

    // =====================================================================
    // 7. BulkReadAsync — Leitura em lote por chave
    // =====================================================================

    /// <summary>
    /// Lê registros em massa por chave primária ou chave composta.
    /// Útil para carregar entidades existentes antes de um update em lote,
    /// evitando N+1 queries.
    /// </summary>
    public async Task<List<RegistroApp>> LerEmMassaAsync(
        List<RegistroApp> registrosComChave,
        CancellationToken ct = default)
    {
        if (registrosComChave.Count == 0)
            return [];

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties =
            [
                nameof(RegistroApp.NomeProjeto),
                nameof(RegistroApp.Repositorio)
            ],
            TrackingEntities = false
        };

        // BulkRead preenche os registros com os dados do banco
        // usando as propriedades definidas em UpdateByProperties como chave de busca
        await dbContext.BulkReadAsync(registrosComChave, bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkRead concluído: {Count} registros carregados", registrosComChave.Count);
        return registrosComChave;
    }

    // =====================================================================
    // 8. TruncateAsync — TRUNCATE TABLE
    // =====================================================================

    /// <summary>
    /// Executa TRUNCATE TABLE para limpar a tabela completamente.
    /// Muito mais rápido que DELETE sem WHERE — não gera log de transação
    /// por registro e redefine identity/sequence.
    /// Use com cuidado — operação irreversível e não filtrada.
    /// </summary>
    public async Task TruncarTabelaAsync(CancellationToken ct = default)
    {
        await dbContext.TruncateAsync<RegistroApp>(cancellationToken: ct);

        logger.LogInformation("Truncate concluído: tabela RegistroApp limpa");
    }
}
