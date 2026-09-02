using CashFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configura a persistência e os serviços de infraestrutura.
var connectionString = builder.Configuration.GetConnectionString("CashFlow") ?? throw new InvalidOperationException("A string de conexão 'CashFlow' não foi configurada.");

builder.Services.AddInfrastructure(connectionString);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();