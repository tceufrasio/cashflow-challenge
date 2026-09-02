using System.Text;
using CashFlow.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Worker.Services;

/// <summary>
/// Consome os eventos de lançamentos publicados para o consolidado diário.
/// </summary>
public sealed class DailyBalanceConsumerService : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DailyBalanceConsumerService> _logger;

    public DailyBalanceConsumerService(IOptions<RabbitMqOptions> options, ILogger<DailyBalanceConsumerService> logger)
    {
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
                _logger.LogInformation("Mensagem recebida para consolidação: {Payload}", payload);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do consolidado.");
                                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: _options.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Mantém o serviço ativo enquanto aguarda novas mensagens.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}