namespace CashFlow.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Configura a conexão e os recursos utilizados no RabbitMQ.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5674;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "cashflow.entries";
    public string QueueName { get; set; } = "cashflow.daily-balance";
    public string RoutingKey { get; set; } = "entry.created";
}