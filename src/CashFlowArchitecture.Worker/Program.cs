using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Infrastructure.Caching;
using CashFlowArchitecture.Infrastructure.Persistence;
using CashFlowArchitecture.Worker.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.AddDbContext<CashFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "cash-flow:";
});
builder.Services.AddScoped<IDailyBalanceStore, PostgresDailyBalanceStore>();
builder.Services.AddScoped<IDailyBalanceCache, DistributedDailyBalanceCache>();
builder.Services.AddHostedService<RabbitMqDailyBalanceWorker>();

await builder.Build().RunAsync();
