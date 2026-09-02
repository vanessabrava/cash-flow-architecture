using CashFlowArchitecture.Api.Domain.Events;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class FileAndRabbitMqIntegrationEventPublisher(
    FileIntegrationEventStore fileIntegrationEventStore,
    RabbitMqIntegrationEventPublisher rabbitMqIntegrationEventPublisher) : IIntegrationEventPublisher
{
    public async Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        fileIntegrationEventStore.Add(integrationEvent);
        await rabbitMqIntegrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
