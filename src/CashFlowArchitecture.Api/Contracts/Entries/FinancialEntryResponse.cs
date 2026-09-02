using CashFlowArchitecture.Core.Domain.Entries;

namespace CashFlowArchitecture.Api.Contracts.Entries;

internal sealed record FinancialEntryResponse(
    string CorrelationId,
    Guid Uid,
    EntryType Type,
    decimal Amount,
    string Description,
    DateOnly EntryDate,
    DateTimeOffset CreatedAt);
