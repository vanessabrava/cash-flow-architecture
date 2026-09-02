using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlowArchitecture.Infrastructure.Messaging;

public sealed class OutboxMessagePublisher(
    IServiceScopeFactory scopeFactory,
    RabbitMqIntegrationEventPublisher rabbitMqIntegrationEventPublisher,
    ILogger<OutboxMessagePublisher> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        await PublishPendingMessagesAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PublishPendingMessagesAsync(stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToArrayAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(message.Payload, JsonOptions);

                if (integrationEvent is null)
                {
                    message.RetryCount++;
                    message.LastError = "Payload invalido para publicacao.";
                    continue;
                }

                await rabbitMqIntegrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;

                logger.LogInformation(
                    "Outbox message published. EventUid: {EventUid}. CorrelationId: {CorrelationId}.",
                    message.EventUid,
                    message.CorrelationId);
            }
            catch (Exception exception)
            {
                message.RetryCount++;
                message.LastError = exception.Message;

                logger.LogWarning(
                    exception,
                    "Error publishing outbox message. EventUid: {EventUid}. CorrelationId: {CorrelationId}. RetryCount: {RetryCount}.",
                    message.EventUid,
                    message.CorrelationId,
                    message.RetryCount);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
