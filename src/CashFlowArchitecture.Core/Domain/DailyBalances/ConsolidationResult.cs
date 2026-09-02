namespace CashFlowArchitecture.Core.Domain.DailyBalances;

public sealed record ConsolidationResult(
    int ProcessedEvents,
    int SkippedEvents,
    int UpdatedBalances);
