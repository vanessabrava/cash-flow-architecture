namespace CashFlowArchitecture.Core.Domain.Entries;

public sealed record FinancialEntry(
    Guid Uid,
    EntryType Type,
    decimal Amount,
    string Description,
    DateOnly EntryDate,
    DateTimeOffset CreatedAt);
