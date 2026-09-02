namespace CashFlowArchitecture.Core.Domain.DailyBalances;

public sealed record DailyBalance(
    Guid Uid,
    DateOnly BalanceDate,
    decimal TotalCredits,
    decimal TotalDebits,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<Guid> ProcessedEventUids)
{
    public decimal Balance => TotalCredits - TotalDebits;
}
