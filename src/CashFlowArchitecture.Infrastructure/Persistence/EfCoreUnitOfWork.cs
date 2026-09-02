using CashFlowArchitecture.Core.Abstractions;

namespace CashFlowArchitecture.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWork(CashFlowDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
