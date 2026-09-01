using CashFlowArchitecture.Api.Endpoints;
using CashFlowArchitecture.Api.Domain.Entries;
using CashFlowArchitecture.Api.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<FileFinancialEntryStore>();
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

if (app.Environment.IsDevelopment())
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

app.Run();

public partial class Program;
