// ==========================================================================
// Artigo: EFCore.BulkExtensions: Operações em Massa Profissionais no .NET
// URL: /posts/2026/efcore-bulkextensions-operacoes-massa-dotnet/
// DbContext dedicado aos exemplos de operações bulk
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.BulkOperations;

/// <summary>
/// DbContext para demonstração de operações bulk com EFCore.BulkExtensions.
/// </summary>
public class BulkDbContext(DbContextOptions<BulkDbContext> options) : DbContext(options)
{
    public DbSet<RegistroApp> RegistrosApps => Set<RegistroApp>();
    public DbSet<ClienteApp> ClientesApps => Set<ClienteApp>();
    public DbSet<ConfiguracaoSegura> ConfiguracoesSeguras => Set<ConfiguracaoSegura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroApp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomeProjeto).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Repositorio).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Linguagem).HasMaxLength(20);
            entity.Property(e => e.Plataforma).HasMaxLength(20);
            entity.Property(e => e.TipoAplicacao).HasMaxLength(20);
            entity.Property(e => e.VersaoFramework).HasMaxLength(20);

            entity.HasIndex(e => new { e.NomeProjeto, e.Repositorio }).IsUnique();
        });

        modelBuilder.Entity<ClienteApp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomeExibicao).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ClientId).HasMaxLength(36);
            entity.Property(e => e.NomeVault).HasMaxLength(45);
            entity.Property(e => e.TipoCliente).HasMaxLength(20);

            entity.HasIndex(e => new { e.NomeVault, e.RegistroAppId }).IsUnique();
        });

        modelBuilder.Entity<ConfiguracaoSegura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(127).IsRequired();
            entity.Property(e => e.Valor).IsRequired();
            entity.Property(e => e.NomeVault).HasMaxLength(24).IsRequired();

            entity.HasIndex(e => new { e.Nome, e.NomeVault, e.RegistroAppId }).IsUnique();
        });
    }
}
