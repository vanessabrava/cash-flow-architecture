using CashFlowArchitecture.Core.Domain.Entries;

namespace CashFlowArchitecture.Core.Abstractions;

public interface IFinancialEntryStore
{
    void Add(FinancialEntry entry);

    FinancialEntry? GetByUid(Guid uid);

    IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate);
}
