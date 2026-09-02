using CashFlow.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura a persistência do consolidado diário.
/// </summary>
public sealed class DailyBalanceRecordConfiguration : IEntityTypeConfiguration<DailyBalanceRecord>
{
    public void Configure(EntityTypeBuilder<DailyBalanceRecord> builder)
    {
        builder.ToTable("daily_balances");

        builder.HasKey(x => x.Date);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.Property(x => x.TotalCredits)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalDebits)
            .HasPrecision(18, 2);
    }
}