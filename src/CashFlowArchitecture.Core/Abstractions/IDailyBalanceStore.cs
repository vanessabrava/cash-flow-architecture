using CashFlowArchitecture.Core.Domain.DailyBalances;
using CashFlowArchitecture.Core.Domain.Events;

namespace CashFlowArchitecture.Core.Abstractions;

public interface IDailyBalanceStore
{
    DailyBalance? GetByDate(DateOnly balanceDate);

    bool Apply(EntryCreatedEvent integrationEvent);
}
