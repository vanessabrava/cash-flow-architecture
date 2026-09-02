using System.Net;
using System.Text.Json;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CashFlowArchitecture.Consolidation.Api.Tests;

public sealed class DailyBalanceEndpointsTests : IDisposable
{
    private readonly string integrationEventsFilePath = Path.Combine(
        Path.GetTempPath(),
        $"cash-flow-consolidation-events-{Guid.NewGuid()}.json");
    private readonly string dailyBalancesFilePath = Path.Combine(
        Path.GetTempPath(),
        $"cash-flow-consolidation-balances-{Guid.NewGuid()}.json");
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public DailyBalanceEndpointsTests()
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
                        ["Storage:IntegrationEventsPath"] = integrationEventsFilePath,
                        ["Storage:DailyBalancesPath"] = dailyBalancesFilePath
                    });
                });
            });

        client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
    }

    [Fact]
    public async Task Health_ReturnsConsolidationApiServiceName()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/health");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("cash-flow-consolidation-api", body.RootElement.GetProperty("service").GetString());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task BusinessEndpoint_ReturnsUnauthorizedWithoutApiKey()
    {
        using var unauthenticatedClient = factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/daily-balances/2026-09-01");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("AUTHENTICATION_REQUIRED", body.RootElement.GetProperty("code").GetString());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task SwaggerDocument_OnlyExposesConsolidationEndpoints()
    {
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.True(paths.TryGetProperty("/health/live", out _));
        Assert.True(paths.TryGetProperty("/health/ready", out _));
        Assert.True(paths.TryGetProperty("/daily-balances/{date}", out _));
        Assert.True(paths.TryGetProperty("/daily-balances/process-events", out _));
        Assert.False(paths.TryGetProperty("/entries", out _));
        Assert.True(HasApiKeySecurityScheme(body.RootElement));
    }

    [Fact]
    public async Task GetDailyBalanceByDate_ReturnsPendingWhenEventsWereNotProcessed()
    {
        using var response = await client.GetAsync("/daily-balances/2026-09-01");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("2026-09-01", body.RootElement.GetProperty("date").GetString());
        Assert.Equal("PENDING", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetDailyBalanceByDate_ReturnsConsolidatedBalance()
    {
        AddEntryCreatedEvent(EntryType.CREDIT, 150.75m);
        AddEntryCreatedEvent(EntryType.DEBIT, 40.00m);

        using var processRequest = new HttpRequestMessage(HttpMethod.Post, "/daily-balances/process-events");
        processRequest.Headers.Add("X-Correlation-Id", "process-correlation-123");

        using var processResponse = await client.SendAsync(processRequest);
        using var processBody = await JsonDocument.ParseAsync(await processResponse.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, processResponse.StatusCode);
        Assert.Equal("process-correlation-123", processBody.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(2, processBody.RootElement.GetProperty("processedEvents").GetInt32());
        Assert.Equal(0, processBody.RootElement.GetProperty("skippedEvents").GetInt32());
        Assert.Equal(1, processBody.RootElement.GetProperty("updatedBalances").GetInt32());

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

    [Fact]
    public async Task ProcessEvents_IsIdempotent()
    {
        AddEntryCreatedEvent(EntryType.CREDIT, 150.75m);

        using var firstProcessResponse = await client.PostAsync("/daily-balances/process-events", null);
        using var secondProcessResponse = await client.PostAsync("/daily-balances/process-events", null);
        using var secondProcessBody = await JsonDocument.ParseAsync(await secondProcessResponse.Content.ReadAsStreamAsync());
        using var balanceResponse = await client.GetAsync("/daily-balances/2026-09-01");
        using var balanceBody = await JsonDocument.ParseAsync(await balanceResponse.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, firstProcessResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondProcessResponse.StatusCode);
        Assert.Equal(0, secondProcessBody.RootElement.GetProperty("processedEvents").GetInt32());
        Assert.Equal(1, secondProcessBody.RootElement.GetProperty("skippedEvents").GetInt32());
        Assert.Equal(150.75m, balanceBody.RootElement.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task GetDailyBalanceByDate_ReturnsCachedConsolidatedBalance()
    {
        AddEntryCreatedEvent(EntryType.CREDIT, 150.75m);
        await client.PostAsync("/daily-balances/process-events", null);

        using var firstResponse = await client.GetAsync("/daily-balances/2026-09-01");
        using var firstBody = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());

        await File.WriteAllTextAsync(dailyBalancesFilePath, "[]");

        using var secondResponse = await client.GetAsync("/daily-balances/2026-09-01");
        using var secondBody = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(150.75m, firstBody.RootElement.GetProperty("balance").GetDecimal());
        Assert.Equal(150.75m, secondBody.RootElement.GetProperty("balance").GetDecimal());
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();

        if (File.Exists(integrationEventsFilePath))
        {
            File.Delete(integrationEventsFilePath);
        }

        if (File.Exists(dailyBalancesFilePath))
        {
            File.Delete(dailyBalancesFilePath);
        }
    }

    private void AddEntryCreatedEvent(EntryType type, decimal amount)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<FileIntegrationEventStore>();

        store.Add(new EntryCreatedEvent(
            Guid.NewGuid(),
            "event-correlation-123",
            EntryCreatedEvent.Type,
            DateTimeOffset.UtcNow,
            new EntryCreatedEventData(
                Guid.NewGuid(),
                type,
                amount,
                new DateOnly(2026, 9, 1))));
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
