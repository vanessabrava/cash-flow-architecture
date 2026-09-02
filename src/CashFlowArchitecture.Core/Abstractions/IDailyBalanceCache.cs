using CashFlowArchitecture.Core.Domain.DailyBalances;

namespace CashFlowArchitecture.Core.Abstractions;

public interface IDailyBalanceCache
{
    DailyBalance? GetByDate(DateOnly balanceDate);

    void Set(DailyBalance balance);
}
