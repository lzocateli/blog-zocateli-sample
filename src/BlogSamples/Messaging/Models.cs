// ==========================================================================
// Modelos compartilhados entre domínios Messaging, DataAccess e Workers.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.Messaging;

/// <summary>
/// Entidade Pedido usada nos exemplos de mensageria e processamento em lote.
/// </summary>
public class Pedido
{
    public Guid Id { get; set; }
    public string ClienteId { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataCriacao { get; set; }
    public string Status { get; set; } = "Pendente";
}

/// <summary>
/// DbContext exemplo para os domínios de Messaging, DataAccess e Workers.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
}
