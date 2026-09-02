using CashFlow.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura a persistência das mensagens processadas.
/// </summary>
public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        // O EntryId também impede o processamento duplicado. 
        builder.HasKey(x => x.EntryId);

        builder.Property(x => x.EntryId)
            .ValueGeneratedNever();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();
    }
}