using System.Text;
using CashFlow.Infrastructure.Messaging.RabbitMq;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CashFlow.Worker.Services;

/// <summary>
/// Publica no RabbitMQ as mensagens pendentes da Outbox.
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<OutboxPublisherService> logger)
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

        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(_options.QueueName, _options.ExchangeName, _options.RoutingKey, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishPendingAsync(channel, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();

        var messages = await context.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            var body = Encoding.UTF8.GetBytes(message.Payload);

            await channel.BasicPublishAsync(exchange: _options.ExchangeName, routingKey: _options.RoutingKey, body: body, cancellationToken: cancellationToken);

            message.MarkAsProcessed();

            _logger.LogInformation("Mensagem {MessageId} publicada no RabbitMQ.", message.Id);
        }

        if (messages.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}