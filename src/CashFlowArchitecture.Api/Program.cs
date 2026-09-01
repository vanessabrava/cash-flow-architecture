using CashFlowArchitecture.Api.Endpoints;
using CashFlowArchitecture.Api.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<InMemoryFinancialEntryStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "cash-flow-api",
    checkedAt = DateTimeOffset.UtcNow
}));

app.MapFinancialEntryEndpoints();

app.Run();
