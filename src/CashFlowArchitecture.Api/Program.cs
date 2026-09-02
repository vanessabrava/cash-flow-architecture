using CashFlowArchitecture.Api.Endpoints;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Infrastructure;
using CashFlowArchitecture.Infrastructure.Messaging;
using CashFlowArchitecture.Infrastructure.Persistence;
using CashFlowArchitecture.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<FileIntegrationEventStore>();

if (builder.Environment.IsEnvironment("Testing")
    || builder.Configuration.GetValue("Storage:UseFileStorage", false))
{
    builder.Services.AddSingleton<IFinancialEntryStore, FileFinancialEntryStore>();
    builder.Services.AddSingleton<IDailyBalanceStore, FileDailyBalanceStore>();
    builder.Services.AddSingleton<IIdempotencyStore, FileIdempotencyStore>();
    builder.Services.AddSingleton<IIntegrationEventPublisher>(provider =>
        provider.GetRequiredService<FileIntegrationEventStore>());
}
else
{
    builder.Services.AddDbContext<CashFlowDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
    builder.Services.AddScoped<IFinancialEntryStore, PostgresFinancialEntryStore>();
    builder.Services.AddScoped<IDailyBalanceStore, PostgresDailyBalanceStore>();
    builder.Services.AddScoped<IIdempotencyStore, PostgresIdempotencyStore>();
    builder.Services.AddSingleton<RabbitMqIntegrationEventPublisher>();
    builder.Services.AddSingleton<IIntegrationEventPublisher, FileAndRabbitMqIntegrationEventPublisher>();
}

builder.Services.AddScoped<DailyBalanceConsolidationProcessor>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.MapType<EntryType>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Enum =
        [
            JsonValue.Create(nameof(EntryType.CREDIT)),
            JsonValue.Create(nameof(EntryType.DEBIT))
        ]
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "cash-flow-api",
    checkedAt = DateTimeOffset.UtcNow
}))
.WithName("GetHealth")
.WithSummary("Consulta a saúde da API.")
.WithDescription("Retorna o estado básico de disponibilidade da aplicação.");

app.MapFinancialEntryEndpoints();
app.MapDailyBalanceEndpoints();

app.Run();

public partial class Program;
