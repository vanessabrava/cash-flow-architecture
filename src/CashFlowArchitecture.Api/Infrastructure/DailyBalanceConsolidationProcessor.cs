using CashFlowArchitecture.Api.Domain.DailyBalances;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class DailyBalanceConsolidationProcessor(
    FileIntegrationEventStore integrationEventStore,
    IDailyBalanceStore dailyBalanceStore)
{
    public ConsolidationResult ProcessPendingEvents()
    {
        var processedEvents = 0;
        var skippedEvents = 0;
        var updatedBalanceDates = new HashSet<DateOnly>();

        foreach (var integrationEvent in integrationEventStore.GetAll())
        {
            if (dailyBalanceStore.Apply(integrationEvent))
            {
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
