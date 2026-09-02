using CashFlowArchitecture.Consolidation.Api.Endpoints;
using CashFlowArchitecture.Consolidation.Api.Observability;
using CashFlowArchitecture.Consolidation.Api.Security;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Infrastructure;
using CashFlowArchitecture.Infrastructure.Caching;
using CashFlowArchitecture.Infrastructure.Persistence;
using CashFlowArchitecture.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Result", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<FileIntegrationEventStore>();

if (builder.Environment.IsEnvironment("Testing")
    || builder.Configuration.GetValue("Storage:UseFileStorage", false))
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IDailyBalanceStore, FileDailyBalanceStore>();
    builder.Services.AddSingleton<IDailyBalanceCache, DistributedDailyBalanceCache>();
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
    builder.Services.AddScoped<IDailyBalanceStore, PostgresDailyBalanceStore>();
    builder.Services.AddScoped<IDailyBalanceCache, DistributedDailyBalanceCache>();
}

builder.Services.AddScoped<DailyBalanceConsolidationProcessor>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.MapHealthEndpoints();
app.MapDailyBalanceEndpoints();

app.Run();

public partial class Program;
