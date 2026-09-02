using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Infrastructure;

internal interface IFinancialEntryStore
{
    void Add(FinancialEntry entry);

    IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate);
}
