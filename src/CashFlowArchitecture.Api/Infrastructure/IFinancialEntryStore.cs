using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Infrastructure;

internal interface IFinancialEntryStore
{
    void Add(FinancialEntry entry);

    FinancialEntry? GetByUid(Guid uid);

    IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate);
}
