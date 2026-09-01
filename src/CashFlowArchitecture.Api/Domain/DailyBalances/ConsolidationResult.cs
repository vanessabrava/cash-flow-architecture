namespace CashFlowArchitecture.Api.Domain.DailyBalances;

internal sealed record ConsolidationResult(
    int ProcessedEvents,
    int SkippedEvents,
    int UpdatedBalances);
