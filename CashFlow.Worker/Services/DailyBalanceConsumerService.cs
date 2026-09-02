using System.Text;
using System.Text.Json;
using CashFlow.Application.Contracts.Messaging;
using CashFlow.Domain.Enums;
using CashFlow.Infrastructure.Messaging.RabbitMq;
using CashFlow.Infrastructure.Persistence;
using CashFlow.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Worker.Services;

/// <summary>
/// Consome os eventos de lançamentos e atualiza o consolidado diário.
/// </summary>
public sealed class DailyBalanceConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DailyBalanceConsumerService> _logger;

    public DailyBalanceConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<DailyBalanceConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.Span);

                var entryCreated = JsonSerializer.Deserialize<EntryCreatedEvent>(payload) ?? throw new InvalidOperationException("Não foi possível desserializar o evento de lançamento.");

                await UpdateDailyBalanceAsync(entryCreated, stoppingToken);

                // Confirma a mensagem somente após a atualização do consolidado.
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

                _logger.LogInformation("Lançamento {EntryId} consolidado com sucesso.", entryCreated.EntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do consolidado.");
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: _options.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task UpdateDailyBalanceAsync(EntryCreatedEvent entryCreated, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();

        // Uma mensagem já processada não pode alterar o consolidado novamente.
        var alreadyProcessed = await context.ProcessedMessages.AnyAsync(x => x.EntryId == entryCreated.EntryId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Lançamento {EntryId} já foi processado.", entryCreated.EntryId);
            return;
        }

        var date = DateOnly.FromDateTime(entryCreated.OccurredAt.UtcDateTime);

        var balance = await context.DailyBalances.SingleOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (balance is null)
        {
            balance = new DailyBalanceRecord(date);
            await context.DailyBalances.AddAsync(balance, cancellationToken);
        }

        if (entryCreated.Type == EntryType.Credito)
            balance.AddCredit(entryCreated.Amount);
        else
            balance.AddDebit(entryCreated.Amount);

        // Registra o lançamento como processado junto com a atualização do consolidado.
        await context.ProcessedMessages.AddAsync(new ProcessedMessage(entryCreated.EntryId), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}