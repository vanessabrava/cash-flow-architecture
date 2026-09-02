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
    private readonly string idempotencyRecordsFilePath = Path.Combine(
        Path.GetTempPath(),
        $"cash-flow-idempotency-{Guid.NewGuid()}.json");
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public FinancialEntryEndpointsTests()
    {
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:ApiKey"] = "test-api-key",
                        ["Storage:UseFileStorage"] = "true",
                        ["Storage:FinancialEntriesPath"] = entriesFilePath,
                        ["Storage:IntegrationEventsPath"] = integrationEventsFilePath,
                        ["Storage:IdempotencyRecordsPath"] = idempotencyRecordsFilePath
                    });
                });
            });

        client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
    }

    [Fact]
    public async Task Health_ReturnsOkWithoutApiKey()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/health");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("cash-flow-api", body.RootElement.GetProperty("service").GetString());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Liveness_ReturnsOkWithoutApiKey()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/health/live");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Empty(body.RootElement.GetProperty("dependencies").EnumerateArray());
    }

    [Fact]
    public async Task Readiness_ReturnsLocalStorageDependencyWhenUsingFileStorage()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/health/ready");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());

        var dependency = Assert.Single(body.RootElement.GetProperty("dependencies").EnumerateArray());
        Assert.Equal("local-storage", dependency.GetProperty("name").GetString());
        Assert.Equal("Healthy", dependency.GetProperty("status").GetString());
        Assert.True(dependency.GetProperty("critical").GetBoolean());
    }

    [Fact]
    public async Task BusinessEndpoint_ReturnsUnauthorizedWithoutApiKey()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/entries?date=2026-09-01");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("AUTHENTICATION_REQUIRED", body.RootElement.GetProperty("code").GetString());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
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
    public async Task CreateEntry_ReturnsSameEntryWhenIdempotencyKeyIsReused()
    {
        var payload = new
        {
            type = "CREDIT",
            amount = 150.75m,
            description = "Venda no cartao",
            entryDate = "2026-09-01"
        };

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", "entry-create-key-123");

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(payload)
        };
        secondRequest.Headers.Add("Idempotency-Key", "entry-create-key-123");

        using var firstResponse = await client.SendAsync(firstRequest);
        using var firstBody = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using var secondResponse = await client.SendAsync(secondRequest);
        using var secondBody = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        using var entriesResponse = await client.GetAsync("/entries?date=2026-09-01");
        using var entriesBody = await JsonDocument.ParseAsync(await entriesResponse.Content.ReadAsStreamAsync());
        using var eventsFile = File.OpenRead(integrationEventsFilePath);
        using var events = await JsonDocument.ParseAsync(eventsFile);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(
            firstBody.RootElement.GetProperty("uid").GetString(),
            secondBody.RootElement.GetProperty("uid").GetString());
        Assert.Single(entriesBody.RootElement.GetProperty("items").EnumerateArray());
        Assert.Single(events.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task CreateEntry_ReturnsConflictWhenIdempotencyKeyIsReusedWithDifferentPayload()
    {
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(new
            {
                type = "CREDIT",
                amount = 150.75m,
                description = "Venda no cartao",
                entryDate = "2026-09-01"
            })
        };
        firstRequest.Headers.Add("Idempotency-Key", "entry-create-conflict-key-123");

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(new
            {
                type = "CREDIT",
                amount = 200.00m,
                description = "Venda no cartao",
                entryDate = "2026-09-01"
            })
        };
        secondRequest.Headers.Add("Idempotency-Key", "entry-create-conflict-key-123");

        using var firstResponse = await client.SendAsync(firstRequest);
        using var secondResponse = await client.SendAsync(secondRequest);
        using var secondBody = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", secondBody.RootElement.GetProperty("code").GetString());
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
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.True(paths.TryGetProperty("/health/live", out _));
        Assert.True(paths.TryGetProperty("/health/ready", out _));
        Assert.True(paths.TryGetProperty("/entries", out _));

        Assert.True(HasStringEntryTypeSchema(body.RootElement));
        Assert.True(HasApiKeySecurityScheme(body.RootElement));
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();

        if (File.Exists(entriesFilePath))
        {
            File.Delete(entriesFilePath);
        }

        if (File.Exists(integrationEventsFilePath))
        {
            File.Delete(integrationEventsFilePath);
        }

        if (File.Exists(idempotencyRecordsFilePath))
        {
            File.Delete(idempotencyRecordsFilePath);
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

    private static bool HasApiKeySecurityScheme(JsonElement document)
    {
        return document.TryGetProperty("components", out var components)
            && components.TryGetProperty("securitySchemes", out var securitySchemes)
            && securitySchemes.TryGetProperty("ApiKey", out var apiKey)
            && apiKey.GetProperty("type").GetString() == "apiKey"
            && apiKey.GetProperty("name").GetString() == "X-Api-Key"
            && apiKey.GetProperty("in").GetString() == "header";
    }
}
