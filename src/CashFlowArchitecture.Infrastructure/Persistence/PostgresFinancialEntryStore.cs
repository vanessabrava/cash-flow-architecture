using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Entries;

namespace CashFlowArchitecture.Infrastructure.Persistence;

public sealed class PostgresFinancialEntryStore(CashFlowDbContext dbContext) : IFinancialEntryStore
{
    public void Add(FinancialEntry entry)
    {
        dbContext.FinancialEntries.Add(new FinancialEntryEntity
        {
            Uid = entry.Uid,
            Type = entry.Type,
            Amount = entry.Amount,
            Description = entry.Description,
            EntryDate = entry.EntryDate,
            CreatedAt = entry.CreatedAt
        });

        dbContext.SaveChanges();
    }

    public IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate)
    {
        return dbContext.FinancialEntries
            .Where(entry => entry.EntryDate == entryDate)
            .OrderBy(entry => entry.CreatedAt)
            .Select(entry => new FinancialEntry(
                entry.Uid,
                entry.Type,
                entry.Amount,
                entry.Description,
                entry.EntryDate,
                entry.CreatedAt))
            .ToArray();
    }

    public FinancialEntry? GetByUid(Guid uid)
    {
        return dbContext.FinancialEntries
            .Where(entry => entry.Uid == uid)
            .Select(entry => new FinancialEntry(
                entry.Uid,
                entry.Type,
                entry.Amount,
                entry.Description,
                entry.EntryDate,
                entry.CreatedAt))
            .SingleOrDefault();
    }
}
