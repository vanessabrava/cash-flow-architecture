using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CashFlowArchitecture.Api.Tests;

public sealed class FinancialEntryEndpointsTests : IDisposable
{
    private readonly string entriesFilePath = Path.Combine(
        Path.GetTempPath(),
        $"cash-flow-entries-{Guid.NewGuid()}.json");
    private readonly string integrationEventsFilePath = Path.Combine(
        Path.GetTempPath(),
        $"cash-flow-events-{Guid.NewGuid()}.json");
    private readonly HttpClient client;

    public FinancialEntryEndpointsTests()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Storage:FinancialEntriesPath"] = entriesFilePath,
                        ["Storage:IntegrationEventsPath"] = integrationEventsFilePath
                    });
                });
            });

        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEntry_ReturnsCreatedEntryWithCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(new
            {
                type = "CREDIT",
                amount = 150.75m,
                description = "Venda no cartao",
                entryDate = "2026-09-01"
            })
        };
        request.Headers.Add("X-Correlation-Id", "test-correlation-123");

        using var response = await client.SendAsync(request);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("test-correlation-123", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("test-correlation-123", body.RootElement.GetProperty("correlationId").GetString());
        Assert.True(Guid.TryParse(body.RootElement.GetProperty("uid").GetString(), out _));
        Assert.Equal("CREDIT", body.RootElement.GetProperty("type").GetString());
        Assert.Equal(150.75m, body.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal("Venda no cartao", body.RootElement.GetProperty("description").GetString());
        Assert.Equal("2026-09-01", body.RootElement.GetProperty("entryDate").GetString());
        Assert.False(body.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateEntry_PublishesEntryCreatedEventWithCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(new
            {
                type = "CREDIT",
                amount = 150.75m,
                description = "Venda no cartao",
                entryDate = "2026-09-01"
            })
        };
        request.Headers.Add("X-Correlation-Id", "event-correlation-123");

        using var response = await client.SendAsync(request);
        using var eventsFile = File.OpenRead(integrationEventsFilePath);
        using var events = await JsonDocument.ParseAsync(eventsFile);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var integrationEvent = Assert.Single(events.RootElement.EnumerateArray());
        Assert.True(Guid.TryParse(integrationEvent.GetProperty("eventUid").GetString(), out _));
        Assert.Equal("event-correlation-123", integrationEvent.GetProperty("correlationId").GetString());
        Assert.Equal("EntryCreated", integrationEvent.GetProperty("eventType").GetString());
        Assert.True(Guid.TryParse(integrationEvent.GetProperty("data").GetProperty("entryUid").GetString(), out _));
        Assert.Equal("CREDIT", integrationEvent.GetProperty("data").GetProperty("type").GetString());
        Assert.Equal(150.75m, integrationEvent.GetProperty("data").GetProperty("amount").GetDecimal());
        Assert.Equal("2026-09-01", integrationEvent.GetProperty("data").GetProperty("entryDate").GetString());
    }

    [Fact]
    public async Task GetEntriesByDate_ReturnsCreatedEntriesForRequestedDate()
    {
        await client.PostAsJsonAsync("/entries", new
        {
            type = "DEBIT",
            amount = 40.00m,
            description = "Pagamento de fornecedor",
            entryDate = "2026-09-01"
        });

        using var response = await client.GetAsync("/entries?date=2026-09-01");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-09-01", body.RootElement.GetProperty("date").GetString());
        Assert.NotEmpty(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.False(body.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateEntry_ReturnsValidationErrorWhenAmountIsInvalid()
    {
        using var response = await client.PostAsJsonAsync("/entries", new
        {
            type = "CREDIT",
            amount = 0,
            description = "Venda no cartao",
            entryDate = "2026-09-01"
        });
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            body.RootElement.GetProperty("details").EnumerateArray(),
            detail => detail.GetProperty("field").GetString() == "amount");
    }

    [Fact]
    public async Task SwaggerDocument_IsAvailableInDevelopment()
    {
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/entries", out _));
        Assert.True(paths.TryGetProperty("/daily-balances/{date}", out _));

        Assert.True(HasStringEntryTypeSchema(body.RootElement));
    }

    [Fact]
    public async Task GetDailyBalanceByDate_ReturnsConsolidatedBalance()
    {
        await client.PostAsJsonAsync("/entries", new
        {
            type = "CREDIT",
            amount = 150.75m,
            description = "Venda no cartao",
            entryDate = "2026-09-01"
        });
        await client.PostAsJsonAsync("/entries", new
        {
            type = "DEBIT",
            amount = 40.00m,
            description = "Pagamento de fornecedor",
            entryDate = "2026-09-01"
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/daily-balances/2026-09-01");
        request.Headers.Add("X-Correlation-Id", "balance-correlation-123");

        using var response = await client.SendAsync(request);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("balance-correlation-123", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("balance-correlation-123", body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("2026-09-01", body.RootElement.GetProperty("date").GetString());
        Assert.Equal(150.75m, body.RootElement.GetProperty("totalCredits").GetDecimal());
        Assert.Equal(40.00m, body.RootElement.GetProperty("totalDebits").GetDecimal());
        Assert.Equal(110.75m, body.RootElement.GetProperty("balance").GetDecimal());
        Assert.Equal("CONSOLIDATED", body.RootElement.GetProperty("status").GetString());
        Assert.False(body.RootElement.TryGetProperty("id", out _));
    }

    public void Dispose()
    {
        client.Dispose();

        if (File.Exists(entriesFilePath))
        {
            File.Delete(entriesFilePath);
        }

        if (File.Exists(integrationEventsFilePath))
        {
            File.Delete(integrationEventsFilePath);
        }
    }

    private static bool HasStringEntryTypeSchema(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String
                && type.GetString() == "string"
                && element.TryGetProperty("enum", out var enumValues))
            {
                var values = enumValues.EnumerateArray()
                    .Select(value => value.GetString())
                    .ToArray();

                if (values.Contains("CREDIT") && values.Contains("DEBIT"))
                {
                    return true;
                }
            }

            return element.EnumerateObject()
                .Any(property => HasStringEntryTypeSchema(property.Value));
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(HasStringEntryTypeSchema);
        }

        return false;
    }
}
