using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class InMemoryFinancialEntryStore
{
    private readonly List<FinancialEntry> entries = [];
    private readonly Lock syncRoot = new();

    public void Add(FinancialEntry entry)
    {
        lock (syncRoot)
        {
            entries.Add(entry);
        }
    }

    public IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate)
    {
        lock (syncRoot)
        {
            return entries
                .Where(entry => entry.EntryDate == entryDate)
                .OrderBy(entry => entry.CreatedAt)
                .ToArray();
        }
    }
}
