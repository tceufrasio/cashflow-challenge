using CashFlow.Api.ExceptionHandling;
using CashFlow.Application;
using CashFlow.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Obtém a conexão utilizada pela infraestrutura.
var connectionString = builder.Configuration.GetConnectionString("CashFlow")
    ?? throw new InvalidOperationException(
        "A string de conexão 'CashFlow' não foi configurada.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Padroniza o tratamento de erros da API.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();

app.Run();