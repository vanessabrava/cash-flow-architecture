using CashFlowArchitecture.Core.Abstractions;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
