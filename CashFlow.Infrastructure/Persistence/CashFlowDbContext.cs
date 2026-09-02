using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistência da aplicação.
/// </summary>
public sealed class CashFlowDbContext : DbContext
{
    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Entry> Entries => Set<Entry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica as configurações de persistência definidas neste assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashFlowDbContext).Assembly);
    }
}