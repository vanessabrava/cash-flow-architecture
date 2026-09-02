using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Infrastructure.Persistence;

namespace CashFlowArchitecture.Infrastructure.Messaging;

public sealed class OutboxIntegrationEventPublisher(CashFlowDbContext dbContext) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.OutboxMessages.Add(new OutboxMessageEntity
        {
            EventUid = integrationEvent.EventUid,
            EventType = integrationEvent.EventType,
            CorrelationId = integrationEvent.CorrelationId,
            Payload = JsonSerializer.Serialize(integrationEvent, JsonOptions),
            OccurredAt = integrationEvent.OccurredAt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }
}
