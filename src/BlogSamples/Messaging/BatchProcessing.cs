// ==========================================================================
// Artigo: Gargalo em Banco de Dados: Mensageria e Paginação
// URL: /posts/2026/gargalo-banco-dados-efcore-mensageria-paginacao/
// ==========================================================================

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace BlogSamples.Messaging;

// ❌ Abordagem ingênua — InsertOne por vez
// foreach (var pedido in listaDe50MilPedidos)
// {
//     await context.Pedidos.AddAsync(pedido);
//     await context.SaveChangesAsync(); // PROBLEMA: 1 roundtrip por registro!
// }

/// <summary>
/// Exemplos de gravação em lote para evitar gargalo no banco de dados.
/// </summary>
public class BatchProcessingExamples(AppDbContext context)
{
    /// <summary>
    /// Batch por chunk + 1 SaveChanges por lote.
    /// Limpa ChangeTracker a cada lote para não acumular entidades em memória.
    /// </summary>
    public async Task GravarPedidosEmLoteAsync(
        IEnumerable<Pedido> pedidos,
        CancellationToken ct = default)
    {
        const int tamanhoLote = 500;

        foreach (var chunk in pedidos.Chunk(tamanhoLote))
        {
            await context.Pedidos.AddRangeAsync(chunk, ct);
            await context.SaveChangesAsync(ct);

            // Limpar o ChangeTracker para não acumular entidades em memória
            context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// BulkInsert para SQL Server e Oracle (via EFCore.BulkExtensions).
    /// </summary>
    public async Task BulkInsertPedidosAsync(
        List<Pedido> pedidos,
        CancellationToken ct = default)
    {
        var bulkConfig = new BulkConfig
        {
            BatchSize = 1000,
            UseTempDB = true,              // SQL Server: tabela temporária para staging
            SetOutputIdentity = true,      // Preenche os IDs gerados pelo banco
            PreserveInsertOrder = true
        };

        await context.BulkInsertAsync(pedidos, bulkConfig, cancellationToken: ct);
    }
}

// --- Configuração Oracle: identity no OnModelCreating ---
// protected override void OnModelCreating(ModelBuilder modelBuilder)
// {
//     modelBuilder.Entity<Pedido>(entity =>
//     {
//         entity.HasKey(e => e.Id);
//         // Oracle 12c+: identity nativo
//         entity.Property(e => e.Id).UseIdentityColumn();
//         // Oracle 11g/legado: entity.Property(e => e.Id).HasDefaultValueSql("SEQ_PEDIDOS.NEXTVAL");
//     });
// }
