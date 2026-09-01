namespace CashFlowArchitecture.Api.Domain.Entries;

internal sealed record FinancialEntry(
    Guid Uid,
    EntryType Type,
    decimal Amount,
    string Description,
    DateOnly EntryDate,
    DateTimeOffset CreatedAt);
