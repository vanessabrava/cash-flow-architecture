namespace CashFlowArchitecture.Api.Contracts.Entries;

internal sealed record FinancialEntriesByDateResponse(
    string CorrelationId,
    DateOnly Date,
    IReadOnlyCollection<FinancialEntryItemResponse> Items);
