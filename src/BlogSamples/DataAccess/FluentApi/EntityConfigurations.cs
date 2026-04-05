// ==========================================================================
// Artigo: EF Core 8 Fluent API: Mapeamento e Desacoplamento
// URL: /posts/2026/efcore-fluent-api-mapeamento-desacoplamento/
// Configurações Fluent API — IEntityTypeConfiguration<T>
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogSamples.DataAccess.FluentApi;

// --- Produto ---

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Sku)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Preco)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Ativo)
            .HasDefaultValue(true);

        builder.Property(p => p.CriadoEm)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        // N:N — Produto ↔ Tag
        builder.HasMany(p => p.Tags)
            .WithMany(t => t.Produtos)
            .UsingEntity<Dictionary<string, object>>(
                "produto_tags",
                right => right.HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey("TagId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Produto>()
                    .WithMany()
                    .HasForeignKey("ProdutoId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("ProdutoId", "TagId");
                    join.ToTable("produto_tags");
                });

        // Filtro global — queries NUNCA retornam produtos inativos
        builder.HasQueryFilter(p => p.Ativo);

        // Owned type como JSON
        builder.OwnsOne(p => p.Dimensoes, dim =>
        {
            dim.ToJson();
        });

        // Coleções primitivas como JSON
        builder.Property(p => p.ImagensUrls)
            .HasColumnType("jsonb");

        builder.Property(p => p.PalavrasChave)
            .HasColumnType("jsonb");
    }
}

// --- Categoria ---

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Descricao)
            .HasMaxLength(500);

        builder.Property(c => c.Ativa)
            .HasDefaultValue(true);

        builder.Property(c => c.CriadoEm)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(c => c.Nome)
            .IsUnique();

        // 1:N — Uma Categoria tem muitos Produtos
        builder.HasMany(c => c.Produtos)
            .WithOne(p => p.Categoria)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// --- ProdutoDetalhe ---

public class ProdutoDetalheConfiguration : IEntityTypeConfiguration<ProdutoDetalhe>
{
    public void Configure(EntityTypeBuilder<ProdutoDetalhe> builder)
    {
        builder.ToTable("produto_detalhes");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DescricaoCompleta)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(d => d.Especificacoes)
            .HasColumnType("text");

        builder.Property(d => d.PesoKg)
            .HasColumnType("double precision");

        // 1:1 — Um ProdutoDetalhe pertence a exatamente um Produto
        builder.HasOne(d => d.Produto)
            .WithOne(p => p.Detalhe)
            .HasForeignKey<ProdutoDetalhe>(d => d.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.ProdutoId)
            .IsUnique();
    }
}

// --- Comentário ---

public class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
{
    public void Configure(EntityTypeBuilder<Comentario> builder)
    {
        builder.ToTable("comentarios");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Texto)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasDefaultValueSql("NOW()");

        // 1:N com Produto
        builder.HasOne(c => c.Produto)
            .WithMany()
            .HasForeignKey(c => c.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Auto-referenciamento
        builder.HasOne(c => c.ComentarioPai)
            .WithMany(c => c.Respostas)
            .HasForeignKey(c => c.ComentarioPaiId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(c => c.ProdutoId);
        builder.HasIndex(c => c.ComentarioPaiId);
    }
}

// --- Pagamento (TPH) ---

public class PagamentoTphConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("pagamentos");
        builder.HasKey(p => p.Id);

        // TPH — Discriminator define o tipo concreto
        builder.HasDiscriminator<string>("tipo_pagamento")
            .HasValue<PagamentoCartao>("cartao")
            .HasValue<PagamentoPix>("pix")
            .HasValue<PagamentoBoleto>("boleto");

        builder.Property(p => p.Valor)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PagoEm)
            .HasDefaultValueSql("NOW()");
    }
}

// --- Estratégias alternativas de herança ---
// TPT: builder.Entity<PagamentoCartao>().ToTable("pagamentos_cartao");
// TPC: builder.UseTpcMappingStrategy();

// --- Value Converter: enum para string ---
// builder.Property(p => p.Status)
//     .HasConversion<string>()
//     .HasMaxLength(20);

// --- DbContext com ApplyConfigurationsFromAssembly ---
// protected override void OnModelCreating(ModelBuilder modelBuilder)
// {
//     modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
// }
