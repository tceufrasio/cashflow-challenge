using CashFlow.Infrastructure;
using CashFlow.Infrastructure.Messaging.RabbitMq;
using CashFlow.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CashFlow") ?? throw new InvalidOperationException("A string de conexão 'CashFlow' não foi configurada.");

builder.Services.AddInfrastructure(connectionString);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHostedService<DailyBalanceConsumerService>();

var host = builder.Build();

host.Run();