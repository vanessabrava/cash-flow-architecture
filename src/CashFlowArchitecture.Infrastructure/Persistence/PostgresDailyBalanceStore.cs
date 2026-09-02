using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.DailyBalances;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Core.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace CashFlowArchitecture.Infrastructure.Persistence;

public sealed class PostgresDailyBalanceStore(CashFlowDbContext dbContext) : IDailyBalanceStore
{
    public DailyBalance? GetByDate(DateOnly balanceDate)
    {
        return dbContext.DailyBalances
            .Include(balance => balance.ProcessedEvents)
            .SingleOrDefault(balance => balance.BalanceDate == balanceDate)
            ?.ToDomain();
    }

    public bool Apply(EntryCreatedEvent integrationEvent)
    {
        var currentBalance = dbContext.DailyBalances
            .Include(balance => balance.ProcessedEvents)
            .SingleOrDefault(balance => balance.BalanceDate == integrationEvent.Data.EntryDate);

        if (currentBalance?.ProcessedEvents.Any(processedEvent =>
            processedEvent.EventUid == integrationEvent.EventUid) == true)
        {
            return false;
        }

        currentBalance ??= new DailyBalanceEntity
        {
            Uid = Guid.NewGuid(),
            BalanceDate = integrationEvent.Data.EntryDate,
            Status = "CONSOLIDATED"
        };

        if (integrationEvent.Data.Type == EntryType.CREDIT)
        {
            currentBalance.TotalCredits += integrationEvent.Data.Amount;
        }
        else
        {
            currentBalance.TotalDebits += integrationEvent.Data.Amount;
        }

        currentBalance.UpdatedAt = DateTimeOffset.UtcNow;
        currentBalance.ProcessedEvents.Add(new DailyBalanceProcessedEventEntity
        {
            EventUid = integrationEvent.EventUid
        });

        if (currentBalance.Id == 0)
        {
            dbContext.DailyBalances.Add(currentBalance);
        }

        dbContext.SaveChanges();

        return true;
    }
}

public static class DailyBalanceEntityExtensions
{
    public static DailyBalance ToDomain(this DailyBalanceEntity balance)
    {
        return new DailyBalance(
            balance.Uid,
            balance.BalanceDate,
            balance.TotalCredits,
            balance.TotalDebits,
            balance.Status,
            balance.UpdatedAt,
            balance.ProcessedEvents.Select(processedEvent => processedEvent.EventUid).ToArray());
    }
}
