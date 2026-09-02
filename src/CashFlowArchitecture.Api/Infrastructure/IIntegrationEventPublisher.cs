using CashFlowArchitecture.Api.Domain.Events;

namespace CashFlowArchitecture.Api.Infrastructure;

internal interface IIntegrationEventPublisher
{
    Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken);
}
