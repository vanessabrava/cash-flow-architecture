using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Infrastructure.Messaging;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class FileAndRabbitMqIntegrationEventPublisher(
    FileIntegrationEventStore fileIntegrationEventStore,
    RabbitMqIntegrationEventPublisher rabbitMqIntegrationEventPublisher) : IIntegrationEventPublisher
{
    public async Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        fileIntegrationEventStore.Add(integrationEvent);
        await rabbitMqIntegrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
