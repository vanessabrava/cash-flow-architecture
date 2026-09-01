namespace CashFlowArchitecture.Api.Contracts.DailyBalances;

internal sealed record DailyBalanceResponse(
    string CorrelationId,
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance,
    string Status,
    DateTimeOffset UpdatedAt);
