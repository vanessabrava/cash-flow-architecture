using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CashFlowArchitecture.Api.Tests;

public sealed class FinancialEntryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public FinancialEntryEndpointsTests(WebApplicationFactory<Program> factory)
    {
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
}
