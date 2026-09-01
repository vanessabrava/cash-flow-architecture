namespace CashFlowArchitecture.Api.Contracts.DailyBalances;

internal sealed record PendingDailyBalanceResponse(
    string CorrelationId,
    DateOnly Date,
    string Status,
    string Message);
