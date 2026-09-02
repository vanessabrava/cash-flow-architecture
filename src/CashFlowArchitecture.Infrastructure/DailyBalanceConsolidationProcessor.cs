using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.DailyBalances;
using CashFlowArchitecture.Infrastructure.Storage;

namespace CashFlowArchitecture.Infrastructure;

public sealed class DailyBalanceConsolidationProcessor(
    FileIntegrationEventStore integrationEventStore,
    IDailyBalanceStore dailyBalanceStore,
    IDailyBalanceCache dailyBalanceCache)
{
    public ConsolidationResult ProcessPendingEvents()
    {
        var processedEvents = 0;
        var skippedEvents = 0;
        var updatedBalanceDates = new HashSet<DateOnly>();

        foreach (var integrationEvent in integrationEventStore.GetAll())
        {
            var updatedBalance = dailyBalanceStore.Apply(integrationEvent);

            if (updatedBalance is not null)
            {
                dailyBalanceCache.Set(updatedBalance);
                processedEvents++;
                updatedBalanceDates.Add(integrationEvent.Data.EntryDate);
            }
            else
            {
                skippedEvents++;
            }
        }

        return new ConsolidationResult(
            processedEvents,
            skippedEvents,
            updatedBalanceDates.Count);
    }
}
