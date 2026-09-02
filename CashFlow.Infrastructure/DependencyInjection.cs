using CashFlow.Application.Contracts.Persistence;
using CashFlow.Infrastructure.Persistence;
using CashFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Infrastructure;

/// <summary>
/// Configura os serviços fornecidos pela camada de infraestrutura.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Configura o EF Core para utilizar MySQL como mecanismo de persistência.
        services.AddDbContext<CashFlowDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddScoped<IEntryRepository, EntryRepository>();
        return services;
    }
}