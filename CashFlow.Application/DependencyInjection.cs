using CashFlow.Application.Entries.Create;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Application;

/// <summary>
/// Configura os serviços fornecidos pela camada de aplicação.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registra os casos de uso utilizados pela aplicação.
        services.AddScoped<CreateEntryUseCase>();

        return services;
    }
}