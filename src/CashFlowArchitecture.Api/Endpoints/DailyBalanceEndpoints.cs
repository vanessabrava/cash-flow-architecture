using CashFlowArchitecture.Api.Common;
using CashFlowArchitecture.Api.Contracts.DailyBalances;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Infrastructure;

namespace CashFlowArchitecture.Api.Endpoints;

internal static class DailyBalanceEndpoints
{
    public static void MapDailyBalanceEndpoints(this WebApplication app)
    {
        var balances = app.MapGroup("/daily-balances");

        balances.MapGet("/{date}", GetByDate)
            .WithName("GetDailyBalanceByDate")
            .WithSummary("Consulta o saldo diário.")
            .WithDescription("Retorna o saldo diário consolidado para a data informada.");

        balances.MapPost("/process-events", ProcessEvents)
            .WithName("ProcessDailyBalanceEvents")
            .WithSummary("Processa eventos de consolidação.")
            .WithDescription("Processa eventos locais EntryCreated e atualiza a visão de saldo diário consolidado.");
    }

    private static IResult GetByDate(
        DateOnly date,
        HttpContext httpContext,
        IDailyBalanceStore store,
        IDailyBalanceCache cache)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var cachedBalance = cache.GetByDate(date);

        if (cachedBalance is not null)
        {
            return Results.Ok(new DailyBalanceResponse(
                correlationId,
                cachedBalance.BalanceDate,
                cachedBalance.TotalCredits,
                cachedBalance.TotalDebits,
                cachedBalance.Balance,
                cachedBalance.Status,
                cachedBalance.UpdatedAt));
        }

        var balance = store.GetByDate(date);

        if (balance is null)
        {
            return Results.Accepted(
                $"/daily-balances/{date:yyyy-MM-dd}",
                new PendingDailyBalanceResponse(
                    correlationId,
                    date,
                    "PENDING",
                "Saldo diario ainda nao consolidado."));
        }

        cache.Set(balance);

        return Results.Ok(new DailyBalanceResponse(
            correlationId,
            date,
            balance.TotalCredits,
            balance.TotalDebits,
            balance.Balance,
            balance.Status,
            balance.UpdatedAt));
    }

    private static IResult ProcessEvents(
        HttpContext httpContext,
        DailyBalanceConsolidationProcessor processor)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var result = processor.ProcessPendingEvents();

        return Results.Ok(new ConsolidationProcessResponse(
            correlationId,
            result.ProcessedEvents,
            result.SkippedEvents,
            result.UpdatedBalances));
    }
}
