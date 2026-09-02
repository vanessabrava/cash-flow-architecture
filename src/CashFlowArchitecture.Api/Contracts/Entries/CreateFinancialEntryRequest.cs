using CashFlowArchitecture.Core.Domain.Entries;

namespace CashFlowArchitecture.Api.Contracts.Entries;

internal sealed record CreateFinancialEntryRequest(
    EntryType Type,
    decimal Amount,
    string Description,
    DateOnly EntryDate);
