namespace CashFlowArchitecture.Consolidation.Api.Contracts.DailyBalances;

internal sealed record ConsolidationProcessResponse(
    string CorrelationId,
    int ProcessedEvents,
    int SkippedEvents,
    int UpdatedBalances);
