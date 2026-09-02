using CashFlowArchitecture.Api.Domain.DailyBalances;
using CashFlowArchitecture.Api.Domain.Events;

namespace CashFlowArchitecture.Api.Infrastructure;

internal interface IDailyBalanceStore
{
    DailyBalance? GetByDate(DateOnly balanceDate);

    bool Apply(EntryCreatedEvent integrationEvent);
}
