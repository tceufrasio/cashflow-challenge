using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento de persistência dos lançamentos.
/// </summary>
public sealed class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Descricao)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DataOcorrencia)
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        // Favorece consultas e consolidações por data de ocorrência.
        builder.HasIndex(x => x.DataOcorrencia);
    }
}