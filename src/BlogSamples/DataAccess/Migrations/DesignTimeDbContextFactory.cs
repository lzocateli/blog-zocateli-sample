// ==========================================================================
// Artigo: EF Core Migrations: Multi-Projeto, Secrets e Scaffolding
// URL: /posts/2026/efcore-migrations-multi-projeto-secrets-scaffolding/
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlogSamples.DataAccess.Migrations;

/// <summary>
/// Factory usada APENAS pelo EF Core CLI (dotnet ef migrations add/update).
/// Em runtime, o DbContext é resolvido pela DI configurada no IoC.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Messaging.AppDbContext>
{
    public Messaging.AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não encontrada. " +
                "Configure via 'dotnet user-secrets set' ou variável de ambiente.");

        var optionsBuilder = new DbContextOptionsBuilder<Messaging.AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(
                typeof(DesignTimeDbContextFactory).Assembly.FullName);

            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        return new Messaging.AppDbContext(optionsBuilder.Options);
    }
}

// --- CLI Commands ---
// dotnet user-secrets init
// dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=blog;..."
// dotnet ef migrations add InitialCreate
// dotnet ef database update
// dotnet ef migrations script --idempotent -o migration.sql
