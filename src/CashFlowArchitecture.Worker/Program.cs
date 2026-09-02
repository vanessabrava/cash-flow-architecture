using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Infrastructure.Persistence;
using CashFlowArchitecture.Worker.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<CashFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<IDailyBalanceStore, PostgresDailyBalanceStore>();
builder.Services.AddHostedService<RabbitMqDailyBalanceWorker>();

await builder.Build().RunAsync();
