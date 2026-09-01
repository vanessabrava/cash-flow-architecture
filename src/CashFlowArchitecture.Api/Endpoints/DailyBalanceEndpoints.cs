using CashFlowArchitecture.Api.Common;
using CashFlowArchitecture.Api.Contracts.DailyBalances;
using CashFlowArchitecture.Api.Domain.Entries;
using CashFlowArchitecture.Api.Infrastructure;

namespace CashFlowArchitecture.Api.Endpoints;

internal static class DailyBalanceEndpoints
{
    public static void MapDailyBalanceEndpoints(this WebApplication app)
    {
        var balances = app.MapGroup("/daily-balances");

        balances.MapGet("/{date}", GetByDate)
            .WithName("GetDailyBalanceByDate")
            .WithSummary("Consulta o saldo diário.")
            .WithDescription("Calcula o saldo diário a partir dos lançamentos financeiros registrados para a data informada.");
    }

    private static IResult GetByDate(
        DateOnly date,
        HttpContext httpContext,
        FileFinancialEntryStore store)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var entries = store.GetByDate(date);
        var totalCredits = entries
            .Where(entry => entry.Type == EntryType.CREDIT)
            .Sum(entry => entry.Amount);
        var totalDebits = entries
            .Where(entry => entry.Type == EntryType.DEBIT)
            .Sum(entry => entry.Amount);

        return Results.Ok(new DailyBalanceResponse(
            correlationId,
            date,
            totalCredits,
            totalDebits,
            totalCredits - totalDebits,
            "CONSOLIDATED",
            DateTimeOffset.UtcNow));
    }
}
