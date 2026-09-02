using CashFlowArchitecture.Api.Endpoints;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Infrastructure;
using CashFlowArchitecture.Infrastructure.Caching;
using CashFlowArchitecture.Infrastructure.Messaging;
using CashFlowArchitecture.Infrastructure.Persistence;
using CashFlowArchitecture.Infrastructure.Storage;
using CashFlowArchitecture.Api.Security;
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
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

if (builder.Environment.IsEnvironment("Testing")
    || builder.Configuration.GetValue("Storage:UseFileStorage", false))
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IFinancialEntryStore, FileFinancialEntryStore>();
    builder.Services.AddSingleton<IDailyBalanceStore, FileDailyBalanceStore>();
    builder.Services.AddSingleton<IDailyBalanceCache, DistributedDailyBalanceCache>();
    builder.Services.AddSingleton<IIdempotencyStore, FileIdempotencyStore>();
    builder.Services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
    builder.Services.AddSingleton<IIntegrationEventPublisher>(provider =>
        provider.GetRequiredService<FileIntegrationEventStore>());
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "cash-flow:";
    });
    builder.Services.AddDbContext<CashFlowDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
    builder.Services.AddScoped<IFinancialEntryStore, PostgresFinancialEntryStore>();
    builder.Services.AddScoped<IDailyBalanceStore, PostgresDailyBalanceStore>();
    builder.Services.AddScoped<IDailyBalanceCache, DistributedDailyBalanceCache>();
    builder.Services.AddScoped<IIdempotencyStore, PostgresIdempotencyStore>();
    builder.Services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
    builder.Services.AddSingleton<RabbitMqIntegrationEventPublisher>();
    builder.Services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
    builder.Services.AddHostedService<OutboxMessagePublisher>();
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
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyAuthenticationMiddleware.HeaderName,
        Description = "Informe a API Key local para acessar os endpoints protegidos."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", document, null),
            []
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.MapHealthEndpoints();
app.MapFinancialEntryEndpoints();
app.MapDailyBalanceEndpoints();

app.Run();

public partial class Program;
