using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Contracts.Entries;

internal sealed record FinancialEntryItemResponse(
    Guid Uid,
    EntryType Type,
    decimal Amount,
    string Description,
    DateOnly EntryDate,
    DateTimeOffset CreatedAt);
