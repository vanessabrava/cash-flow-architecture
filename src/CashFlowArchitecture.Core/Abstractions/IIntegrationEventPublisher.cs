using CashFlowArchitecture.Core.Domain.Events;

namespace CashFlowArchitecture.Core.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken);
}
